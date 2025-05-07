using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask;
using Signal2025AzureConversationRelay.Messages.FromTwilio;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Threading;
using Microsoft.DurableTask.Entities;
using Signal2025AzureConversationRelay.Messages;
using Signal2025AzureConversationRelay.Functions.Activities;
using Signal2025AzureConversationRelay.Entities;
using System.Collections.Generic;

namespace Signal2025AzureConversationRelay
{
    /// <summary>
    /// This orchestrator function starts when a call hits the IncomingCallHttpTrigger.
    /// It sets up the necessary entities for state management, and then waits for events
    /// from Conversation Relay, and Twilio status callbacks.
    /// 
    /// It's important to note that the orchestrator is a long-running function that delegates
    /// work to activity functions. The orchestrator itself is intended to be deterministic, and 
    /// in the event of host failure or restart, the orchestrator may replay multiple times.
    /// Because of this, we will use entities to manage state, and the orchestrator will read the state
    /// from the entities when it needs to make decisions.
    /// </summary>
    public static class CallOrchestratorFunction
    {
        // we will set a maximum time to live for the orchestrator as most customer interactions
        // using conversation relay will be short-lived. this is a fail-safe mechanism to ensure
        // that the orchestrator does not run indefinitely.
        private static readonly int MaxMinutesTTL = 30;

        private static ILogger _logger;

        [Function(nameof(CallOrchestratorFunction))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            _logger = context.CreateReplaySafeLogger(nameof(CallOrchestratorFunction));
            string callSid = context.InstanceId;
            bool callActive = true;

            using (_logger.BeginScope(callSid))
            {
                // create a durable entity to manage the conversation turns of the call
                var conversationEntityInstance = new EntityInstanceId(nameof(ConversationHistoryEntity), callSid);

                var endSessionEvents = new List<Task>();

                // end the orchestrator if the call runs for too long
                TimeSpan timeout = TimeSpan.FromMinutes(MaxMinutesTTL);
                DateTime deadline = context.CurrentUtcDateTime.Add(timeout);
                var timerTask = context.CreateTimer(deadline, CancellationToken.None);

                // end the orchestrator if the call moves to a completed state by Twilio
                var callStatusCompletedTask = context.WaitForExternalEvent<string>("TwilioCallStatusEvent");

                // end the orchestrator if conversation relay triggers the action callback url
                var actionCallbackTask = context.WaitForExternalEvent<string>("ConversationRelayActionCallbackEvent");

                endSessionEvents.Add(timerTask);
                endSessionEvents.Add(callStatusCompletedTask);
                endSessionEvents.Add(actionCallbackTask);

                // wait for any of the end session events to complete
                // await Task.WhenAny(endSessionEvents);

                while (callActive)
                {
                    callActive = await ProcessCallEvents(context, conversationEntityInstance, timerTask, _logger);
                }

                await context.Entities.SignalEntityAsync(conversationEntityInstance, nameof(ConversationHistoryEntity.Delete));
            }
        }

        private static async Task<bool> ProcessCallEvents(TaskOrchestrationContext context, EntityInstanceId entityInstanceId, Task timerTask, ILogger _logger)
        {
            // wait for an event from the conversation relay, or the timer to expire
            var eventTask = context.WaitForExternalEvent<string>("ConversationRelayMessage");
            var callStatusTask = context.WaitForExternalEvent<string>("TwilioCallStatusEvent");
            var completedTask = await Task.WhenAny(eventTask, callStatusTask, timerTask);

            // if the timer expired, we will end the call and set the status
            if (completedTask == timerTask)
            {
                _logger.LogWarning("Max TTL Timer expired");
                context.SetCustomStatus("Max TTL Timer expired");
                return false;
                // TODO: gracefully end the twilio call if not already ended
            }

            if (completedTask == callStatusTask)
            {
                // if the call status task completed, we will log the status and return
                var callStatus = callStatusTask.Result;
                context.SetCustomStatus($"Call status moved to {callStatus} by Twilio");
                return false;
            }


            // if the eventTask completed, we will deserialize the message and pass it to the 
            // appropriate activity function
            var eventJson = eventTask.Result;
            var inboundMessage = MessageFactory.Deserialize(eventJson, context.InstanceId);

            switch (inboundMessage.Type)
            {
                case InboundMessageType.Prompt:
                    var promptMessage = (UserPromptMessage)inboundMessage;
                    _logger.LogTrace($"Voice Prompt: {promptMessage.VoicePrompt}");
                    
                    // add the prompt to the conversation history entity
                    var currentHistory = await context.Entities.CallEntityAsync<ChatHistory>(entityInstanceId, nameof(ConversationHistoryEntity.AddUserMessage), promptMessage.VoicePrompt);

                    // call the activity function to handle the prompt
                    var activityParameters = new HandlePromptActivityFunctionParams()
                    {
                        UserPromptMessage = promptMessage,
                        ChatHistory = currentHistory
                    };
                    var response = await context.CallActivityAsync<string>(nameof(HandlePromptActivityFunction), activityParameters);
                    await context.Entities.SignalEntityAsync(entityInstanceId, nameof(ConversationHistoryEntity.AddAssistantMessage), response);
                    break;
                case InboundMessageType.DTMF:
                    var dtmfMessage = (UserDTMFMessage)inboundMessage;
                    _logger.LogTrace($"DTMF Digit: {dtmfMessage.Digit}");
                    var updateForEntity = $"The user entered '{dtmfMessage.Digit}' on their phone keypad.";
                    await context.Entities.SignalEntityAsync(entityInstanceId, nameof(ConversationHistoryEntity.AddDeveloperMessage), updateForEntity);
                    break;
                case InboundMessageType.Interrupt:
                    var interruptMessage = (UserInterruptMessage)inboundMessage;
                    _logger.LogTrace($"Interrupted @: {interruptMessage.UtteranceUntilInterrupt}");
                    var interruptUpdate = $"The user interrupted the conversation around when you said '{interruptMessage.UtteranceUntilInterrupt}'.  Anything said after that was not heard by the user.";
                    await context.Entities.SignalEntityAsync(entityInstanceId, nameof(ConversationHistoryEntity.AddDeveloperMessage), interruptUpdate);
                    break;
                case InboundMessageType.Setup:
                    var setupMessage = (SystemSetupMessage)inboundMessage;
                    _logger.LogTrace($"Conversation Relay Session ID: {setupMessage.SessionId}");
                    if(setupMessage.CustomParameters?.Count > 0)
                    {
                        var customParams = string.Join(Environment.NewLine, setupMessage.CustomParameters);
                        var setupMessageUpdate = $"The following custom parameters were recieved and may be helpful context: {customParams}";
                        await context.Entities.SignalEntityAsync(entityInstanceId, nameof(ConversationHistoryEntity.AddDeveloperMessage), setupMessageUpdate);
                    }
                    break;
                case InboundMessageType.Error:
                    var errorMessage = (SystemErrorMessage)inboundMessage;
                    _logger.LogError($"Error Type: {errorMessage.Type}, Error Message: {errorMessage.Description}");
                    break;
                default:
                    _logger.LogWarning($"Unknown message type: {inboundMessage.Type}");
                    break;
            }

            return true;
        }
    }
}
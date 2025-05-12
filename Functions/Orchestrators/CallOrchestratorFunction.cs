using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Entities;
using Signal2025AzureConversationRelay.Entities;
using Signal2025AzureConversationRelay.Messages.FromTwilio;
using Signal2025AzureConversationRelay.Functions.Activities;
using Signal2025AzureConversationRelay.Messages.ToTwilio;


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

        /// <summary>
        /// The orchestrator manages all events from ConversationRelay and Twilio.
        /// It will then delegate the responses to an LLM via sub-orchestrators or activity functions.
        /// 
        /// It will also manage the state of the conversation using a durable entity.
        /// The orchestrator will run in a loop until one of the following conditions are met:
        /// 1. The CallStatus is updated to completed by Twilio
        /// 2. The orchestrator ends after a timeout
        /// 3. Conversation Relay signals the end of a session
        /// 
        /// Once one of these conditions are met, it will handle transitioning the call accordingly.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [Function(nameof(CallOrchestratorFunction))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            _logger = context.CreateReplaySafeLogger(nameof(CallOrchestratorFunction));
            string callSid = context.InstanceId;
            bool callActive = true;

            using (_logger.BeginScope(callSid))
            {
                // we'll create a durable entity to manage the conversation turns of the call using 
                // Microsoft.SemanticKernel.ChatCompletion.ChatHistory as the state
                // this initializes the ChatHistory entity with a default system prompt so that the 
                // agent has some base instructions to work with when responding to the user
                var conversationEntityInstance = new EntityInstanceId(nameof(ConversationHistoryEntity), callSid);

                #region setup events capable of interrupting the default flow
                // we will set a maximum time to live for the orchestrator as most customer interactions
                // using conversation relay will be short-lived. this is a fail-safe mechanism to ensure
                // that the orchestrator does not run indefinitely.
                TimeSpan timeout = TimeSpan.FromMinutes(MaxMinutesTTL);
                DateTime deadline = context.CurrentUtcDateTime.Add(timeout);
                var timerTask = context.CreateTimer(deadline, CancellationToken.None);

                // we'll setup a listener which listens to the call status callback from Twilio to let us know
                // if the call has ended
                var callStatusCompletedTask = context.WaitForExternalEvent<string>("TwilioCallStatusCompletedEvent");

                //TODO: add a listener for the conversation relay action callback event
                // var actionCallbackTask = context.WaitForExternalEvent<string>("ConversationRelayActionCallbackEvent");
                #endregion

                // very quickly after the orchestrator starts, the first event which should be received 
                // is the setup message from conversation relay
                var setupMessage = context.WaitForExternalEvent<SystemSetupMessage>(nameof(SystemSetupMessage));

                // wait for the setup message to complete, the call to end, or the timer to expire
                var processedEvent = await Task.WhenAny(timerTask, callStatusCompletedTask, setupMessage);

                // decide if we should continue the call based on the event that was raised
                callActive = await HandleIntialAwaitedEvent(context, conversationEntityInstance, timerTask, callStatusCompletedTask, setupMessage, processedEvent);

                // next, we'll start a loop where we listen for messages from conversation relay 
                // which signal a user action, and handle them accordingly.
                while (callActive)
                {
                    // we'll setup a listener for when the user speaks to the assistant. 
                    var userPromptTask = context.WaitForExternalEvent<UserPromptMessage>(nameof(UserPromptMessage));

                    // lastly we'll setup a listener for when the user presses a DTMF key on their phone
                    var userDTMFTask = context.WaitForExternalEvent<UserDTMFMessage>(nameof(UserDTMFMessage));

                    List<Task> eventsToWaitFor = [userPromptTask, userDTMFTask, timerTask, callStatusCompletedTask];

                    var callEvent = await Task.WhenAny(eventsToWaitFor);

                    switch (callEvent)
                    {
                        case var _ when callEvent == userPromptTask:
                            //send to a sub-orchestrator to handle the prompt
                            var taskOptions = new TaskOptions().WithInstanceId(callSid.Substring(2));

                            // call a sub-orchestrator to handle the prompt.  we do this so that we can cancel the sub-orchestrator
                            // if the user interrupts the assistant.  this is a bit of a hack to get around the fact that azure
                            // durable orchestrator functions do not support cancellation tokens
                            _ = context.CallSubOrchestratorAsync(nameof(PromptOrchestratorFunction), userPromptTask.Result, new TaskOptions().WithInstanceId(callSid.Substring(2)));
                            break;

                        case var _ when callEvent == userDTMFTask:
                            
                            var dtmfMessage = userDTMFTask.Result;
                            _logger.LogTrace($"DTMF Digit: {dtmfMessage.Digit}");

                            if(dtmfMessage.Digit == 0){
                                var end = new EndSessionMessage(callSid, new Dictionary<string, object>
                                {
                                    { "action", "human_escalation" },
                                    { "reason_code", "ZERO_ENTERED" },
                                    { "summary", await GetConversationSummary(context) }
                                });

                                // var human_escalation = await context.CallActivityAsync<Task>(nameof(EndSessionActivityFunction), end);
                                callActive = false;
                            }

                            break;

                        case var _ when callEvent == timerTask:
                            callActive = false;
                            break;

                        case var _ when callEvent == callStatusCompletedTask:
                            callActive = false;
                            break;
                    }
                }
            }
        }
        private static async Task<string> GetConversationSummary(TaskOrchestrationContext context)
        {
            var summary = await context.Entities.CallEntityAsync<string>(
                new EntityInstanceId(nameof(ConversationHistoryEntity), context.InstanceId), 
                nameof(ConversationHistoryEntity.GetSummary));

            _logger.LogInformation($"Conversation summary: {summary}");
            return summary;
        }
        private static async Task<bool> HandleIntialAwaitedEvent(TaskOrchestrationContext context, EntityInstanceId conversationEntityInstance, Task timerTask, Task<string> callStatusCompletedTask, Task<SystemSetupMessage> setupMessage, Task processedEvent)
        {
            if (processedEvent == timerTask)
            {
                // if the timer expired, we will end the call and set the status
                _logger.LogWarning("Max TTL Timer expired");
                context.SetCustomStatus("Max TTL Timer expired");
                return false;
            }
            else if (processedEvent == callStatusCompletedTask)
            {
                // if the call status task completed, we will log the status and return
                var callStatus = callStatusCompletedTask.Result;
                context.SetCustomStatus($"Call status moved to {callStatus} by Twilio");
                return false;
            }
            else if (processedEvent == setupMessage)
            {
                // if the setup message has any custom parameters, we will add them as a developer message
                // so that they can be used by the LLM when generating responses
                if (setupMessage.Result.CustomParameters?.Count > 0)
                {
                    var customParams = string.Join(Environment.NewLine, setupMessage.Result.CustomParameters);
                    var setupMessageUpdate = $"The following custom parameters were recieved as context for the call and may be helpful: {customParams}";
                    await context.Entities.SignalEntityAsync(conversationEntityInstance, nameof(ConversationHistoryEntity.AddDeveloperMessage), setupMessageUpdate);
                }
            }

            return true;
        }
    }
}
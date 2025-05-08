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
    public static class PromptOrchestratorFunction
    {
        private static ILogger _logger;

        [Function(nameof(PromptOrchestratorFunction))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context, UserPromptMessage promptMessage)
        {
            _logger = context.CreateReplaySafeLogger(nameof(PromptOrchestratorFunction));
            using(_logger.BeginScope(promptMessage.CallSid))
            {

                await context.Entities.SignalEntityAsync(
                    new EntityInstanceId(nameof(ConversationHistoryEntity), promptMessage.CallSid), 
                    nameof(ConversationHistoryEntity.AddUserMessage), 
                    promptMessage.VoicePrompt
                );

                // get the conversation entity by callSid
                var chatHistory = await context.Entities.CallEntityAsync<ChatHistory>(
                    new EntityInstanceId(
                        nameof(ConversationHistoryEntity), 
                        promptMessage.CallSid), 
                    nameof(ConversationHistoryEntity.GetConversationHistory)
                );

                // generate a payload to pass to the activity function
                var activityParams = new HandlePromptActivityFunctionParams
                {
                    UserPromptMessage = promptMessage,
                    ChatHistory = chatHistory,
                };

                // call the activity function to handle the prompt
                var assistantResponse = await context.CallActivityAsync<string>(nameof(HandlePromptActivityFunction), activityParams);

                await context.Entities.SignalEntityAsync(
                    new EntityInstanceId(nameof(ConversationHistoryEntity), promptMessage.CallSid), 
                    nameof(ConversationHistoryEntity.AddAssistantMessage), 
                    assistantResponse
                );

                return;
            }
        }
    }
}
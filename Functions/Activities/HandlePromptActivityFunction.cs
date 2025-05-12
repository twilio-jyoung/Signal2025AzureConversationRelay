using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Signal2025AzureConversationRelay.Entities;
using Signal2025AzureConversationRelay.Messages.ToTwilio;
using Signal2025AzureConversationRelay.Services;

namespace Signal2025AzureConversationRelay.Functions.Activities
{
    public class HandlePromptActivityFunction
    {
        private readonly ILogger<HandlePromptActivityFunction> _logger;
        private readonly SemanticKernelService _semanticKernelService;
        private readonly AzureWebPubSubService _azureWebPubSubService;
        private readonly string _interruptNote = "The previous message was interrupted by the user.  They may not have heard everything, so you may need to repeat some of it.";        
        public HandlePromptActivityFunction(ILogger<HandlePromptActivityFunction> logger, SemanticKernelService semanticKernelService, AzureWebPubSubService azureWebPubSubService)
        {
            _logger = logger;
            _semanticKernelService = semanticKernelService;
            _azureWebPubSubService = azureWebPubSubService;
        }

        [Function(nameof(HandlePromptActivityFunction))]
        public async Task<string> HandlePrompt(
            [ActivityTrigger] HandlePromptActivityFunctionParams promptParams, 
            [DurableClient] DurableTaskClient dtClient,
            string instanceId
        )
        {
            var callSid = promptParams.UserPromptMessage.CallSid;

            var sentenceBuilder = new StringBuilder();
            var fullResponseBuilder = new StringBuilder();

            using (_logger.BeginScope(callSid))
            {
                _logger.LogTrace($"User: {promptParams.UserPromptMessage.VoicePrompt}");

                var ccs = _semanticKernelService.GetChatCompletionService();
                var botResponse = ccs.GetStreamingChatMessageContentsAsync(
                    chatHistory: promptParams.ChatHistory,
                    kernel: _semanticKernelService.Kernel,
                    executionSettings: _semanticKernelService.GetOpenAIPromptExecutionSettings()
                );
                
                await foreach (var chunk in botResponse)
                {
                    if(string.IsNullOrEmpty(chunk.Content))
                        continue;
                    
                    sentenceBuilder.Append(chunk.Content);

                    if(chunk.Content.Contains(".") || chunk.Content.Contains("!") || chunk.Content.Contains("?")){

                        var completeSentence = sentenceBuilder.ToString();

                        var promptOrchestrator = await dtClient.GetInstanceAsync(callSid.Substring(2));
                        if (promptOrchestrator.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
                        {
                            _logger.LogWarning($"Parent Orchestrator {instanceId} was terminated.  Stopping streaming.");

                            // signal to twilio that we're done sending tokens for right now
                            _azureWebPubSubService.SendMessageToCall(new SendTokenMessage(callSid, "", last: true));

                            // push what we have so far to the conversation history
                            fullResponseBuilder.Append(completeSentence);
                            await dtClient.Entities.SignalEntityAsync(
                                new EntityInstanceId(nameof(ConversationHistoryEntity), callSid),
                                nameof(ConversationHistoryEntity.AddAssistantMessage),
                                fullResponseBuilder.ToString()
                            );

                            // then push that this was interrupted
                            await dtClient.Entities.SignalEntityAsync(
                                new EntityInstanceId(nameof(ConversationHistoryEntity), callSid),
                                nameof(ConversationHistoryEntity.AddDeveloperMessage),
                                _interruptNote
                            );

                            return "";
                        }

                        var sentenceEvent = new SendTokenMessage(callSid, completeSentence, last: false);
                        _azureWebPubSubService.SendMessageToCall(sentenceEvent);
                        fullResponseBuilder.Append(completeSentence);
                        sentenceBuilder.Clear();
                    }
                }

                _azureWebPubSubService.SendMessageToCall(new SendTokenMessage(callSid, "", last: true));

                var response = fullResponseBuilder.ToString();
                _logger.LogTrace($"LLM: {response}");

                return fullResponseBuilder.ToString();
            }
        }
    }
}
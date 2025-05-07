using System;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.SemanticKernel.ChatCompletion;
using Signal2025AzureConversationRelay.Entities;
using Signal2025AzureConversationRelay.Messages.FromTwilio;
using Signal2025AzureConversationRelay.Messages.ToTwilio;
using Signal2025AzureConversationRelay.Services;

namespace Signal2025AzureConversationRelay.Functions.Activities
{
    public class HandlePromptActivityFunction
    {
        private readonly ILogger<HandlePromptActivityFunction> _logger;
        private readonly SemanticKernelService _semanticKernelService;
        private readonly AzureWebPubSubService _azureWebPubSubService;
        public HandlePromptActivityFunction(ILogger<HandlePromptActivityFunction> logger, SemanticKernelService semanticKernelService, AzureWebPubSubService azureWebPubSubService)
        {
            _logger = logger;
            _semanticKernelService = semanticKernelService;
            _azureWebPubSubService = azureWebPubSubService;
        }

        [Function(nameof(HandlePromptActivityFunction))]
        public async Task<string> HandlePrompt(
            [ActivityTrigger] HandlePromptActivityFunctionParams promptParams, 
            string instanceId
        )
        {
            _logger.LogTrace("HandlePromptActivityFunction started.");
            _logger.LogTrace($"InstanceId: {instanceId}");

            var callSid = promptParams.UserPromptMessage.CallSid;

            var sentenceBuilder = new StringBuilder();
            var fullResponseBuilder = new StringBuilder();

            using (_logger.BeginScope(callSid))
            {
                _logger.LogTrace($"Handing off to LLM for prompt: {promptParams.UserPromptMessage.VoicePrompt}");

                var ccs = _semanticKernelService.GetChatCompletionService();
                var botResponse = ccs.GetStreamingChatMessageContentsAsync(
                    chatHistory: promptParams.ChatHistory,
                    kernel: _semanticKernelService.Kernel
                );
                
                await foreach (var chunk in botResponse)
                {
                    _logger.LogTrace($"LLM has generated a chunk: {chunk.Content}");

                    if(string.IsNullOrEmpty(chunk.Content))
                        continue;
                    
                    sentenceBuilder.Append(chunk.Content);

                    if(chunk.Content.Contains(".") || chunk.Content.Contains("!") || chunk.Content.Contains("?")){

                        var completeSentence = sentenceBuilder.ToString();

                        _logger.LogTrace($"LLM has generated a complete sentence: {completeSentence}");

                        var sentenceEvent = new SendTokenMessage(callSid, completeSentence, last: false);
                        _azureWebPubSubService.SendMessageToCall(sentenceEvent);

                        // SendToCall(new SendTokenMessage(message.CallSid, sentenceBuilder.ToString(), last: false));
                        fullResponseBuilder.Append(completeSentence);
                        sentenceBuilder.Clear();
                    }
                }

                _azureWebPubSubService.SendMessageToCall(new SendTokenMessage(callSid, "", last: true));

                var response = fullResponseBuilder.ToString();
                _logger.LogTrace($"LLM response: {response}");

                return fullResponseBuilder.ToString();
            }
        }
    }
}
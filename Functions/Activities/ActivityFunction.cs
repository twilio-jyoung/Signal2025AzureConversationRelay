using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Signal2025AzureConversationRelay.Services;

namespace Signal2025AzureConversationRelay
{
    public static class ActivityFunction
    {
        [Function("ActivityFunction_Hello")]
        public static async Task<string> SayHello([ActivityTrigger] string name, FunctionContext context, SemanticKernelService semanticKernelService)
        {
            var log = context.GetLogger("ActivityFunction");
            // var chatCompletionService = semanticKernelService.GetChatCompletionService();
            // var chatMessageContent = await chatCompletionService.GetChatMessageContentAsync($"Tell me about {name}");


            // log.LogInformation($"Tell me about {name}.");
            // log.LogInformation($"Response: {chatMessageContent.Content}");
            log.LogError($"Saying hello to {name} from the ActivityFunction.");
            await Task.Delay(TimeSpan.FromSeconds(5));
            return $"Hello, {name}!";
        }
    }
}
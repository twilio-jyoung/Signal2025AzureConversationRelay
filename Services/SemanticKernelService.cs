using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Kernel = Microsoft.SemanticKernel.Kernel;
using System;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Signal2025AzureConversationRelay.Models.Messages.ToTwilio;

namespace Signal2025AzureConversationRelay.Services
{
    public class SemanticKernelService
    {
        private readonly IChatCompletionService _chatCompletionService;
        private readonly OpenAIPromptExecutionSettings _openAIPromptExecutionSettings;
        private readonly Kernel _kernel;
        public SemanticKernelService(ILogger<SemanticKernelService> logger, IConfiguration configuration)
        {
            var keyVaultUrl = new Uri(configuration["Azure:KeyVault:Url"]);

            logger.LogTrace("Fetching Key Vault Secrets.");
            var kv = new SecretClient(keyVaultUrl, new DefaultAzureCredential());
            var deploymentName = kv.GetSecret("AzureOpenAiDeploymentName").Value.Value;
            var endpoint = kv.GetSecret("AzureOpenAiEndpoint").Value.Value;
            var apiKey = kv.GetSecret("AzureOpenAiApiKey").Value.Value;
            logger.LogTrace($"Fetched Key Vault Secrets.");
            
            var _kernelBuilder = Kernel.CreateBuilder();

            _openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings() 
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            _kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName ?? configuration["Azure:OpenAI:DeploymentName"],
                endpoint: endpoint ?? configuration["Azure:OpenAI:Endpoint"],
                apiKey: apiKey ?? configuration["Azure:OpenAI:ApiKey"]
            );
            _kernel = _kernelBuilder.Build();

            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public IChatCompletionService GetChatCompletionService()
        {
            return _chatCompletionService;
        }

        public OpenAIPromptExecutionSettings GetOpenAIPromptExecutionSettings()
        {
            return _openAIPromptExecutionSettings;
        }

        public Kernel Kernel => _kernel;
    }
}
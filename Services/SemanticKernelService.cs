using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Kernel = Microsoft.SemanticKernel.Kernel;
using System;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Signal2025AzureConversationRelay.Services
{
    public class SemanticKernelService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly OpenAIPromptExecutionSettings _openAIPromptExecutionSettings;
        private readonly Kernel _kernel;
        public SemanticKernelService(ILogger<SemanticKernelService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // _logger.LogInformation("Fetching Key Vault Secrets.");
            // var kv = new SecretClient(new Uri(_configuration["Azure:KeyVault:Url"]),new DefaultAzureCredential());
            // var deploymentName = kv.GetSecret("AzureOpenAiDeploymentName").Value.Value;
            // var endpoint = kv.GetSecret("AzureOpenAiEndpoint").Value.Value;
            // var apiKey = kv.GetSecret("AzureOpenAiApiKey").Value.Value;
            // _logger.LogInformation($"Fetched Key Vault Secrets.");
            
            var _kernelBuilder = Kernel.CreateBuilder();

            _openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings() 
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            _kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: _configuration["Azure:OpenAI:DeploymentName"],
                endpoint: _configuration["Azure:OpenAI:Endpoint"],
                apiKey: _configuration["Azure:OpenAI:ApiKey"]
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
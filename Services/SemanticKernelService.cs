using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Kernel = Microsoft.SemanticKernel.Kernel;

namespace Signal2025AzureConversationRelay.Services
{
    public class SemanticKernelService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly Kernel _kernel;
        public SemanticKernelService(ILogger<SemanticKernelService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            var _kernelBuilder = Kernel.CreateBuilder();
            _kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: _configuration["Azure:OpenAI:DeploymentName"],
                endpoint: _configuration["Azure:OpenAI:Endpoint"],
                apiKey: _configuration["Azure:OpenAI:APIKey"]
            );
            _kernel = _kernelBuilder.Build();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public IChatCompletionService GetChatCompletionService()
        {
            return _chatCompletionService;
        }

        public Kernel Kernel => _kernel;
    }
}
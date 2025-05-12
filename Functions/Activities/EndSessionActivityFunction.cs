using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Signal2025AzureConversationRelay.Services;
using Signal2025AzureConversationRelay.Messages.ToTwilio;

namespace Signal2025AzureConversationRelay.Functions.Activities
{
    public class EndSessionActivityFunction
    {
        private readonly ILogger<EndSessionActivityFunction> _logger;
        private readonly AzureWebPubSubService _azureWebPubSubService;
        public EndSessionActivityFunction(
            ILogger<EndSessionActivityFunction> logger,
            AzureWebPubSubService azureWebPubSubService)
        {
            _logger = logger;
            _azureWebPubSubService = azureWebPubSubService;
        }

        [Function(nameof(EndSessionActivityFunction))]
        public async Task Run(
            [ActivityTrigger] EndSessionMessage endSessionMessage,
            [DurableClient] DurableTaskClient dtClient)
        {
            var callSid = endSessionMessage.CallSid;

            using(_logger.BeginScope(callSid))
            {
                _logger.LogTrace($"Ending session");                    
                _azureWebPubSubService.SendMessageToCall(endSessionMessage);
                // _azureWebPubSubService.CloseConnection(callSid, "Session ended");
            }
        }
    }
}
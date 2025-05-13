using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Signal2025AzureConversationRelay.Services;

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
        public void Run(
            [ActivityTrigger] EndSessionActivityFunctionParams endSessionActivityFunctionParams)
        {
            var callSid = endSessionActivityFunctionParams.CallSid;

            using(_logger.BeginScope(callSid))
            {
                _logger.LogTrace($"Ending session");

                _azureWebPubSubService.SendMessageToCall(endSessionActivityFunctionParams.ToEndSessionMessage());
            }
        }
    }
}
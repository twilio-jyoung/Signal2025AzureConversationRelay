using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Signal2025AzureConversationRelay.Messages;
using Signal2025AzureConversationRelay.Services;
using Signal2025AzureConversationRelay.Models.Messages.ToTwilio;
using Signal2025AzureConversationRelay.Utilities;

namespace Signal2025AzureConversationRelay.Functions.Triggers.Twilio
{
    public class ConnectActionCallbackHttpTriggerFunction
    {
        private readonly ILogger<ConnectActionCallbackHttpTriggerFunction> _logger;
        private readonly TwiMLGeneratorService _twiMLGeneratorService;
        private readonly AzureWebPubSubService _azureWebPubSubService;

        public ConnectActionCallbackHttpTriggerFunction(
            ILogger<ConnectActionCallbackHttpTriggerFunction> logger, 
            TwiMLGeneratorService twiMLGeneratorService,
            AzureWebPubSubService azureWebPubSubService
        )
        {
            _logger = logger;
            _twiMLGeneratorService = twiMLGeneratorService;
            _azureWebPubSubService = azureWebPubSubService;
        }

        [Function(nameof(ConnectActionCallbackHttpTriggerFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calls/actionCallback")] HttpRequestData req,
            [DurableClient] DurableTaskClient dtClient,
            FunctionContext context)
        {

            _logger.LogTrace("ConnectActionCallbackHttpTriggerFunction called");

            var callSid = ContextParamsHelper.GetParamFromContext(context.Items, "CallSid");

            // at this point, conversation relay has indicated that it is no longer processing the call
            // so go ahead and tear down the websocket connection (it would happen automatically after the timeout)
            _azureWebPubSubService.CloseConnection(callSid);

            using (_logger.BeginScope(callSid))
            {
                var sessionStatus = ContextParamsHelper.GetParamFromContext(context.Items, "SessionStatus");

                switch(sessionStatus)
                {
                    case "completed":
                        _logger.LogTrace("Session completed normally (caller hung up)");
                        break;
                    case "ended":
                        _logger.LogTrace("Session ended by application");
                        var handoffDataJson = ContextParamsHelper.GetParamFromContext(context.Items, "HandoffData");

                        var handoffData = MessageFactory.Deserialize<EndSessionMessageHandoffData>(handoffDataJson);
                        if (handoffData.EndSessionAction == EndSessionAction.Hangup){
                            _logger.LogTrace("HandoffData indicates to hang up the call");
                            return _twiMLGeneratorService.CreateTwiMLHttpResponse(
                                req, _twiMLGeneratorService.GenerateHangupTwiML());
                        }
                        else if (handoffData.EndSessionAction == EndSessionAction.Escalate)
                        {
                            // at this point, you can generate any new TwiML you want to sent to Twilio
                            _logger.LogTrace("HandoffData indicates to escalate the call");

                            // say goodbye and hang up
                            return _twiMLGeneratorService.CreateTwiMLHttpResponse(req, _twiMLGeneratorService.GenerateSayAndHangupTwiML("At this point you would be conected to a human. Goodbye..."));

                            // send to flex
                            //return await _twiMLGeneratorService.CreateTwiMLHttpResponse(req, _twiMLGeneratorService.GenerateEnqueueToFlexWorkflowTwiML("WW0123456789abcdef0123456789abcdef"));

                            // whatever you want to do here
                            // you can also add more data to the handoffData object if nceessary to provide more context
                        }
                        break;
                    case "failed":
                        _logger.LogTrace("Error occurred during session");
                        break;
                    default:
                        _logger.LogWarning("Unknown session status: {SessionStatus}", sessionStatus);
                        break;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("OK");
                return response;
            }
        }
    }
}
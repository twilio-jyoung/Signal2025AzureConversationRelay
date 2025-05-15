using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Signal2025AzureConversationRelay.Utilities;

namespace Signal2025AzureConversationRelay.Functions.Triggers.Twilio
{
    public class CallStatusHttpTriggerFunction
    {
        private readonly ILogger<CallStatusHttpTriggerFunction> _logger;

        public CallStatusHttpTriggerFunction(ILogger<CallStatusHttpTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(CallStatusHttpTriggerFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calls/status")] HttpRequestData req,
            [DurableClient] DurableTaskClient dtClient,
            FunctionContext context)
        {
            var callSid = ContextParamsHelper.GetParamFromContext(context.Items, "CallSid");
            var callStatus = ContextParamsHelper.GetParamFromContext(context.Items, "CallStatus");

            using (_logger.BeginScope(callSid))
            {
                _logger.LogTrace("New call status: {CallStatus}", callStatus);

                if(callStatus == "completed" || callStatus == "failed" || callStatus == "busy")
                {
                    var instance = await dtClient.GetInstanceAsync(callSid);
                    if (instance?.IsRunning == true)
                        await dtClient.RaiseEventAsync(callSid, "TwilioCallStatusCompletedEvent", callStatus);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("OK");
                return response;
            }
        }
    }
}
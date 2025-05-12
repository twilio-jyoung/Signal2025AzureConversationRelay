using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using System.Collections.Generic;
using System;

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
            string callSid = GetParamFromContext(context.Items, "CallSid");
            string callStatus = GetParamFromContext(context.Items, "CallStatus");

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

        /// <summary>
        /// Retrieves a string value from the FunctionContext.Items dictionary by key name.
        /// (This is extracted from the raw http request by the ValidateTwilioRequestMiddleware).
        /// </summary>
        private static string GetParamFromContext(IDictionary<object, object> items, string paramName)
        {
            if (items.TryGetValue(paramName, out var paramObj) && paramObj is string paramString)
            {
                return paramString;
            }
            throw new ArgumentException($"{paramName} not found in FunctionContext.Items");
        }
    }
}
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using System.Collections.Generic;
using System;
using Signal2025AzureConversationRelay.Messages.FromTwilio;
using Signal2025AzureConversationRelay.Messages;

namespace Signal2025AzureConversationRelay.Functions.Triggers.Twilio
{
    public class ConnectActionCallbackHttpTriggerFunction
    {
        private readonly ILogger<ConnectActionCallbackHttpTriggerFunction> _logger;

        public ConnectActionCallbackHttpTriggerFunction(ILogger<ConnectActionCallbackHttpTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ConnectActionCallbackHttpTriggerFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calls/actionCallback")] HttpRequestData req,
            [DurableClient] DurableTaskClient dtClient,
            FunctionContext context)
        {
            string callSid = GetParamFromContext(context.Items, "CallSid");

            using (_logger.BeginScope(callSid))
            {
                string sessionStatus = GetParamFromContext(context.Items, "SessionStatus");

                switch(sessionStatus)
                {
                    case "completed":
                        _logger.LogTrace("Session completed normally (caller hung up)");
                        break;
                    case "ended":
                        _logger.LogTrace("Session ended by application");
                        var handoffDataJson = GetParamFromContext(context.Items, "HandoffData");
                        var handoffData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(handoffDataJson);

                        // you would then use this to provide new twiml to twilio on how to handle the call
                        // given the information provided in the HandoffData
                        break;
                    case "failed":
                        _logger.LogTrace("Error occurred during session");
                        break;
                    default:
                        _logger.LogWarning("Unknown session status: {SessionStatus}", sessionStatus);
                        break;
                }

                // for now, just log the session status and return a 200 OK response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("OK");
                return response;
            }
        }

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
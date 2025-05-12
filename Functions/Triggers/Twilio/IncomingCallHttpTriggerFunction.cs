using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Signal2025AzureConversationRelay.Services;
using Twilio.TwiML;

namespace Signal2025AzureConversationRelay
{
    public class IncomingCallHttpTriggerFunction
    {
        private readonly TwiMLGeneratorService _TwiMLGeneratorService;
        private readonly SemanticKernelService _semanticKernelService;
        private readonly ILogger<IncomingCallHttpTriggerFunction> _logger;

        public IncomingCallHttpTriggerFunction(
            ILogger<IncomingCallHttpTriggerFunction> logger, 
            TwiMLGeneratorService twiMLGeneratorService,
            SemanticKernelService semanticKernelService)
        {
            _logger = logger;
            _TwiMLGeneratorService = twiMLGeneratorService;

            // we dont actually use this here, but loading it here speeds up the first response
            // on the very first call after a cold start as key vault secrets load slowly.
            _semanticKernelService = semanticKernelService;
        }

        /// <summary>
        /// When Twilio receives an incoming call, it will send a webhook request to this function.
        /// This function will then extract the necessary parameters from the request, start the 
        /// Durable Task orchestrator, and generate a ConversationRelay TwiML response to Twilio which
        /// includes a unique URL for the websocket connection to Azure WebPubSub scoped to the call SID.
        /// </summary>
        [Function(nameof(IncomingCallHttpTriggerFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calls")] HttpRequestData req,   
            [DurableClient] DurableTaskClient dtClient,
            FunctionContext context
        )
        {
            string callSid = GetParamFromContext(context.Items, "CallSid");
            string to = GetParamFromContext(context.Items, "To");
            string from = GetParamFromContext(context.Items, "From");
            using (_logger.BeginScope(callSid))
            {
                try
                {
                    _logger.LogTrace("Incoming call from {FROM} to {TO}", from, to);
                    await StartCallOrchestrator(dtClient, callSid);
                    return await CreateTwiMLHttpResponse(req, _TwiMLGeneratorService.GenerateConversationRelayTwiML(callSid.ToString()));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Incoming call processing failed.");   
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
            }
        }

        /// <summary>
        /// Creates an HTTP response with the provided TwiML.
        /// </summary> 
        private static async Task<HttpResponseData> CreateTwiMLHttpResponse(HttpRequestData req, VoiceResponse twiml)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/xml");
            await response.WriteStringAsync(twiml.ToString(), encoding: Encoding.UTF8);
            
            return response;
        }

        /// <summary>
        /// Retrieves a string value from the FunctionContext.Items dictionary by key name.
        /// (This is extracted from the raw http request by the ValidateTwilioRequestMiddleware).
        /// </summary>
        private string GetParamFromContext(IDictionary<object, object> items, string paramName)
        {
            if (items.TryGetValue(paramName, out var paramObj) && paramObj is string paramString)
            {
                return paramString;
            }
            throw new ArgumentException($"{paramName} not found in FunctionContext.Items");
        }

        /// <summary>
        /// Starts the Durable Task call orchestrator for the given call SID.
        /// </summary>
        private async Task StartCallOrchestrator(DurableTaskClient dtClient, string callSid)
        {
            // set the instance ID to the call SID
            var options = new StartOrchestrationOptions
            {
                InstanceId = callSid
            };

            await dtClient.ScheduleNewOrchestrationInstanceAsync(nameof(CallOrchestratorFunction), input: null, options: options);
            
            var instance = await dtClient.WaitForInstanceStartAsync(callSid);

            if (instance.RuntimeStatus != OrchestrationRuntimeStatus.Running)
            {
                _logger.LogError("Failed to start orchestration for {CALLSID}", callSid);
            }
        }
    }
}
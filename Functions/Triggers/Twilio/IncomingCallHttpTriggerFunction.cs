using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Signal2025AzureConversationRelay.Services;
using Signal2025AzureConversationRelay.Utilities;

namespace Signal2025AzureConversationRelay
{
    public class IncomingCallHttpTriggerFunction
    {
        private readonly TwiMLGeneratorService _twiMLGeneratorService;
        private readonly SemanticKernelService _semanticKernelService;
        private readonly ILogger<IncomingCallHttpTriggerFunction> _logger;

        public IncomingCallHttpTriggerFunction(
            ILogger<IncomingCallHttpTriggerFunction> logger, 
            TwiMLGeneratorService twiMLGeneratorService,
            SemanticKernelService semanticKernelService)
        {
            _logger = logger;
            _twiMLGeneratorService = twiMLGeneratorService;

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
            var callSid = ContextParamsHelper.GetParamFromContext(context.Items, "CallSid");

            using (_logger.BeginScope(callSid))
            {
                try
                {
                    var to = ContextParamsHelper.GetParamFromContext(context.Items, "To");
                    var from = ContextParamsHelper.GetParamFromContext(context.Items, "From");
                    _logger.LogTrace("Incoming call from {FROM} to {TO}", from, to);
                    
                    await StartCallOrchestrator(dtClient, callSid);
                    return _twiMLGeneratorService.CreateTwiMLHttpResponse(req, _twiMLGeneratorService.GenerateConversationRelayTwiML(callSid.ToString()));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Incoming call processing failed.");   
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
            }
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
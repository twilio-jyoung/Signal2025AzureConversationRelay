using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask;
using System.Text.Json;
using Signal2025AzureConversationRelay.Messages.FromTwilio;

namespace Signal2025AzureConversationRelay
{
    public static class OrchestratorFunction
    {
        [Function("OrchestratorFunction")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context, UserEventRequest request)
        {
            var _logger = context.CreateReplaySafeLogger("OrchestratorFunction");
            var callSid = request.ConnectionContext.UserId;
            var json = request.Data.ToString();

            _logger.LogWarning($"{callSid} sent a message: {json}");

            string messageType = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("type").GetString();

            switch (messageType)
            {
                case "prompt":
                    _logger.LogInformation("Received prompt message.");
                    // Deserialize the prompt message
                    var promptMessage = GetType<UserPromptMessage>(callSid, json);
                    _logger.LogInformation($"Call SID: {promptMessage.CallSid}, Voice Prompt: {promptMessage.VoicePrompt}, Language: {promptMessage.Language}, Last: {promptMessage.Last}");
                    break;
                case "setup":
                    _logger.LogInformation("Received setup message.");
                    // Deserialize the setup message
                    var setupMessage = JsonSerializer.Deserialize<SystemSetupMessage>(request.Data);
                    _logger.LogInformation($"Call SID: {setupMessage.CallSid}");
                    break;
                case "dtmf":
                    _logger.LogInformation("Received DTMF message.");
                    // Deserialize the DTMF message
                    var dtmfMessage = JsonSerializer.Deserialize<UserDTMFMessage>(request.Data);
                    _logger.LogInformation($"DTMF Digit: {dtmfMessage.Digit}");
                    break;
                case "error":
                    _logger.LogError("Received error message.");
                    break;
                default:
                    _logger.LogWarning($"Unknown message type: {messageType}");
                    break;

                // Setup,
                // Interrupt,
                // DTMF,
                // Prompt,
                // Error
            }
            
            var outputs = new List<string>
            {
                // Call activity functions in sequence
                await context.CallActivityAsync<string>("ActivityFunction_Hello", "Tokyo"),
                await context.CallActivityAsync<string>("ActivityFunction_Hello", "Seattle"),
                await context.CallActivityAsync<string>("ActivityFunction_Hello", "London")
            };

            return outputs;
        }

        private static T GetType<T>(string callSid, string json)
        {
            var response = JsonSerializer.Deserialize<T>(json);
            (response as IInboundMessage).CallSid = callSid;
            return response;
        }
    }
}
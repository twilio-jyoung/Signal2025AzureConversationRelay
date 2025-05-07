using System;
using System.Text.Json;
using Azure.Messaging.WebPubSub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Signal2025AzureConversationRelay.Messages;
using Signal2025AzureConversationRelay.Messages.ToTwilio;

namespace Signal2025AzureConversationRelay.Services
{
    public class AzureWebPubSubService
    {
        private readonly ILogger _logger;
        private readonly WebPubSubServiceClient _webPubSubClient;

        public AzureWebPubSubService(ILogger<AzureWebPubSubService> logger, IConfiguration configuration)
        {
            _logger = logger;

            var connectionString = configuration["Azure:WebPubSub:ConnectionString"] ?? throw new ArgumentException("'Azure:WebPubSub:ConnectionString' not configured.");
            var hubName = configuration["Azure:WebPubSub:HubName"] ?? throw new ArgumentException("'Azure:WebPubSub:HubName' not configured.");

            _webPubSubClient = new WebPubSubServiceClient(connectionString, hubName);
        }

        public string GetClientWssUrl(string callSid)
        {
            return _webPubSubClient.GetClientAccessUri(userId: callSid).ToString();
        }

        public void CloseConnection(string callSid, string closeConnectionReason = null)
        {
            _webPubSubClient.CloseUserConnections(callSid, reason: closeConnectionReason ?? $"{typeof(AzureWebPubSubService).FullName}.{nameof(CloseConnection)}");
        }

        public void SendMessageToCall<T>(T message)
        {
            if (message is IOutboundMessage)
            {
                var callSid = (message as IOutboundMessage).CallSid;

                var jsonMessage = MessageFactory.Serialize(message);

                using (_logger.BeginScope(callSid))
                    _logger.LogDebug($"{jsonMessage}");

                _webPubSubClient.SendToUser(callSid, jsonMessage, "application/json");
            }
        }
    }
}
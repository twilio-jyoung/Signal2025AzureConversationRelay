using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Signal2025AzureConversationRelay.Messages.FromTwilio;
using Signal2025AzureConversationRelay.Messages.ToTwilio;

namespace Signal2025AzureConversationRelay.Messages
{
    public static class MessageFactory
    {
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            DefaultBufferSize = 4096,
            MaxDepth = 32,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        
        public static IInboundMessage Deserialize(string json, string callSid)
        {
            // Step 1: Parse the type property
            string messageTypeString = JsonSerializer.Deserialize<JsonElement>(json, _serializerOptions).GetProperty("type").GetString();

            // Step 2: Map to enum
            if (!Enum.TryParse<InboundMessageType>(messageTypeString, true, out var messageType))
                throw new InvalidOperationException($"Unknown message type: {messageTypeString}");

            // Step 3: Deserialize to the correct type
            IInboundMessage message = messageType switch
            {
                InboundMessageType.Prompt   => JsonSerializer.Deserialize<UserPromptMessage>(json, _serializerOptions),
                InboundMessageType.Setup    => JsonSerializer.Deserialize<SystemSetupMessage>(json, _serializerOptions),
                InboundMessageType.Interrupt=> JsonSerializer.Deserialize<UserInterruptMessage>(json, _serializerOptions),
                InboundMessageType.DTMF     => JsonSerializer.Deserialize<UserDTMFMessage>(json, _serializerOptions),
                InboundMessageType.Error    => JsonSerializer.Deserialize<SystemErrorMessage>(json, _serializerOptions),
                _ => throw new InvalidOperationException($"Unhandled message type: {messageTypeString}")
            };

            message.CallSid = callSid;

            return message;
        }

        public static string Serialize<T>(T message)
        {
            return JsonSerializer.Serialize(message, _serializerOptions);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _serializerOptions);
        }   
    }
}
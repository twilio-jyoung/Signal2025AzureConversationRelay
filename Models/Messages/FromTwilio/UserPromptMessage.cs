using System.Text.Json.Serialization;

/// <summary>
/// Represents the message sent to your app when a user speaks in the conversation.
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#prompt-message
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.FromTwilio{
    public class UserPromptMessage : IInboundMessage
    {
        public string CallSid { get; set; }
        public InboundMessageType Type { get; } = InboundMessageType.Prompt;
        // a complete utterance spoken by the user (inbound tokens are not streamed)
        public string VoicePrompt { get; set; }
        // see supported languages here:
        // https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#default-voice-settings
        [JsonPropertyName("lang")]
        public string Language { get; set; }
        public bool Last { get; set; }
    }
}
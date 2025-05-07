/// <summary>
/// Represents an error.  Usually sent when your app sends an invalid request to the Twilio Conversation Relay service.
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#error-message
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.FromTwilio{
    public class SystemErrorMessage : IInboundMessage
    {
        public string CallSid { get; set; }
        public InboundMessageType Type { get; } = InboundMessageType.Error;
        public string Description { get; set; }
    }
}

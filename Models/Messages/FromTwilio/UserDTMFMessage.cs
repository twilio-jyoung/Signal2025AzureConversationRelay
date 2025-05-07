/// <summary>
/// Represents a message containing DTMF (Dual-Tone Multi-Frequency) digits sent by a user in a conversation.
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#dtmf-message
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.FromTwilio{
    public class UserDTMFMessage : IInboundMessage
    {
        public string CallSid { get; set; }
        public InboundMessageType Type { get; } = InboundMessageType.DTMF;
        // serializer/deserializer will convert the string from twilio to an int
        public int Digit { get; set; }
    }
}

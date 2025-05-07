/// <summary>
/// Represents the event conversation relay sends to your app when a user interrupts the conversation.
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#interrupt-message
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.FromTwilio{
    public class UserInterruptMessage : IInboundMessage
    {
        public string CallSid { get; set; }
        public InboundMessageType Type { get; } = InboundMessageType.Interrupt;
        public string UtteranceUntilInterrupt { get; set; }
        public int DurationUntilInterruptMs { get; set; }
    }
}
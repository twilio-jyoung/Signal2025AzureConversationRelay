namespace Signal2025AzureConversationRelay.Messages.FromTwilio
{
    public interface IInboundMessage
    {
        string CallSid { get; set; }
        InboundMessageType Type { get; }
    }
}
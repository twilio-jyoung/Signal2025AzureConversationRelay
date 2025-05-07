namespace Signal2025AzureConversationRelay.Messages.ToTwilio
{
    public interface IOutboundMessage
    {
        string CallSid { get; set; }
        OutboundMessageType Type { get; }
    }
}
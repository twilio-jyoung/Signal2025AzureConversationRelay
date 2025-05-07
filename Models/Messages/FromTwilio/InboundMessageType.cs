namespace Signal2025AzureConversationRelay.Messages.FromTwilio
{
    /// <summary>
    /// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#messages-from-conversationrelay-to-your-application
    /// </summary>
    public enum InboundMessageType
    {
        Setup,
        Interrupt,
        DTMF,
        Prompt,
        Error
    }
}

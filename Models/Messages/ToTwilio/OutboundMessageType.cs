/// <summary>
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#messages-from-your-application-to-conversationrelay
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.ToTwilio
{
    public enum OutboundMessageType
    {
        Text,
        Play,
        SendDigits,
        Language,
        End
    }
}
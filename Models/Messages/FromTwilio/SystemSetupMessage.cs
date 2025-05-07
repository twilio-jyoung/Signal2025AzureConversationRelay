using System.Collections.Generic;

/// <summary>
/// Represents a system setup event message in the Twilio Conversation Relay system.
/// This message is used to initialize a conversation with system-level information such as call details,
/// participant information, and custom parameters.
/// 
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#setup-message
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.FromTwilio{
    public class SystemSetupMessage : IInboundMessage
    {
        public InboundMessageType Type { get; set; } = InboundMessageType.Setup;
        public string SessionId { get; set; }
        public string CallSid { get; set; }
        public string ParentCallSid { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string ForwardedFrom { get; set; }
        public string CallerName { get; set; }
        public string Direction { get; set; }
        public string CallType { get; set; }
        public string CallStatus { get; set; }
        public string AccountSid { get; set; }
        public Dictionary<string, string> CustomParameters { get; set; }
    }
}

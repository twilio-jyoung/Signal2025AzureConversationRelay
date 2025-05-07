using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the payload to send to Twilio when you want take a text response from the assistant 
/// and convert it to speech for the user on the phone call.
/// 
/// This message can be generated once the assistant has completely responded,
/// or it can be used to send a partial responses as the assistant is generating it.
/// 
/// For voice applications, it is best to send the message as the assistant is generating it.
/// 
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#text-tokens-message
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.ToTwilio
{
    public class SendTokenMessage : IOutboundMessage
    {
        [JsonIgnore]
        public string CallSid { get; set; }
        public OutboundMessageType Type { get; }
        public string Token { get; set; }
        public bool Last { get; set; }
        public SendTokenMessage(string callSid, string token, bool last = false)
        {
            CallSid = callSid ?? throw new ArgumentNullException(nameof(callSid), "CallSid cannot be null.");
            Type = OutboundMessageType.Text;
            Token = token;
            Last = last;
        }
    }
}
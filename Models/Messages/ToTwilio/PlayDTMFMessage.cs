using System;
using System.Text.Json.Serialization;

/// <summary>
/// Sends a DTMF tone to Twilio to be played to the user. Could be used to navigate an IVR system or enter a code.
/// 
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#send-digits-message
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.ToTwilio
{
    public class PlayDTMFMessage : IOutboundMessage
    {
        [JsonIgnore]
        public string CallSid { get; set; }
        public OutboundMessageType Type { get; }
        // https://www.twilio.com/docs/voice/twiml/play#attributes-digits
        // string of digits to send over the call, use w to wait for .5 seconds between digits
        public string Digits { get; set; }

        public PlayDTMFMessage(string callSid, string digits)
        {
            CallSid = callSid ?? throw new ArgumentNullException(nameof(callSid), "CallSid cannot be null.");
            Type = OutboundMessageType.SendDigits;
            Digits = digits;
        }
    }
}
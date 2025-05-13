using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Signal2025AzureConversationRelay.Models.Messages.ToTwilio;

namespace Signal2025AzureConversationRelay.Messages.ToTwilio
{
    /// <summary>
    /// End the session and return control of the call to Twilio through ConversationRelay.
    /// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#end-session-message
    /// </summary>
    public class EndSessionMessage : IOutboundMessage
    {
        [JsonIgnore]
        public string CallSid { get; set; }
        public OutboundMessageType Type => OutboundMessageType.End;

        // Handoff data is JSON that you define which will be passed to the connect action url
        // https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#connect-action-url-callback
        [JsonPropertyName("handoffData")]
        [JsonConverter(typeof(EscapedJsonConverter<EndSessionMessageHandoffData>))]
        public EndSessionMessageHandoffData EndSessionMessageHandoffData { get; set; }

        public EndSessionMessage(string callSid, EndSessionMessageHandoffData handoff) // 
        {
            CallSid = callSid ?? throw new ArgumentNullException(nameof(callSid), "CallSid cannot be null.");
            // Set the message type to End
            // This is used by the Twilio Conversation Relay service to determine how to handle the message
        
            // If handoffData is null, initialize it to an empty dictionary
            EndSessionMessageHandoffData = handoff;
        }
        public override string ToString()
        {
            return MessageFactory.Serialize(this);
        }
    }
}
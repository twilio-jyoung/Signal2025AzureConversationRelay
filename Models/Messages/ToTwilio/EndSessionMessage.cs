using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

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
        public OutboundMessageType Type { get; }

        // Handoff data is JSON that you define which will be passed to the connect action url
        // https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#connect-action-url-callback
        // 
        // we highly recommend defining a schema for the various types of ways you plan to end sessions
        [JsonIgnore]
        public Dictionary<string, object> HandoffData { get; set; }

        [JsonPropertyName("handoffData")]
        public string SerializedHandoffData => MessageFactory.Serialize<Dictionary<string, object>>(HandoffData);

        public EndSessionMessage() {}
        public EndSessionMessage(string callSid, Dictionary<string, object> handoffData = null)
        {
            CallSid = callSid ?? throw new ArgumentNullException(nameof(callSid), "CallSid cannot be null.");
            // Set the message type to End
            // This is used by the Twilio Conversation Relay service to determine how to handle the message
        
            Type = OutboundMessageType.End;
            // If handoffData is null, initialize it to an empty dictionary
            HandoffData = handoffData ?? new Dictionary<string, object>();
        }
    }
}
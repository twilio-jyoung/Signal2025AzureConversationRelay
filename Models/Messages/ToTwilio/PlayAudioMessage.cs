using System;
using System.Text.Json.Serialization;

/// <summary>
/// Sends an audio file to be played in the call.
/// 
/// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#play-media-message
/// </summary>
namespace Signal2025AzureConversationRelay.Messages.ToTwilio
{
    public class PlayAudioMessage : IOutboundMessage
    {
        [JsonIgnore]
        public string CallSid { get; set; }
        public OutboundMessageType Type { get; }
        public Uri Source { get; set; }
        public int Loop { get; set; }
        // set to true to allow subsequent Say or Play messages to stop the playback
        public bool Preemptible { get; set; }

        public PlayAudioMessage(string callSid, Uri source, int loop = 1, bool preemptible = false)
        {
            Type = OutboundMessageType.Play;
            CallSid = callSid ?? throw new ArgumentNullException(nameof(callSid), "CallSid cannot be null.");
            Source = source ?? throw new ArgumentNullException(nameof(source), "Source cannot be null.");
            Loop = loop;
            Preemptible = preemptible;
        }
    }
}
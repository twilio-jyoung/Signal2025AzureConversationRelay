using System;
using System.Text.Json.Serialization;

namespace Signal2025AzureConversationRelay.Messages.ToTwilio
{
    public class SwitchLanguageMessage : IOutboundMessage
    {
        [JsonIgnore]
        public string CallSid { get; set; }
        public OutboundMessageType Type { get; }
        public string TtsLanguage { get; set; }
        public string TranscriptionLanguage { get; set; }
        public SwitchLanguageMessage(string callSid, string ttsLanguage, string transcriptionLanguage)
        {
            CallSid = callSid ?? throw new ArgumentNullException(nameof(callSid), "CallSid cannot be null.");
            Type = OutboundMessageType.Language;
            TtsLanguage = ttsLanguage;
            TranscriptionLanguage = transcriptionLanguage;
        }
        public override string ToString()
        {
            return MessageFactory.Serialize(this);
        }
    }
}
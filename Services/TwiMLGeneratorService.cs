using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace Signal2025AzureConversationRelay.Services
{
    public class TwiMLGeneratorService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly AzureWebPubSubService _azureWebPubSubService;

        public TwiMLGeneratorService(ILogger<TwiMLGeneratorService> logger, IConfiguration configuration, AzureWebPubSubService azureWebPubSubService)
        {
            _logger = logger;
            _configuration = configuration;
            _azureWebPubSubService = azureWebPubSubService;
        }

        public VoiceResponse GenerateConversationRelayTwiML(string callSid, Dictionary<string, string> additionalParameters = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(callSid);

            // get the Azure Web PubSub client URL
            var azureWebPubSubClientUrl = _azureWebPubSubService.GetClientWssUrl(callSid);
            
            if (string.IsNullOrEmpty(azureWebPubSubClientUrl))
                throw new InvalidOperationException("Failed to get Azure Web PubSub client URL.");
            
            using (_logger.BeginScope(callSid))
                _logger.LogTrace("Azure Web PubSub client URL: {URL}", azureWebPubSubClientUrl);

            // setup the action callback URL for the TwiML response
            // https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#connect-action-url-callback
            var actionUrl = new Uri($"/api/calls/actionCallback", UriKind.Relative);
            var connect = new Connect(action: actionUrl);

            // setup the conversation relay attributes
            // https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#conversationrelay-attributes
            var path = "Twilio:ConversationRelay:TwiML:";
            var conversationRelay = new ConversationRelay
            {
                // Event Settings
                Url = azureWebPubSubClientUrl,
                Interruptible = bool.Parse(_configuration[$"{path}Interruptible"]),
                DtmfDetection = bool.Parse(_configuration[$"{path}DtmfDetection"]),
                WelcomeGreeting = _configuration[$"{path}WelcomeGreeting"] ?? "Conversation Relay is now connected.",

                // Transcription or STT Settings
                TranscriptionLanguage = _configuration[$"{path}TranscriptionLanguage"] ?? "en-US",
                TranscriptionProvider = _configuration[$"{path}TranscriptionProvider"] ?? "Google",
                SpeechModel = _configuration[$"{path}TranscriptionSpeechModel"] ?? "telephony",

                // Synthesis or TTS Settings
                TtsLanguage = _configuration[$"{path}TTSLanguage"] ?? "en-US",
                TtsProvider = _configuration[$"{path}TTSProvider"] ?? "Google",
                Voice = _configuration[$"{path}TTSVoice"] ?? "en-US-Journey-O",

                // Debugging Setting
                Debug = bool.Parse(_configuration[$"{path}Debug"])
            };

            if(additionalParameters != null){
                foreach (var param in additionalParameters)
                    conversationRelay.Parameter(param.Key, param.Value);
            }

            var response = new VoiceResponse();
            response.Append(connect.Append(conversationRelay));

            return response;
        }

        public VoiceResponse GenerateHangupTwiML()
        {
            var response = new VoiceResponse();
            response.Hangup();

            return response;
        }

        public VoiceResponse GenerateSayAndHangupTwiML(string message)
        {
            var response = new VoiceResponse();
            response.Say(message);
            response.Hangup();

            return response;
        }

        public VoiceResponse GenerateEnqueueToFlexWorkflowTwiML(string workflowSid)
        {
            var response = new VoiceResponse();
            response.Enqueue(workflowSid: workflowSid);
            return response;
        }

        /// <summary>
        /// Creates an HTTP response with the provided TwiML.
        /// </summary> 
        public HttpResponseData CreateTwiMLHttpResponse(HttpRequestData req, VoiceResponse twiml)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/xml");
            response.WriteString(twiml.ToString(), encoding: Encoding.UTF8);
            
            return response;
        }
    }
}
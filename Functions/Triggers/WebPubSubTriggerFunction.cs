using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Signal2025AzureConversationRelay.Entities;
using Signal2025AzureConversationRelay.Messages;
using Signal2025AzureConversationRelay.Messages.FromTwilio;
using Twilio.TwiML.Messaging;

namespace Signal2025AzureConversationRelay
{
    public class WebPubSubUserEventTriggerFunction
    {
        /// <summary>
        /// Azure WebPubSub will forward messages from Conversation Relay to this function.
        /// https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#messages-from-conversationrelay-to-your-application
        /// 
        /// This function will then raise an event to the Durable Orchestrator function that is running for this call.
        /// The event name is "ConversationRelayMessage" and the payload is the raw message from Twilio.
        /// </summary>
        [Function(nameof(WebPubSubUserEventTriggerFunction))]
        public async Task<IActionResult> RunAsync(
            [WebPubSubTrigger("Hub", WebPubSubEventType.User, "message")] UserEventRequest request,
            [DurableClient] DurableTaskClient dtClient,
            FunctionContext context
        )
        {
            var _logger = context.GetLogger(context.FunctionDefinition.Name);
            var callSid = request.ConnectionContext.UserId;
            var json = request.Data.ToString();
            
            using (_logger.BeginScope(callSid))
            {
                // log the event
                _logger.LogDebug($"{json}");

                // deserialize the message to get the type
                var message = MessageFactory.Deserialize(json, callSid);

                // we need to bypass the orchestrator (which executes serially in a loop) for the interrupt message
                // as it needs to be handled immediately.  This is a special case.
                if (message.Type == InboundMessageType.Interrupt)
                {
                    _logger.LogDebug("UserInterruptMessage detected.  Bypassing orchestrator.");
                    var interruptMessage = (UserInterruptMessage)message;

                    var interruptMessageLog = new StringBuilder();
                    interruptMessageLog.AppendLine("The customer interrupted your response before it could be completely read to them.");
                    interruptMessageLog.AppendLine($"They only heard up to when you said: {interruptMessage.UtteranceUntilInterrupt}");

                    await dtClient.Entities.SignalEntityAsync(
                        new EntityInstanceId(nameof(ConversationHistoryEntity), callSid),
                        nameof(ConversationHistoryEntity.AddSystemMessage),
                        interruptMessageLog.ToString()
                    );
                    
                    try
                    {
                        _logger.LogDebug("Terminating the PromptOrchestrator instance for callSid: {callSid}", callSid);
                        await dtClient.TerminateInstanceAsync(callSid.Substring(2));
                    }
                    catch (Exception) { } // may already be terminated, so ignore the error
                }
                else
                {
                    // raise the event to the running orchestrator, returns when successfully queued
                    await dtClient.RaiseEventAsync(
                        instanceId: callSid,
                        eventName: message.GetType().Name,
                        eventPayload: message
                    );
                }

                return new OkObjectResult("Ack");
            }
        }
    }
}

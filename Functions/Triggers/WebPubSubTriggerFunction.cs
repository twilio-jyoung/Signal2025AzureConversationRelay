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
                    try
                    {
                        _logger.LogDebug("Interrupt message received.  Terminating the prompt sub-orchestrator if running.");
                        var interruptMessage = (UserInterruptMessage)message;
                        await HandleInterrupt(dtClient, interruptMessage);
                    }
                    catch (Exception) { } // swallow exceptions here as the prompt sub-orchestrator may already be terminated
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

        private async Task HandleInterrupt(DurableTaskClient dtClient, UserInterruptMessage interruptMessage)
        {
            var interruptMessageLog = new StringBuilder();
            interruptMessageLog.Append("The customer interrupted your response before it could be completely read to them. ");
            interruptMessageLog.Append($"They only heard up to when you said: '{interruptMessage.UtteranceUntilInterrupt}'.");

            await dtClient.Entities.SignalEntityAsync(
                new EntityInstanceId(nameof(ConversationHistoryEntity), interruptMessage.CallSid),
                nameof(ConversationHistoryEntity.AddSystemMessage),
                interruptMessageLog.ToString()
            );

            var promptSubOrchestratorInstance = await dtClient.GetInstanceAsync(interruptMessage.CallSid.Substring(2));
            if (promptSubOrchestratorInstance.IsRunning)
                await dtClient.TerminateInstanceAsync(promptSubOrchestratorInstance.InstanceId);
        }
    }
}

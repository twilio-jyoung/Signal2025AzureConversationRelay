using System;
using Signal2025AzureConversationRelay.Messages.ToTwilio;
using Signal2025AzureConversationRelay.Models.Messages.ToTwilio;

/// <summary>
/// When you call an activity function, you're actually just scheduling the execution with some parameters.
/// The parameters that you pass into it is serialized / deserialized in the scheduling process.
/// Because of this, we cannot reuse the EndSessionMessage class because of the decorators that are used.
/// to make it adhere to the format expected by Twilio.  To get around this, 
/// we need essentially build a duplicate class with the same properties, but no serialization decorators.
/// 
/// If there's a better way to do this, please submit a PR.
/// </summary>
public class EndSessionActivityFunctionParams
{
    public string CallSid { get; set; }
    public OutboundMessageType Type { get; }

    // Handoff data is JSON that you define which will be passed as a json string to the connect action url
    // https://www.twilio.com/docs/voice/twiml/connect/conversationrelay#connect-action-url-callback
    public EndSessionMessageHandoffData EndSessionMessageHandoffData { get; set; }

    public EndSessionActivityFunctionParams(string callSid, EndSessionMessageHandoffData endSessionMessageHandoffData) // 
    {
        CallSid = callSid ?? throw new ArgumentNullException(nameof(callSid), "CallSid cannot be null.");
        Type = OutboundMessageType.End;
        EndSessionMessageHandoffData = endSessionMessageHandoffData;
    }

    public EndSessionMessage ToEndSessionMessage()
    {
        return new EndSessionMessage(CallSid, EndSessionMessageHandoffData);
    }
}
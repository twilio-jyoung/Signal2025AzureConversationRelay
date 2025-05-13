using System;

namespace Signal2025AzureConversationRelay.Models.Messages.ToTwilio
{

    public class EndSessionMessageHandoffData
    {
        public EndSessionAction EndSessionAction { get; set; }
        public EscalationReason? EscalationReason { get; set; }
        public string CallSummary { get; set; }
        public EndSessionMessageHandoffData(){}
        public EndSessionMessageHandoffData(EndSessionAction action, EscalationReason? escalationReason = null, string callSummary = "")
        {
            if(EndSessionAction == EndSessionAction.Escalate && escalationReason == null)
                throw new ArgumentNullException(nameof(escalationReason), "EscalationReason cannot be null when EndSessionAction is Escalate.");

            EndSessionAction = action;
            EscalationReason = escalationReason;
            CallSummary = callSummary;
        }
    }

    public enum EndSessionAction
    {
        Hangup,
        Escalate
    }

    public enum EscalationReason
    {
        UserRequested,
        SystemRequested
    }
}
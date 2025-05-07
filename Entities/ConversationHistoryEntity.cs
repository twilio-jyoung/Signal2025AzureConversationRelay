using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Entities;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Signal2025AzureConversationRelay.Entities
{
    public class ConversationHistoryEntity : TaskEntity<ChatHistory>
    {
        private string setupPrompt = new StringBuilder()
                .AppendLine("You're a helpful assistant.")
                .AppendLine("A user will speak a message over the phone, and you will receive it as text.")
                .AppendLine("You will respond with text, and the user will hear your response as speech.")
                .AppendLine("To the user, the conversation will feel like a natural phone call.")
                .AppendLine("You must begin streaming a response immediately after the user speaks.")
                .AppendLine("Keep responses short and concise.")
                .AppendLine("If you know the user's name, you can use it in your responses, but dont use it too much as this can be annoying.")
                .AppendLine("optimize the text you reply with for the user to hear it as speech.")
                .AppendLine("optimize the text you reply with to sound upbeat and positive.").ToString();

        private string RunningState = "Waiting for Initialization";
        protected override ChatHistory InitializeState(TaskEntityOperation operation){
            RunningState = "Initializing State";
            var history = new ChatHistory();
            history.AddSystemMessage(setupPrompt);
            RunningState = "Ready";
            return history;
        }
        public ChatHistory GetConversationHistory(){
            return State;
        }
        public ChatHistory AddUserMessage(string message)
        {
            RunningState = "Adding User Message";
            State.AddUserMessage(message);
            RunningState = "Ready";
            return State;
        }
        public void AddAssistantMessage(string message)
        {
            RunningState = "Adding Assistant Message";
            State.AddAssistantMessage(message);
            RunningState = "Ready";
        }
        public void AddSystemMessage(string message)
        {
            RunningState = "Adding System Message";
            State.AddSystemMessage(message);
            RunningState = "Ready";
        }
        public void AddDeveloperMessage(string message)
        {
            RunningState = "Adding Developer Message";
            State.AddDeveloperMessage(message);
            RunningState = "Ready";
        }
        public void ClearHistory()
        {
            RunningState = "Clearing History";
            State = new ChatHistory();

        }
        public int GetMessageCount()
        {
            return State.Count;
        }
        public void RemoveLastMessage()
        {
            if (State.Count > 0)
                State.RemoveAt(State.Count - 1);
        }
        public void Delete()
        {
            RunningState = "Deleting Entity";
            State = null;
            RunningState = "Deleted";
        }
        [Function(nameof(ConversationHistoryEntity))]
        public static Task Run([EntityTrigger] TaskEntityDispatcher dispatcher)
        {
            return dispatcher.DispatchAsync<ConversationHistoryEntity>();
        }
    }
}
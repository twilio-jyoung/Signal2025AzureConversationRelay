using Microsoft.SemanticKernel.ChatCompletion;
using Signal2025AzureConversationRelay.Messages.FromTwilio;

public class HandlePromptActivityFunctionParams
{
    public ChatHistory ChatHistory { get; set; }
    public UserPromptMessage UserPromptMessage { get; set; }
}
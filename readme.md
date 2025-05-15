![banner](/Assets/cr-azure.png)

## Pre-requisites:

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?ref=twiliio.com).
- A Twilio Account. [Create an account for free](https://www.twilio.com/try-twilio?utm-source=signal-jyoung).
  - A phone number
  - Navigate to the **Voice** section, select **General** under **Settings**, and turn on the **Predictive and Generative AI/ML Features Addendum** in order to use ConversationRelay.
- Development Environment for .NET 9 (one option below)
  - [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
  - [Visual Studio Code](https://code.visualstudio.com/Download)
  - [C\# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) for Visual Studio Code.
  - [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions) for Visual Studio Code.
  - [Azurite extension](https://marketplace.visualstudio.com/items?itemName=Azurite.azurite) for Visual Studio Code.
  - [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd)
- [ngrok](https://ngrok.com/download?utm-source=signal-jyoung)

## Deploy Azure Resources

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Ftwilio-jyoung%2FSignal2025AzureConversationRelay%2F39fe8b52e9f946737e1ba706ed2c46b56a004af2%2Finfra%2Fmain.json)

_Create a new resource group for easy resource isolation_

## Run the Code

- Setup a local.settings.json file (cp local.settings.json.sample local.settings.json)

  - Azure.OpenAI
    - Navigate to the new resource group and select the resource ending in `-project` then click **Launch Studio**
    - Select 'Models + Endpoints' in **My assets** section, then select a model, then set language to C#
    - Copy the `DeploymentName`, `Endpoint`, and `APIKey` into your settings file
  - Azure.WebPubSub
    - Navigate back to the new resource group and select the resource ending in `-webpubsub`
    - Select **Settings**, then select **Keys**
    - Copy the Primary > `Connection String` into your settings file
    - Set the `HubName` to `cr` if not already set.
  - Azure.KeyVault
    - Navigate back to the new resource group and select the resource ending in `-kv`
    - Copy the `Vault URI` from the top right of the page into you settings file
  - Twilio
    - Copy your `Account SID` and `Auth Token` into your settings file from the [Twilio Account Dashboard](https://console.twilio.com/)

- Ensure SDKs are working

  - `dotnet --version`
    - You should see 9.\*. If you don't you may need to add dotnet to your path.
  - `azd auth login`
    - You should be asked to login via the portal.

- Start Azurite using the Azurite Extention from the Command Palette

  - `>Azurite: Start`

- Run the app from the terminal

  - `func start`

- Open a terminal and start ngrok

  - `ngrok http 7071`
  - make note of the forwarding URL

- Configure your phone number

  - Open your [Active Numbers](https://console.twilio.com/us1/develop/phone-numbers/manage/incoming) on your Twilio Account
    - Set the incoming call handler (**A call comes in**)
      - webhook
      - {ngrok_url}/api/call
      - HTTP POST
    - Set the call status handler (**Call status changes**)
      - {ngrok_url}/api/call-status-update
      - HTTP POST

- Place a call to the number, and check out the [Conversation Relay Docs](https://www.twilio.com/docs/voice/twiml/connect/conversationrelay)

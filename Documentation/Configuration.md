# Tunnel Relay Configuration

As a part of configuration we configure the Azure Relay which will be used to open relay between Azure and local server. This requires user to login with their Azure credentials and select or create the Azure Relay namespace they want to use with Tunnel Relay. We __do not__ store Azure credentials. Configuration is stored in appSettings.json file in the Tunnel Relay folder. 

### What is stored?
As a part of configuration we store shared access key for the selected Azure Relay. Refer [this](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-authentication-and-authorization) document for more information. This key allows access to selected Azure Relay and no other resource in the Azure subscription.

Along with Azure Relay shared access key. We store the redirection url. Redirection url refers to the url to which all incoming requests are routed to. This is usually a local server url in the format of http(s)://localhost:portnumber. Default is http://localhost:8080. This can be changed from the GUI post login at any point. Command-line requires you to delete the configuration using `--DeleteConfiguration` option.

Plugin data is also stored as a part of configuration along with the list of enabled plugins. This ensures that users don't have to enable or configure plugins every time they start the application. You can read about plugins more [here](PluginManagement.md).

Stored shared access key for the Azure Relay is encrypted using [DPAPI](https://msdn.microsoft.com/en-us/library/ms995355.aspx) on Windows thus any one with access to the machine can decrypt it. You are however allowed during configuration process to
allow storing the key without encryption. This can be useful if you want to skip logging in while using a different machine. *Disabling encryption can allow someone to copy the key from your config and perform operations against it.*

Shared key encryption is not yet supported in non-Windows OSes.

### How do I change selected Azure Relay?
Signing out of Tunnel Relay will remove the Azure Relay information. You can restart the application to login again and select a Azure Relay.

## Configuration process explained

### Diagram
![Relay Configuration](Configuration.png "Tunnel Relay Configuration")

### Details

1. Process begins with user clicking on __'Login with Azure'__ button to launch Azure login prompt. This allows the application to access user's Azure resources on its behalf. We utilize [MSAL](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) and [Azure Management Libraries](https://github.com/Azure/azure-sdk-for-net/tree/Fluent) to get list of user resources.
2. Once the user has selected the subscription, user is presented with an option to either select an existing [Azure Relay](https://azure.microsoft.com/en-us/services/service-bus/) namespace or create a new one.
3. Based on user selection in option 2, Azure Relay is either created or fetched.
4. As a last step, we get the shared access key for the Azure Relay with permission set to Send, Listen, Manage. This key is permanently stored in the config (appSettings.json) for future use. All other tokens are discarded and application at no point in the future will access any Azure resource except the selected Azure Relay.
5. Once the configuration is completed, application main window is launched. Traffic can now be routed to your application from the internet using the url shown on the application.

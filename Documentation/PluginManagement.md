# Tunnel relay plugins

Tunnel relay allows developers to route requests to a local server. Though there are cases where local servers handle request differently as compared to production machines. This is usually true in the case how authentication is done. For example, production server might support a different Azure Active directory token as compared to local server. This makes it hard to redirect requests from production clients\services to local server as authentication fails to work. Authentication is one such case. There can be multiple such cases dealing with differences between local deployments and production deployments. We developed a plugin engine for Tunnel Relay which allows you to process request before it is send to your hosted service and process response before it is sent back to caller.

## How to build plugin
Use the TunnelRelay.Plugins nuget package from releases. This library contains ITunnelRelayPlugin interface. Any class implementing this interface can be loaded into TunnelRelay separately without the need to be compiled along with Tunnel relay.

### Understanding ITunnelRelayPlugin interface
ITunnelRelayPlugin interface is defined as follows
```cs
    /// <summary>
    /// Interface for developing plugins.
    /// </summary>
    public interface ITunnelRelayPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// Gets the help text.
        /// </summary>
        string HelpText { get; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <returns>Task tracking operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Performes required processing before request is made to service asynchronously.
        /// </summary>
        /// <param name="webRequest">The web request.</param>
        /// <returns>Processed http web request.</returns>
        Task<HttpRequestMessage> PreProcessRequestToServiceAsync(HttpRequestMessage webRequest);

        /// <summary>
        /// Performes required processing after response is received from service asynchronously.
        /// </summary>
        /// <param name="webResponse">The web response.</param>
        /// <returns>Processed http web response.</returns>
        Task<HttpResponseMessage> PostProcessResponseFromServiceAsync(HttpResponseMessage webResponse);
    }
```
- `PreProcessRequestToServiceAsync` - This method is executed _before_ call is made to hosted service and allows modification of this request, like adding\removing headers etc.
- `PostProcessResponseFromServiceAsync` - This method is executed _after_ response is received from hosted service and _before_ it is sent to caller allowing modifications to outgoing response.
- `PluginName` - This is the name of your plugin and is shown in plugin management UI (see below) and well as used to store plugin settings.
- `InitializeAsync` - This method can be used to initialize your plugin state. This is called when user enables your plugin.
- `HelpText` - Describes what the plugin does. Is shown on the UI and is supposed to assist user on what operation the plugin performs.

### Taking settings for plugins
Plugins can require settings to be provided by the user. PluginSetting attribute allows you to specify the setting which is to be passed by the user and is auto populated in the UI. 

PluginSetting attribute is defined as follows
```cs
    /// <summary>
    /// Plugin settings.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PluginSettingAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginSettingAttribute"/> class.
        /// </summary>
        /// <param name="displayName">The display name to be shown to user..</param>
        /// <param name="helpText">The help text to be shown on hover..</param>
        public PluginSettingAttribute(string displayName, string helpText)
        {
            this.DisplayName = displayName;
            this.HelpText = helpText;
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Gets the help text.
        /// </summary>
        public string HelpText { get; private set; }
    }
```
This attribute can only be applied on **string public properties**.

- DisplayName - Display name of the settings
- HelpText - Help text guiding user on how to specify the value.


## Loading and using plugins
At application start Tunnel Relay checks **Plugins** directory in root application directory for all assemblies implementing _ITunnelRelayPlugin_ interace. It then creates an instance for all these plugins and intializes the required plugins. Plugin lifetime is managed as follows

1. Explore \Plugins directory and create an instance of the plugin
2. If user enabled the plugin in last session all settings are set and Plugin.Initialize is called in a background thread.
3. During request processing all enabled plugins are called as described above.

### Enabling, disabling, and providing settings for Plugins
In the application main window. Clicking on __Plugin Management__ button will open the following UI will all the plugins loaded (enabled or not enabled) in the left column. User can then select individual plugin, enable, or disable them, or change settings. All these settings are auto persisted and applied in next application start.

![Plugin Management](PluginManagement.png "Plugin Management UI")

### Points to note
- Plugins are only loaded at application start, so ensure your dll is in Plugins directory in root application directory.
- Error during plugin load and initialize are all ignored and specific plugin won’t be loaded.
- If a plugin fails to process a request and throws an exception, this exception will get sent to caller.

## FAQs
**Q.** Are any plugins included in Tunnel Relay for example purposes? </br>
**A.** Tunnel Relay includes 2 plugins HeaderAdditionPlugin and HeaderRemovalPlugin to serve as examples on how plugins can be developed.

**Q.** Do plugins need to be published anywhere to work? </br>
**A.** No, plugins just need to be placed in Plugins directory in application's root directory to work.

**Q.** Can multiple plugins be enabled at once? </br>
**A.** Yes, they will be called one by one in no fixed order. So, ensure your plugin does not assume any order of calling.

**Q.** How many plugins can be loaded in Tunnel Relay? </br>
**A.** There is no limit on how many plugins can be loaded. Though directory search loading plugin takes time, so too many plugins can slow the application during launch and request processing.

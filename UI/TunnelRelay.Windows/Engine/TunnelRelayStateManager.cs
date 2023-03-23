// <copyright file="TunnelRelayStateManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using TunnelRelay.Core;
    using TunnelRelay.UI.PluginManagement;
    using TunnelRelay.UI.StateManagement;

    /// <summary>
    /// Maintains the state of application.
    /// </summary>
    internal static class TunnelRelayStateManager
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private static readonly ILogger Logger = LoggingHelper.GetLogger<string>();

        /// <summary>
        /// Current application data instance.
        /// </summary>
        private static ApplicationData applicationData;

        /// <summary>
        /// The current connection manager.
        /// </summary>
        private static HybridConnectionManager hybridConnectionManager;

        /// <summary>
        /// Gets or sets the relay request event listener.
        /// </summary>
        public static IRelayRequestEventListener RelayRequestEventListener { get; set; }

        /// <summary>
        /// Gets or sets the list of plugins.
        /// </summary>
        public static ObservableCollection<PluginDetails> Plugins { get; set; } = new ObservableCollection<PluginDetails>();

        /// <summary>
        /// Gets or sets the application data.
        /// </summary>
        public static ApplicationData ApplicationData
        {
            get
            {
                if (applicationData == null)
                {
                    if (File.Exists("appSettings.json"))
                    {
                        TunnelRelayStateManager.Logger.LogInformation("Loading existing settings");
                        TunnelRelayStateManager.applicationData = JsonConvert.DeserializeObject<ApplicationData>(File.ReadAllText("appSettings.json"));
                    }
                    else
                    {
                        TunnelRelayStateManager.Logger.LogInformation("Appsettings don't exist. Creating new one.");
                        TunnelRelayStateManager.applicationData = new ApplicationData
                        {
                            RedirectionUrl = "http://localhost:8080/",
                        };
                    }
                }

                return TunnelRelayStateManager.applicationData;
            }

            set
            {
                applicationData = value;
            }
        }

        /// <summary>
        /// Gets the Options monitor to update redirect url.
        /// </summary>
        public static SimpleOptionsMonitor<RelayRequestManagerOptions> RelayRequestManagerOptions { get; } =
            new SimpleOptionsMonitor<RelayRequestManagerOptions>();

        /// <summary>
        /// Initializes the plugins.
        /// </summary>
        public static void InitializePlugins()
        {
            PluginManager pluginManager = new PluginManager(LoggingHelper.GetLogger<PluginManager>());
            TunnelRelayStateManager.Plugins = pluginManager.InitializePlugins(TunnelRelayStateManager.applicationData);
        }

        /// <summary>
        /// Starts or restarts the tunnel relay engine.
        /// </summary>
        /// <returns>Task tracking operation.</returns>
        public static async Task StartTunnelRelayAsync()
        {
            // If there is an existing connection close. This is for cases where a plugin was reconfigured\enabled\disabled etc.
            if (TunnelRelayStateManager.hybridConnectionManager != null)
            {
                await TunnelRelayStateManager.hybridConnectionManager.CloseAsync(CancellationToken.None).ConfigureAwait(false);
            }

            if (TunnelRelayStateManager.RelayRequestEventListener == null)
            {
                throw new InvalidOperationException("No request event listener was assigned");
            }

            HybridConnectionManagerOptions hybridConnectionManagerOptions = new HybridConnectionManagerOptions
            {
                ConnectionPath = TunnelRelayStateManager.ApplicationData.HybridConnectionName,
                AzureRelayKeyName = TunnelRelayStateManager.ApplicationData.HybridConnectionKeyName,
                AzureRelaySharedKey = TunnelRelayStateManager.ApplicationData.HybridConnectionSharedKey,
                AzureRelayUrlHost = TunnelRelayStateManager.ApplicationData.HybridConnectionUrl,
            };

            if (string.IsNullOrEmpty(TunnelRelayStateManager.ApplicationData.RedirectionUrl))
            {
                TunnelRelayStateManager.ApplicationData.RedirectionUrl = "http://localhost:8080";
            }

            TunnelRelayStateManager.RelayRequestManagerOptions.CurrentValue = new RelayRequestManagerOptions
            {
                InternalServiceUrl = new Uri(TunnelRelayStateManager.ApplicationData.RedirectionUrl),
            };

#pragma warning disable CA5400 // Ensure HttpClient certificate revocation list check is not disabled - Allowing developers to use invalid certificates
            RelayRequestManager relayManager = new RelayRequestManager(
                new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    AllowAutoRedirect = false,
                }),
                TunnelRelayStateManager.RelayRequestManagerOptions,
                Plugins.Where(details => details.IsEnabled).Select(details => details.PluginInstance),
                LoggingHelper.GetLogger<RelayRequestManager>(),
                RelayRequestEventListener);
#pragma warning restore CA5400 // Ensure HttpClient certificate revocation list check is not disabled - Allowing developers to use invalid certificates

            TunnelRelayStateManager.hybridConnectionManager = new HybridConnectionManager(
                Options.Create(hybridConnectionManagerOptions),
                relayManager);

            await TunnelRelayStateManager.hybridConnectionManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves the settings to file.
        /// </summary>
        public static void SaveSettingsToFile()
        {
            if (TunnelRelayStateManager.applicationData != null)
            {
                File.WriteAllText("appSettings.json", JsonConvert.SerializeObject(TunnelRelayStateManager.ApplicationData, Formatting.Indented));
            }
        }

        /// <summary>
        /// Logouts this instance.
        /// </summary>
        /// <returns>Task tracking operation.</returns>
        public static async Task LogoutAsync()
        {
            TunnelRelayStateManager.Logger.LogInformation("Logging out");

            await TunnelRelayStateManager.ShutdownTunnelRelayAsync().ConfigureAwait(false);
            TunnelRelayStateManager.ApplicationData = new ApplicationData
            {
                RedirectionUrl = "http://localhost:8080/",
            };
        }

        /// <summary>
        /// Shuts the tunnel relay down.
        /// </summary>
        /// <returns>Task tracking operation.</returns>
        public static async Task ShutdownTunnelRelayAsync()
        {
            if (TunnelRelayStateManager.hybridConnectionManager != null)
            {
                await TunnelRelayStateManager.hybridConnectionManager.CloseAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}

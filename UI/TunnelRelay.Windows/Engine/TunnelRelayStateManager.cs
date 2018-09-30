// <copyright file="TunnelRelayStateManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TunnelRelay.Core;
using TunnelRelay.Diagnostics;
using TunnelRelay.PluginEngine;

namespace TunnelRelay.Windows.Engine
{
    internal static class TunnelRelayStateManager
    {
        private static ApplicationData applicationData;

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
                        Logger.LogInfo(CallInfo.Site(), "Loading existing settings");
                        TunnelRelayStateManager.applicationData = JsonConvert.DeserializeObject<ApplicationData>(File.ReadAllText("appSettings.json"));
                    }
                    else
                    {
                        Logger.LogInfo(CallInfo.Site(), "Appsettings don't exist. Creating new one.");
                        TunnelRelayStateManager.applicationData = new ApplicationData
                        {
                            RedirectionUrl = "http://localhost:3979/",
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
        /// Gets or sets the current connection manager.
        /// </summary>
        public static HybridConnectionManager HybridConnectionManager { get; set; }

        /// <summary>
        /// Gets or sets the relay request event listener.
        /// </summary>
        public static IRelayRequestEventListener RelayRequestEventListener { get; set; }

        /// <summary>
        /// Options monitor to update redirect url.
        /// </summary>
        public static SimpleOptionsMonitor<RelayRequestManagerOptions> RelayRequestManagerOptions = 
            new SimpleOptionsMonitor<RelayRequestManagerOptions>();

        /// <summary>
        /// Current connection manager.
        /// </summary>
        private static HybridConnectionManager hybridConnectionManager;

        /// <summary>
        /// Initializes the plugins.
        /// </summary>
        public static void InitializePlugins()
        {
            PluginManager pluginManager = new PluginManager();
            TunnelRelayStateManager.Plugins = pluginManager.InitializePlugins();
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

            HybridConnectionManagerOptions hybridConnectionManagerOptions = new HybridConnectionManagerOptions
            {
                ConnectionPath = TunnelRelayStateManager.ApplicationData.HybridConnectionName,
                ServiceBusKeyName = TunnelRelayStateManager.ApplicationData.HybridConnectionKeyName,
                ServiceBusSharedKey = TunnelRelayStateManager.ApplicationData.HybridConnectionSharedKey,
                ServiceBusUrl = TunnelRelayStateManager.ApplicationData.HybridConnectionUrl,
            };

            TunnelRelayStateManager.RelayRequestManagerOptions.CurrentValue = new RelayRequestManagerOptions
            {
                InternalServiceUrl = new Uri(TunnelRelayStateManager.ApplicationData.RedirectionUrl),
            };

            RelayRequestManager relayManager = new RelayRequestManager(
                TunnelRelayStateManager.RelayRequestManagerOptions,
                Plugins.Where(details => details.IsEnabled).Select(details => details.PluginInstance),
                RelayRequestEventListener);

            TunnelRelayStateManager.hybridConnectionManager = new HybridConnectionManager(
                Options.Create(hybridConnectionManagerOptions),
                relayManager);

            await TunnelRelayStateManager.hybridConnectionManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}

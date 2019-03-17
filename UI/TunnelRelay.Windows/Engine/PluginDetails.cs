// <copyright file="PluginDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Plugin instance details.
    /// </summary>
    public class PluginDetails
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<PluginDetails> logger = LoggingHelper.GetLogger<PluginDetails>();

        /// <summary>
        /// Is this plugin enabled or not. Used to cache the value to avoid probing the list everytime.
        /// </summary>
        private bool? isEnabled;

        /// <summary>
        /// The instance is initialized.
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled
        {
            get
            {
                if (this.isEnabled == null)
                {
                    this.isEnabled = TunnelRelayStateManager.ApplicationData.EnabledPlugins.Contains(this.PluginInstance.GetType().FullName);
                }

                return this.isEnabled.Value;
            }

            set
            {
                if (value == true)
                {
                    TunnelRelayStateManager.ApplicationData.EnabledPlugins.Add(this.PluginInstance.GetType().FullName);
                    this.isEnabled = true;
                }
                else
                {
                    TunnelRelayStateManager.ApplicationData.EnabledPlugins.Remove(this.PluginInstance.GetType().FullName);
                    this.isEnabled = false;
                }
            }
        }

        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public ITunnelRelayPlugin PluginInstance { get; internal set; }

        /// <summary>
        /// Gets the plugin settings.
        /// </summary>
        public ObservableCollection<PluginSettingDetails> PluginSettings { get; internal set; }

        /// <summary>
        /// Initializes the plugin.
        /// </summary>
        public void InitializePlugin()
        {
            if (!this.isInitialized)
            {
                this.logger.LogInformation("Initializing '{0}'.", this.PluginInstance.PluginName);
                Task.Run(() => this.PluginInstance.InitializeAsync());
                this.isInitialized = true;
            }
        }
    }
}

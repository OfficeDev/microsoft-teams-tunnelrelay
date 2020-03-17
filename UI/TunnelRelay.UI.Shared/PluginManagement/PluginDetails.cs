// <copyright file="PluginDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.PluginManagement
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using TunnelRelay.PluginEngine;
    using TunnelRelay.UI.StateManagement;

    /// <summary>
    /// Plugin instance details.
    /// </summary>
    internal class PluginDetails
    {
        /// <summary>
        /// Active instance of application data.
        /// </summary>
        private readonly ApplicationData applicationData;

        /// <summary>
        /// Is this plugin enabled or not. Used to cache the value to avoid probing the list everytime.
        /// </summary>
        private bool? isEnabled;

        /// <summary>
        /// The instance is initialized.
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginDetails"/> class.
        /// </summary>
        /// <param name="applicationData">Application data.</param>
        public PluginDetails(ApplicationData applicationData)
        {
            this.applicationData = applicationData ?? throw new System.ArgumentNullException(nameof(applicationData));
        }

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
                    this.isEnabled = this.applicationData.EnabledPlugins.Contains(this.PluginInstance.GetType().FullName);
                }

                return this.isEnabled.Value;
            }

            set
            {
                if (value == true)
                {
                    this.applicationData.EnabledPlugins.Add(this.PluginInstance.GetType().FullName);
                    this.isEnabled = true;
                }
                else
                {
                    this.applicationData.EnabledPlugins.Remove(this.PluginInstance.GetType().FullName);
                    this.isEnabled = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the plugin instance.
        /// </summary>
        public ITunnelRelayPlugin PluginInstance { get; set; }

        /// <summary>
        /// Gets or sets the plugin settings.
        /// </summary>
        public ObservableCollection<PluginSettingDetails> PluginSettings { get; set; }

        /// <summary>
        /// Initializes the plugin.
        /// </summary>
        public void InitializePlugin()
        {
            if (!this.isInitialized)
            {
                Task.Run(() => this.PluginInstance.InitializeAsync());
                this.isInitialized = true;
            }
        }
    }
}

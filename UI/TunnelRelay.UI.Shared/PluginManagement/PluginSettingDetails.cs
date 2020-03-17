// <copyright file="PluginSettingDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.PluginManagement
{
    using System.Collections.Generic;
    using System.Reflection;
    using TunnelRelay.PluginEngine;
    using TunnelRelay.UI.StateManagement;

    /// <summary>
    /// Plugin settings details.
    /// </summary>
    internal class PluginSettingDetails
    {
        /// <summary>
        /// Gets or sets the plugin settings.
        /// </summary>
        private readonly ApplicationData applicationData;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginSettingDetails"/> class.
        /// </summary>
        /// <param name="applicationData">Application data where settings for plugin are stored.</param>
        public PluginSettingDetails(ApplicationData applicationData)
        {
            this.applicationData = applicationData ?? throw new System.ArgumentNullException(nameof(applicationData));
        }

        /// <summary>
        /// Gets or sets the attribute data.
        /// </summary>
        public PluginSettingAttribute AttributeData { get; set; }

        /// <summary>
        /// Gets or sets the plugin instance.
        /// </summary>
        public ITunnelRelayPlugin PluginInstance { get; set; }

        /// <summary>
        /// Gets or sets the property details.
        /// </summary>
        public PropertyInfo PropertyDetails { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public string Value
        {
            get
            {
                object val = this.PropertyDetails.GetValue(this.PluginInstance);
                return val == null ? string.Empty : val.ToString();
            }

            set
            {
                this.PropertyDetails.SetValue(this.PluginInstance, value);

                if (!this.applicationData.PluginSettingsMap.TryGetValue(this.PluginInstance.GetType().FullName, out Dictionary<string, string> pluginSettings))
                {
                    pluginSettings = new Dictionary<string, string>();
                    this.applicationData.PluginSettingsMap[this.PluginInstance.GetType().FullName] = pluginSettings;
                }

                pluginSettings[this.PropertyDetails.Name] = value;
            }
        }
    }
}

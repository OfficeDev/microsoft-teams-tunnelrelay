// <copyright file="PluginSettingDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
    using System.Collections.Generic;
    using System.Reflection;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Plugin settings details.
    /// </summary>
    public class PluginSettingDetails
    {
        /// <summary>
        /// Gets the attribute data.
        /// </summary>
        public PluginSettingAttribute AttributeData { get; internal set; }

        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public ITunnelRelayPlugin PluginInstance { get; internal set; }

        /// <summary>
        /// Gets the property details.
        /// </summary>
        public PropertyInfo PropertyDetails { get; internal set; }

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

                if (!TunnelRelayStateManager.ApplicationData.PluginSettingsMap.TryGetValue(this.PluginInstance.GetType().FullName, out Dictionary<string, string> pluginSettings))
                {
                    pluginSettings = new Dictionary<string, string>();
                    TunnelRelayStateManager.ApplicationData.PluginSettingsMap[this.PluginInstance.GetType().FullName] = pluginSettings;
                }

                pluginSettings[this.PropertyDetails.Name] = value;
            }
        }
    }
}

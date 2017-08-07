// <copyright file="PluginSettingDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TunnelRelay
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Plugin settings details.
    /// </summary>
    public class PluginSettingDetails
    {
        /// <summary>
        /// Gets the attribute data.
        /// </summary>
        public PluginSetting AttributeData { get; internal set; }

        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public IRedirectionPlugin PluginInstance { get; internal set; }

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

                if (!ApplicationData.Instance.PluginSettingsMap.TryGetValue(this.PluginInstance.GetType().FullName, out Dictionary<string, string> pluginSettings))
                {
                    pluginSettings = new Dictionary<string, string>();
                    ApplicationData.Instance.PluginSettingsMap[this.PluginInstance.GetType().FullName] = pluginSettings;
                }

                pluginSettings[this.PropertyDetails.Name] = value;
            }
        }
    }
}

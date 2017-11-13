// <copyright file="PluginSettingDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
// Licensed under the MIT license.
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace TunnelRelay.Core
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

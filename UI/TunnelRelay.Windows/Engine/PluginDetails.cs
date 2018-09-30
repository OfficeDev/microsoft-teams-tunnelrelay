// <copyright file="PluginDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

namespace TunnelRelay.Windows.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using TunnelRelay.Diagnostics;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Plugin instance details.
    /// </summary>
    public class PluginDetails
    {
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
                Logger.LogInfo(CallInfo.Site(), "Initializing '{0}'.", this.PluginInstance.PluginName);
                Task.Run(() => this.PluginInstance.InitializeAsync());
                this.isInitialized = true;
            }
        }
    }
}

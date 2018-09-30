// <copyright file="PluginManagement.xaml.cs" company="Microsoft Corporation">
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

namespace TunnelRelay.Windows
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using TunnelRelay.Diagnostics;
    using TunnelRelay.Windows.Engine;

    /// <summary>
    /// Interaction logic for PluginManagement.xaml.
    /// </summary>
    public partial class PluginManagement : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginManagement"/> class.
        /// </summary>
        public PluginManagement()
        {
            this.InitializeComponent();
            this.lstPluginList.ItemsSource = TunnelRelayStateManager.Plugins;
            this.lstPluginList.SelectedIndex = 0;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closed" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                Parallel.ForEach(TunnelRelayStateManager.Plugins.Where(plugin => plugin.IsEnabled), (plugin) => plugin.InitializePlugin());

                // Reload if the plugin configuration was changed.
                Task.Run(async () => await TunnelRelayStateManager.StartTunnelRelayAsync().ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                Logger.LogError(CallInfo.Site(), ex);
                MessageBox.Show("Plugin initialization failed!!", "Plugin management", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            }

            base.OnClosed(e);
        }
    }
}

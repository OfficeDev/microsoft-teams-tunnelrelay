// <copyright file="PluginManagement.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.Extensions.Logging;
    using TunnelRelay.Windows.Engine;

    /// <summary>
    /// Interaction logic for PluginManagement.xaml.
    /// </summary>
    public partial class PluginManagement : Window
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<PluginManagement> logger = LoggingHelper.GetLogger<PluginManagement>();

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
        /// Raises the <see cref="Window.Closed" /> event.
        /// </summary>
        /// <param name="e">An <see cref="EventArgs" /> that contains the event data.</param>
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
                this.logger.LogError(ex, "Failed to set plugin settings");
                MessageBox.Show("Plugin initialization failed!!", "Plugin management", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            }

            base.OnClosed(e);
        }
    }
}

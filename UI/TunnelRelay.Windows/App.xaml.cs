// <copyright file="App.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System.Windows;
    using Microsoft.Extensions.Logging;
    using TunnelRelay.Windows.Engine;

    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    internal partial class App : Application
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private ILogger<App> logger = LoggingHelper.GetLogger<App>();

        /// <summary>
        /// Raises the <see cref="Application.Exit" /> event.
        /// </summary>
        /// <param name="e">An <see cref="ExitEventArgs" /> that contains the event data.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            this.logger.LogInformation("Exiting the application with exit code '{0}'", e.ApplicationExitCode);

            TunnelRelayStateManager.SaveSettingsToFile();
            TunnelRelayStateManager.ShutdownTunnelRelayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}

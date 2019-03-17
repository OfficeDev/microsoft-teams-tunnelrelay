// <copyright file="App.xaml.cs" company="Microsoft Corporation">
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
    using System.Threading;
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

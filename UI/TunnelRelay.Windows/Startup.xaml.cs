// <copyright file="Startup.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System.Windows;
    using Microsoft.Extensions.Logging;
    using TunnelRelay.Windows.Engine;

    /// <summary>
    /// Interaction logic for Startup.xaml.
    /// </summary>
    public partial class Startup : Window
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<Startup> logger = LoggingHelper.GetLogger<Startup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        public Startup()
        {
            this.InitializeComponent();

            if (string.IsNullOrEmpty(TunnelRelayStateManager.ApplicationData.HybridConnectionUrl))
            {
                this.logger.LogInformation("Starting welcome experiance");
                LoginToAzure gettingStarted = new LoginToAzure();
                gettingStarted.Show();
            }
            else
            {
                this.logger.LogInformation("User is logged in already. Starting app directly");
                new MainWindow().Show();
            }

            this.Close();
        }
    }
}

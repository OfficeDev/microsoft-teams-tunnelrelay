// <copyright file="LoginToAzure.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Navigation;
    using Microsoft.Extensions.Logging;
    using Microsoft.Win32;
    using TunnelRelay.UI.ResourceManagement;
    using TunnelRelay.UI.StateManagement;
    using TunnelRelay.Windows.Engine;

    /// <summary>
    /// Interaction logic for LoginToAzure.xaml.
    /// </summary>
    public partial class LoginToAzure : Window
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<LoginToAzure> logger = LoggingHelper.GetLogger<LoginToAzure>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginToAzure"/> class.
        /// </summary>
        public LoginToAzure()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles the RequestNavigate event of the Hyperlink control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RequestNavigateEventArgs"/> instance containing the event data.</param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true,
            };

            Process.Start(processStartInfo);
        }

        /// <summary>
        /// Handles the Click event of the LoginToAzure control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void LoginToAzure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.logger.LogInformation("Starting Azure login.");

                UserAuthenticator userAuthDetails = new UserAuthenticator(LoggingHelper.GetLogger<UserAuthenticator>());

                // Raise Authentication prompt and log the user in.
                userAuthDetails.AuthenticateUserAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                this.logger.LogInformation("Token acquire complete.");

                SelectAzureRelay selectAzureRelay = new SelectAzureRelay(userAuthDetails);
                selectAzureRelay.Left = this.Left;
                selectAzureRelay.Top = this.Top;

                selectAzureRelay.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to Login into Azure");
                MessageBox.Show("Failed to log you in!!", "Login failure", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the Click event of the BtnImportSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BtnImportSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                DefaultExt = ".trs",
                Filter = "Tunnel Relay Settings Files(*.trs)|*.trs",
            };

            bool? dialogResult = openFileDialog.ShowDialog();

            if (dialogResult == true)
            {
                TunnelRelayStateManager.ApplicationData = ApplicationData.ImportSettings(File.ReadAllText(openFileDialog.FileName));

                if (string.IsNullOrEmpty(TunnelRelayStateManager.ApplicationData.HybridConnectionUrl))
                {
                    MessageBox.Show("Failed to find connection details from the imported settings. Please login or import valid settings");
                    return;
                }

                new MainWindow().Show();
                this.Close();
            }
        }
    }
}

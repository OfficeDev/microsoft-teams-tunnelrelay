// <copyright file="LoginToAzure.xaml.cs" company="Microsoft">
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

namespace TunnelRelay
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Navigation;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for LoginToAzure.xaml
    /// </summary>
    public partial class LoginToAzure : Window
    {
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
            System.Diagnostics.Process.Start(e.Uri.ToString());
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
                Logger.LogInfo(CallInfo.Site(), "Starting Azure login.");

                var userAuthDetails = new UserAuthenticator();

                // Raise Authentication prompt and log the user in.
                userAuthDetails.AuthenticateUser();
                Logger.LogInfo(CallInfo.Site(), "Token acquire complete.");

                SelectServiceBus selectServiceBus = new SelectServiceBus(userAuthDetails);
                selectServiceBus.Left = this.Left;
                selectServiceBus.Top = this.Top;

                selectServiceBus.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(CallInfo.Site(), ex, "Failed to Login into Azure");
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
                ApplicationData.ImportSettings(File.ReadAllText(openFileDialog.FileName));

                new MainWindow().Show();
                this.Close();
            }
        }
    }
}

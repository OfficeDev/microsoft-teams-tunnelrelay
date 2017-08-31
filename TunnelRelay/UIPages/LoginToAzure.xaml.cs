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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;
    using TunnelRelay.Core;

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
                AuthenticationContext authContext = new AuthenticationContext("https://login.microsoftonline.com/common", false, TokenCache.DefaultShared);

                // Get Azure Token.
                var azureToken = authContext.AcquireToken(
                    "https://management.azure.com/",
                    "1950a258-227b-4e31-a9cf-717495945fc2",
                    new Uri("urn:ietf:wg:oauth:2.0:oob"),
                    PromptBehavior.RefreshSession);

                Logger.LogInfo(CallInfo.Site(), "Token acquire complete.");

                SelectServiceBus selectServiceBus = new SelectServiceBus(azureToken);
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
    }
}

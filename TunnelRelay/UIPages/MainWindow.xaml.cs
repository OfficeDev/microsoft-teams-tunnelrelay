// <copyright file="MainWindow.xaml.cs" company="Microsoft">
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
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using TunnelRelay.Core;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        /// <summary>
        /// The request map. Request ID => Index.
        /// </summary>
        private ObservableDictionary<string, RequestDetails> requestMap = new ObservableDictionary<string, RequestDetails>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            try
            {
                this.InitializeComponent();

                this.txtProxyDetails.Text = "Starting Azure Proxy";
                this.lstRequests.ItemsSource = this.requestMap;
                CommandBinding cb = new CommandBinding(ApplicationCommands.Copy, this.CopyCmdExecuted, this.CopyCmdCanExecute);
                this.lstRequestHeaders.CommandBindings.Add(cb);
                this.lstResponseHeaders.CommandBindings.Add(cb);
                this.txtRedirectionUrl.Text = ApplicationData.Instance.RedirectionUrl;
                this.StartRelayEngine();

                TunnelRelayEngine.RequestReceived += this.ApplicationEngine_RequestReceived;
                TunnelRelayEngine.RequestUpdated += this.ApplicationEngine_RequestUpdated;
            }
            catch (Exception ex)
            {
                Logger.LogError(CallInfo.Site(), ex);
                MessageBox.Show("Failed to start Tunnel relay!!", "Engine start failure", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the RequestUpdated event of the ApplicationEngine control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RequestEventArgs"/> instance containing the event data.</param>
        private void ApplicationEngine_RequestUpdated(object sender, RequestEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                Logger.LogVerbose(CallInfo.Site(), "Updating request with Id '{0}'", e.Request.RequestId);

                if (this.requestMap.ContainsKey(e.Request.RequestId))
                {
                    try
                    {
                        this.requestMap[e.Request.RequestId] = JObject.FromObject(e.Request).ToObject<RequestDetails>();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });
        }

        /// <summary>
        /// Handles the RequestReceived event of the ApplicationEngine control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RequestEventArgs"/> instance containing the event data.</param>
        private void ApplicationEngine_RequestReceived(object sender, RequestEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                Logger.LogVerbose(CallInfo.Site(), "Received request with Id '{0}'", e.Request.RequestId);

                KeyValuePair<string, RequestDetails> requestItem = new KeyValuePair<string, RequestDetails>(e.Request.RequestId, e.Request);
                this.requestMap.Add(requestItem);
            });
        }

        /// <summary>Executes copy command on list view.</summary>
        /// <param name="target">The target.</param>
        /// <param name="e">The <see cref="ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void CopyCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            ListBox lb = e.OriginalSource as ListBox;
            string copyContent = string.Empty;

            // The SelectedItems could be ListBoxItem instances or data bound objects depending on how you populate the ListBox.
            foreach (HeaderDetails item in lb.SelectedItems)
            {
                copyContent += item.HeaderName + " " + item.HeaderValue;

                // Add a NewLine for carriage return
                copyContent += Environment.NewLine;
            }

            Clipboard.SetText(copyContent);
        }

        /// <summary>
        /// Starts the relay engine.
        /// </summary>
        private void StartRelayEngine()
        {
            Thread backGroundThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    TunnelRelayEngine.StartTunnelRelayEngine();

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.txtProxyDetails.Text = ApplicationData.Instance.ProxyBaseUrl;
                    }));
                }
                catch (Exception)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.txtProxyDetails.Text = "FAILED TO START AZURE PROXY!!!!";
                    }));
                }
            }));

            backGroundThread.Start();
        }

        /// <summary>Checks if Copy command can be execute</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="CanExecuteRoutedEventArgs"/> instance containing the event data.</param>
        private void CopyCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ListBox lb = e.OriginalSource as ListBox;

            // CanExecute only if there is one or more selected Item.
            if (lb.SelectedItems.Count > 0)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnClearAllRequests control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BtnClearAllRequests_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogVerbose(CallInfo.Site(), "Clearing all requests");
            this.requestMap.Clear();
        }

        /// <summary>
        /// Handles the TextChanged event of the TxtRedirectionUrl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private void TxtRedirectionUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            Logger.LogVerbose(CallInfo.Site(), "Updating redirection url");
            ApplicationData.Instance.RedirectionUrl = (sender as TextBox).Text;
        }

        /// <summary>
        /// Handles the Click event of the BtnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.Instance.Logout();

            MessageBox.Show("Logout Complete. Application will now close to complete cleanup. Open again to login");
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Handles the Click event of the PluginManagement control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void PluginManagement_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogVerbose(CallInfo.Site(), "Starting plugin management");
            PluginManagement pluginMangement = new PluginManagement();
            pluginMangement.Show();
        }

        /// <summary>
        /// Handles the Click event of the CoptoClipboard control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void CoptoClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.txtProxyDetails.Text);
        }
    }
}

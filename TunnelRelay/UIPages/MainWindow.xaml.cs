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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Extensions.Options;
    using Microsoft.Win32;
    using Newtonsoft.Json.Linq;
    using TunnelRelay.Core;
    using TunnelRelay.Diagnostics;
    using TunnelRelay.Engine;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window, IRelayRequestEventListener
    {
        /// <summary>
        /// The request map. Request ID => Index.
        /// </summary>
        private ObservableDictionary<string, RequestDetails> requestMap = new ObservableDictionary<string, RequestDetails>();

        /// <summary>
        /// Hybrid connection manager.
        /// </summary>
        internal static HybridConnectionManager hybridConnectionManager;

        /// <summary>
        /// Options monitor to update redirect url.
        /// </summary>
        internal SimpleOptionsMonitor<RelayRequestManagerOptions> relayRequestManagerOptions = new SimpleOptionsMonitor<RelayRequestManagerOptions>();

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

                this.btnExportSettings.IsEnabled = false;
                this.StartRelayEngine();
            }
            catch (Exception ex)
            {
                Logger.LogError(CallInfo.Site(), ex);
                MessageBox.Show("Failed to start Tunnel relay!!", "Engine start failure", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            }
        }

        public Task RequestReceivedAsync(string requestId, RelayRequest relayRequest)
        {
            this.Dispatcher.Invoke(() =>
            {
                Logger.LogVerbose(CallInfo.Site(), "Received request with Id '{0}'", requestId);

                KeyValuePair<string, RequestDetails> requestItem = new KeyValuePair<string, RequestDetails>(requestId, new RequestDetails
                {
                    Method = relayRequest.HttpMethod.Method,
                    RequestHeaders = relayRequest.Headers.GetHeaderMap(),
                    RequestData = (new StreamReader(relayRequest.InputStream)).ReadToEnd(),
                    RequestReceiveTime = relayRequest.RequestStartDateTime.DateTime,
                    Url = relayRequest.RelativeUrl,
                    StatusCode = "Active",
                });

                this.requestMap.Add(requestItem);
            });

            return Task.CompletedTask;
        }

        public Task ResponseSentAsync(string requestId, RelayResponse relayResponse)
        {
            this.Dispatcher.Invoke(() =>
            {
                Logger.LogVerbose(CallInfo.Site(), "Updating request with Id '{0}'", requestId);

                if (this.requestMap.ContainsKey(requestId))
                {
                    try
                    {
                        RequestDetails requestDetails = this.requestMap[requestId];
                        requestDetails.ResponseData = new StreamReader(relayResponse.OutputStream).ReadToEnd();
                        requestDetails.ResponseHeaders = relayResponse.Headers.GetHeaderMap();
                        requestDetails.StatusCode = relayResponse.HttpStatusCode.ToString();
                        requestDetails.Duration = (relayResponse.RequestEndDateTime.DateTime - requestDetails.RequestReceiveTime).TotalMilliseconds + " ms";

                        // For a Change event to fire we need to completely replace the object so we are cloning the object and replacing it.
                        this.requestMap[requestId] = JObject.FromObject(requestDetails).ToObject<RequestDetails>();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(CallInfo.Site(), ex, "Hit exception while updating request with Id '{0}'.", requestId);
                    }
                }
            });

            return Task.CompletedTask;
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
                    HybridConnectionManagerOptions hybridConnectionManagerOptions = new HybridConnectionManagerOptions
                    {
                        ConnectionPath = ApplicationData.Instance.HybridConnectionName,
                        ServiceBusKeyName = ApplicationData.Instance.HybridConnectionKeyName,
                        ServiceBusSharedKey = ApplicationData.Instance.HybridConnectionSharedKey,
                        ServiceBusUrl = ApplicationData.Instance.HybridConnectionUrl
                    };

                    relayRequestManagerOptions.CurrentValue = new RelayRequestManagerOptions
                    {
                        InternalServiceUrl = new Uri(ApplicationData.Instance.RedirectionUrl),
                    };

                    // TODO (ramjsing) : Enable support for Plugins again.
                    RelayRequestManager relayManager = new RelayRequestManager(
                        relayRequestManagerOptions,
                        new List<ITunnelRelayPlugin>(),
                        this);

                    hybridConnectionManager = new HybridConnectionManager(
                        Options.Create(hybridConnectionManagerOptions),
                        relayManager);

                    hybridConnectionManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.txtProxyDetails.Text = ApplicationData.Instance.HybridConnectionUrl +  ApplicationData.Instance.HybridConnectionName;
                        this.btnExportSettings.IsEnabled = true;
                    }));
                }
                catch (Exception ex)
                {
                    Logger.LogError(CallInfo.Site(), ex, "Failed to establish connection to Azure Relay");

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.txtProxyDetails.Text = "FAILED TO START AZURE PROXY!!!!";
                        this.btnExportSettings.IsEnabled = false;
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

            relayRequestManagerOptions.CurrentValue = new RelayRequestManagerOptions
            {
                InternalServiceUrl = new Uri(ApplicationData.Instance.RedirectionUrl),
            };
        }

        /// <summary>
        /// Handles the Click event of the BtnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.Logout();

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

        /// <summary>
        /// Handles the Click event of the btnExportSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BtnExportSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                DefaultExt = ".trs",
                Filter = "Tunnel Relay Settings Files(*.trs)|*.trs",
                FileName = new Uri(ApplicationData.Instance.HybridConnectionUrl).Authority,
                OverwritePrompt = true,
            };

            bool? dialogResult = saveFileDialog.ShowDialog();

            if (dialogResult == true)
            {
                string exportedSettingsFileName = saveFileDialog.FileName;

                File.WriteAllText(exportedSettingsFileName, ApplicationData.GetExportedSettings());
            }
        }
    }
}

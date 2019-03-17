// <copyright file="MainWindow.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Extensions.Logging;
    using Microsoft.Win32;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using TunnelRelay.Core;
    using TunnelRelay.Windows.Engine;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    internal partial class MainWindow : Window, IRelayRequestEventListener
    {
        /// <summary>
        /// The request map. Request ID => Index.
        /// </summary>
        private readonly ObservableDictionary<string, RequestDetails> requestMap = new ObservableDictionary<string, RequestDetails>();

        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<MainWindow> logger = LoggingHelper.GetLogger<MainWindow>();

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
                this.txtRedirectionUrl.Text = TunnelRelayStateManager.ApplicationData.RedirectionUrl;

                this.btnExportSettings.IsEnabled = false;
                this.StartRelayEngine();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Init failed");
                MessageBox.Show("Failed to start Tunnel relay!!", "Engine start failure", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Executed when a new request is received.
        /// </summary>
        /// <param name="requestId">Unique request Id.</param>
        /// <param name="relayRequest">Relay request instance.</param>
        /// <returns>Task tracking operation.</returns>
        public Task RequestReceivedAsync(string requestId, RelayRequest relayRequest)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.logger.LogTrace("Received request with Id '{0}'", requestId);

                KeyValuePair<string, RequestDetails> requestItem = new KeyValuePair<string, RequestDetails>(requestId, new RequestDetails
                {
                    Method = relayRequest.HttpMethod.Method,
                    RequestHeaders = relayRequest.Headers.GetHeaderMap(),
                    RequestData = this.GetUIFriendlyString(relayRequest.InputStream, relayRequest.Headers[HttpRequestHeader.ContentType], relayRequest.Headers[HttpRequestHeader.ContentEncoding]),
                    RequestReceiveTime = relayRequest.RequestStartDateTime.DateTime,
                    Url = relayRequest.RequestPathAndQuery,
                    StatusCode = "Active",
                });

                this.requestMap.Add(requestItem);
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Executed when response for a request is sent back.
        /// </summary>
        /// <param name="requestId">Unique request Id.</param>
        /// <param name="relayResponse">The response being sent back.</param>
        /// <returns>Task tracking operation.</returns>
        public Task ResponseSentAsync(string requestId, RelayResponse relayResponse)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.logger.LogTrace("Updating request with Id '{0}'", requestId);

                if (this.requestMap.ContainsKey(requestId))
                {
                    try
                    {
                        RequestDetails requestDetails = this.requestMap[requestId];
                        requestDetails.ResponseData = this.GetUIFriendlyString(relayResponse.OutputStream, relayResponse.Headers[HttpResponseHeader.ContentType], relayResponse.Headers[HttpResponseHeader.ContentEncoding]);
                        requestDetails.ResponseHeaders = relayResponse.Headers.GetHeaderMap();
                        requestDetails.StatusCode = relayResponse.HttpStatusCode.ToString();
                        requestDetails.Duration = (relayResponse.RequestEndDateTime.DateTime - requestDetails.RequestReceiveTime).TotalMilliseconds + " ms";

                        // For a Change event to fire we need to completely replace the object so we are cloning the object and replacing it.
                        this.requestMap[requestId] = JObject.FromObject(requestDetails).ToObject<RequestDetails>();
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "Hit exception while updating request with Id '{0}'.", requestId);
                    }
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets UI friendly string from stream.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="contentType">Content-Type header value.</param>
        /// <param name="contentEncoding">Content-Encoding header value.</param>
        /// <returns>String read, decompressed (if needed) and formatted (if needed).</returns>
        private string GetUIFriendlyString(Stream stream, string contentType, string contentEncoding)
        {
            if (!string.IsNullOrEmpty(contentEncoding))
            {
                if (contentEncoding.Equals("gzip", StringComparison.OrdinalIgnoreCase))
                {
                    stream = new GZipStream(stream, CompressionMode.Decompress);
                }
                else if (contentEncoding.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                {
                    stream = new DeflateStream(stream, CompressionMode.Decompress);
                }
            }

            string data = new StreamReader(stream).ReadToEnd();

            if (!string.IsNullOrEmpty(contentType) && contentType.Contains("json") && data.Length > 0)
            {
                try
                {
                    data = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(data), Formatting.Indented);
                }
                catch
                {
                    this.logger.LogTrace($"Failed to transform data into formatted json.");
                }
            }

            return data;
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
                    TunnelRelayStateManager.InitializePlugins();
                    TunnelRelayStateManager.RelayRequestEventListener = this;
                    TunnelRelayStateManager.StartTunnelRelayAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.txtProxyDetails.Text = TunnelRelayStateManager.ApplicationData.HybridConnectionUrl + TunnelRelayStateManager.ApplicationData.HybridConnectionName;
                        this.btnExportSettings.IsEnabled = true;
                    }));
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to establish connection to Azure Relay");

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.txtProxyDetails.Text = "FAILED TO START AZURE PROXY!!!!";
                        this.btnExportSettings.IsEnabled = false;
                    }));
                }
            }));

            backGroundThread.Start();
        }

        /// <summary>Checks if Copy command can be execute.</summary>
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
            this.logger.LogTrace("Clearing all requests");
            this.requestMap.Clear();
        }

        /// <summary>
        /// Handles the TextChanged event of the TxtRedirectionUrl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private void TxtRedirectionUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.logger.LogTrace("Updating redirection url");
            TunnelRelayStateManager.ApplicationData.RedirectionUrl = (sender as TextBox).Text;

            try
            {
                TunnelRelayStateManager.RelayRequestManagerOptions.CurrentValue = new RelayRequestManagerOptions
                {
                    InternalServiceUrl = new Uri(TunnelRelayStateManager.ApplicationData.RedirectionUrl),
                };

                (sender as TextBox).Background = Brushes.White;
            }
            catch (Exception)
            {
                (sender as TextBox).Background = Brushes.Red;
            }
        }

        /// <summary>
        /// Handles the Click event of the BtnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            TunnelRelayStateManager.LogoutAsync().ConfigureAwait(false).GetAwaiter().GetResult();

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
            this.logger.LogTrace("Starting plugin management");
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
                FileName = new Uri(TunnelRelayStateManager.ApplicationData.HybridConnectionUrl).Authority,
                OverwritePrompt = true,
            };

            bool? dialogResult = saveFileDialog.ShowDialog();

            if (dialogResult == true)
            {
                string exportedSettingsFileName = saveFileDialog.FileName;

                File.WriteAllText(exportedSettingsFileName, TunnelRelayStateManager.ApplicationData.ExportSettings());
            }
        }
    }
}

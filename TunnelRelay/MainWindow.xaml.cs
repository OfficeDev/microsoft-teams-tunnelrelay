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
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;

            if (string.IsNullOrEmpty(ApplicationData.Instance.LoginId))
            {
                GettingStarted gettingStarted = new GettingStarted();
                bool? val = gettingStarted.ShowDialog();

                if ((val.HasValue && !val.Value) || !val.HasValue)
                {
                    Application.Current.Shutdown();
                    return;
                }
            }

            this.prgressBar.Visibility = Visibility.Visible;
            this.txtProxyDetails.Text = "Starting Azure Proxy";
            this.lstRequests.ItemsSource = ApplicationEngine.Requests;
            this.lstbox_PluginList.ItemsSource = ApplicationEngine.Plugins;
            CommandBinding cb = new CommandBinding(ApplicationCommands.Copy, this.CopyCmdExecuted, this.CopyCmdCanExecute);
            this.lstRequestHeaders.CommandBindings.Add(cb);
            this.lstResponseHeaders.CommandBindings.Add(cb);
            this.txtRedirectionUrl.Text = ApplicationData.Instance.RedirectionUrl;
            this.lblServiceBusDetails.Content = string.Format(
                "Logged in using '{0}' account. And using Service bus with namespace '{1}' with Authorization key '{2}'",
                ApplicationData.Instance.LoginId,
                ApplicationData.Instance.ServiceBusName,
                ApplicationData.Instance.ServiceBusKeyName);
            this.StartRelayEngine();
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        internal static MainWindow Instance { get; private set; }

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
                    ApplicationEngine.StartAzureRelayEngine();

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.prgressBar.Visibility = Visibility.Hidden;
                        this.txtProxyDetails.Text = "Azure Proxy is running on " + ApplicationData.Instance.ProxyBaseUrl;
                    }));
                }
                catch (Exception)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.prgressBar.Visibility = Visibility.Hidden;
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
            ApplicationEngine.Requests.Clear();
        }

        /// <summary>
        /// Handles the TextChanged event of the TxtRedirectionUrl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private void TxtRedirectionUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplicationData.Instance.RedirectionUrl = (sender as TextBox).Text;
        }

        /// <summary>
        /// Handles the Click event of the SavePluginSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SavePluginSettings_Click(object sender, RoutedEventArgs e)
        {
            if (this.lstbox_PluginList.SelectedItem != null)
            {
                (this.lstbox_PluginList.SelectedItem as IRedirectionPlugin).PluginData = this.txtBox_PluginSettings.Text;
            }
        }

        /// <summary>
        /// Handles the Click event of the BtnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.Instance.LoginId = string.Empty;
            ApplicationData.Instance.ServiceBusKeyName = string.Empty;
            ApplicationData.Instance.ServiceBusName = string.Empty;
            ApplicationData.Instance.ServiceBusSharedKey = string.Empty;
            ApplicationData.Instance.ServiceBusUrl = string.Empty;

            MessageBox.Show("Logout Complete. Application will now close to complete cleanup. Open again to login");
            Application.Current.Shutdown();
        }
    }
}

// <copyright file="GettingStarted.xaml.cs" company="Microsoft">
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
    using System.Collections.ObjectModel;
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
    using Microsoft.Azure.Management.ServiceBus.Fluent;
    using Microsoft.Azure.Management.ServiceBus.Fluent.Models;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;
    using RM = Microsoft.Azure.Management.ResourceManager.Fluent.Models;

    /// <summary>
    /// Interaction logic for GettingStarted.xaml
    /// </summary>
    internal partial class GettingStarted : Window
    {
        /// <summary>
        /// The subscription list.
        /// </summary>
        private ObservableCollection<RM.SubscriptionInner> subscriptionList = new ObservableCollection<RM.SubscriptionInner>();

        /// <summary>
        /// The service bus namespace map.
        /// </summary>
        private Dictionary<RM.SubscriptionInner, List<NamespaceModelInner>> serviceBusNamespaceMap = new Dictionary<RM.SubscriptionInner, List<NamespaceModelInner>>();

        /// <summary>
        /// The authentication rule map.
        /// </summary>
        private Dictionary<NamespaceModelInner, List<SharedAccessAuthorizationRuleInner>> authRuleMap = new Dictionary<NamespaceModelInner, List<SharedAccessAuthorizationRuleInner>>();

        /// <summary>
        /// The shared key map.
        /// </summary>
        private Dictionary<SharedAccessAuthorizationRuleInner, string> sharedKeyMap = new Dictionary<SharedAccessAuthorizationRuleInner, string>();

        /// <summary>
        /// The login identifier.
        /// </summary>
        private string loginId;

        /// <summary>
        /// Initializes a new instance of the <see cref="GettingStarted"/> class.
        /// </summary>
        public GettingStarted()
        {
            this.InitializeComponent();
            this.comboAzureSubscription.ItemsSource = this.subscriptionList;
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
            this.progressAzureLogin.Visibility = Visibility.Visible;
            Thread backGroundThread = new Thread(new ThreadStart(() =>
            {
                AuthenticationContext authContext = new AuthenticationContext("https://login.microsoftonline.com/common", false, TokenCache.DefaultShared);

                this.UpdateStatus("Authenticating with Azure");

                // Get Azure Token.
                var azureToken = authContext.AcquireToken(
                    "https://management.azure.com/",
                    "1950a258-227b-4e31-a9cf-717495945fc2",
                    new Uri("urn:ietf:wg:oauth:2.0:oob"),
                    PromptBehavior.Always);

                this.loginId = azureToken.UserInfo.DisplayableId;

                // Get Service bus Token.
                var serviceBusToken = authContext.AcquireToken(
                    "https://management.core.windows.net/",
                    "1950a258-227b-4e31-a9cf-717495945fc2",
                    new UserAssertion(azureToken.AccessToken, azureToken.AccessTokenType));

                TokenCredentials tokenCredentials = new TokenCredentials(azureToken.AccessToken);
                TokenCredentials serviceBusTokenCreds = new TokenCredentials(serviceBusToken.AccessToken);

                SubscriptionClient subsClient = new SubscriptionClient(tokenCredentials);
                ServiceBusManagementClient serviceBusManagementClient = new ServiceBusManagementClient(serviceBusTokenCreds);

                this.UpdateStatus("Fetching subscription list");
                var subsList = subsClient.Subscriptions.List();

                this.serviceBusNamespaceMap.Clear();
                this.authRuleMap.Clear();
                foreach (var sub in subsList)
                {
                    serviceBusManagementClient.SubscriptionId = sub.SubscriptionId;
                    this.UpdateStatus("Fetching Service Bus namespaces for subscription " + sub.DisplayName);

                    var namespaceList = serviceBusManagementClient.Namespaces.List();

                    List<NamespaceModelInner> namespaceListFiltered = new List<NamespaceModelInner>();

                    foreach (var sbNamespace in namespaceList)
                    {
                        try
                        {
                            this.UpdateStatus("Getting auth rules for namespace " + sbNamespace.Name);
                            string rgName = sbNamespace.Id.Split('/')[4];
                            var authRules = serviceBusManagementClient.Namespaces
                                .ListAuthorizationRules(rgName, sbNamespace.Name)
                                .Where(rule => rule.Rights.Contains(AccessRights.Listen)).ToList();

                            authRules.ForEach(rule =>
                            {
                                this.sharedKeyMap.Add(rule, serviceBusManagementClient.Namespaces.ListKeys(rgName, sbNamespace.Name, rule.Name).PrimaryKey);
                            });

                            this.authRuleMap.Add(sbNamespace, authRules);
                            namespaceListFiltered.Add(sbNamespace);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    this.serviceBusNamespaceMap.Add(sub, namespaceListFiltered);
                }

                this.Dispatcher.Invoke(() =>
                {
                    this.subscriptionList.Clear();
                    this.txtStatus.Text = string.Empty;
                    subsList.ToList().ForEach(sub => this.subscriptionList.Add(sub));
                    this.progressAzureLogin.Visibility = Visibility.Hidden;
                });
            }));
            backGroundThread.Start();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ComboAzureSubscription control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ComboAzureSubscription_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem as RM.SubscriptionInner != null)
            {
                this.comboServiceBus.Items.Clear();

                this.serviceBusNamespaceMap[(sender as ComboBox).SelectedItem as RM.SubscriptionInner].ForEach(sbNamespace =>
                    this.comboServiceBus.Items.Add(sbNamespace));
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ComboServiceBus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ComboServiceBus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem as NamespaceModelInner != null)
            {
                this.comboAuthRules.Items.Clear();

                this.authRuleMap[(sender as ComboBox).SelectedItem as NamespaceModelInner].ForEach(rule =>
                    this.comboAuthRules.Items.Add(rule));
            }
        }

        /// <summary>
        /// Handles the Click event of the OK Button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.comboServiceBus.SelectedItem as NamespaceModelInner == null)
            {
                MessageBox.Show("Select a Service bus before clicking OK");
            }

            if (this.comboAuthRules.SelectedItem as SharedAccessAuthorizationRuleInner == null)
            {
                MessageBox.Show("Select an Authorization rule before clicking OK");
            }

            ApplicationData.Instance.LoginId = this.loginId;
            ApplicationData.Instance.ServiceBusKeyName = (this.comboAuthRules.SelectedItem as SharedAccessAuthorizationRuleInner).Name;
            ApplicationData.Instance.ServiceBusName = (this.comboServiceBus.SelectedItem as NamespaceModelInner).Name;
            ApplicationData.Instance.ServiceBusUrl = (this.comboServiceBus.SelectedItem as NamespaceModelInner).ServiceBusEndpoint;
            ApplicationData.Instance.ServiceBusSharedKey = this.sharedKeyMap[this.comboAuthRules.SelectedItem as SharedAccessAuthorizationRuleInner];
            this.DialogResult = true;
        }

        /// <summary>
        /// Handles the Click event of the Cancel Button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        /// <summary>
        /// Updates the status.
        /// </summary>
        /// <param name="statusText">The status text.</param>
        private void UpdateStatus(string statusText)
        {
            this.Dispatcher.Invoke(() => this.txtStatus.Text = statusText + "...");
        }
    }
}

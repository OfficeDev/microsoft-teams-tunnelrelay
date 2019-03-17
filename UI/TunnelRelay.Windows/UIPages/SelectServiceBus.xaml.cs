// <copyright file="SelectServiceBus.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Azure.Management.Relay.Fluent;
    using Microsoft.Azure.Management.Relay.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Extensions.Logging;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure;
    using TunnelRelay.Windows.Engine;
    using RM = Microsoft.Azure.Management.ResourceManager.Fluent;

    /// <summary>
    /// Interaction logic for SelectServiceBus.xaml.
    /// </summary>
    public partial class SelectServiceBus : Window
    {
        /// <summary>
        /// The new service bus template.
        /// </summary>
        private static RelayNamespaceInner newServiceBus = new RelayNamespaceInner("WestUS", null, "Create a new Service Bus")
        {
            Sku = new Sku(SkuTier.Standard),
            Tags = new Dictionary<string, string>() { { "CreatedBy", "TunnelRelayv2" } },
        };

        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<SelectServiceBus> logger = LoggingHelper.GetLogger<SelectServiceBus>();

        /// <summary>
        /// User authentication manager.
        /// </summary>
        private UserAuthenticator userAuthenticator;

        /// <summary>
        /// List of Azure subscription.
        /// </summary>
        private ObservableCollection<RM.Models.SubscriptionInner> subscriptions = new ObservableCollection<RM.Models.SubscriptionInner>();

        /// <summary>
        /// List of Azure service buses.
        /// </summary>
        private ObservableCollection<RelayNamespaceInner> serviceBuses = new ObservableCollection<RelayNamespaceInner>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectServiceBus"/> class.
        /// </summary>
        /// <param name="authenticationDetails">The authentication details for the user.</param>
        public SelectServiceBus(UserAuthenticator authenticationDetails)
        {
            this.ContentRendered += this.Window_ContentRendered;
            this.InitializeComponent();
            this.userAuthenticator = authenticationDetails;
            this.comboSubscriptionList.ItemsSource = this.subscriptions;
            this.comboServiceBusList.ItemsSource = this.serviceBuses;

            // Disable controls to begin with.
            this.comboSubscriptionList.IsEnabled = false;
            this.comboServiceBusList.IsEnabled = false;
        }

        /// <summary>
        /// Handles the ContentRendered event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            this.progressBar.Visibility = Visibility.Visible;

            Thread subscriptionThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    var subscriptionList = this.userAuthenticator.GetUserSubscriptions();

                    this.Dispatcher.Invoke(() =>
                    {
                        this.subscriptions.Clear();
                        subscriptionList.ForEach(sub => this.subscriptions.Add(sub));
                        this.comboSubscriptionList.IsEnabled = true;
                        this.progressBar.Visibility = Visibility.Hidden;
                    });
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to get user subscriptions");

                    this.Dispatcher.Invoke(() => MessageBox.Show("Failed to get list of subscriptons!! Exiting", "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error));
                    Application.Current.Shutdown();
                }
            }));

            subscriptionThread.Start();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the SubscriptionList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void SubscriptionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnDone.IsEnabled = false;

            this.progressBar.Visibility = Visibility.Visible;
            var selectedSubscription = (sender as ComboBox).SelectedItem as RM.Models.SubscriptionInner;

            Thread serviceBusThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    TokenCredentials tokenCredentials = new TokenCredentials(this.userAuthenticator.GetSubscriptionSpecificUserToken(selectedSubscription).AccessToken);
                    RelayManagementClient relayManagementClient = new RelayManagementClient(tokenCredentials);

                    relayManagementClient.SubscriptionId = selectedSubscription.SubscriptionId;

                    List<RelayNamespaceInner> serviceBusList = new List<RelayNamespaceInner>();
                    var resp = relayManagementClient.Namespaces.ListAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    serviceBusList.AddRange(resp);

                    while (!string.IsNullOrEmpty(resp.NextPageLink))
                    {
                        resp = relayManagementClient.Namespaces.ListNextAsync(resp.NextPageLink).ConfigureAwait(false).GetAwaiter().GetResult();
                        serviceBusList.AddRange(resp);
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        this.serviceBuses.Clear();
                        this.serviceBuses.Add(newServiceBus);
                        serviceBusList.ForEach(sub => this.serviceBuses.Add(sub));
                        this.comboServiceBusList.IsEnabled = true;
                        this.progressBar.Visibility = Visibility.Hidden;
                    });
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to get list of Service bus namespaces");

                    this.Dispatcher.Invoke(() => MessageBox.Show("Failed to get list of Service bus namespaces!!. Exiting", "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error));
                    Application.Current.Shutdown();
                }
            }));

            serviceBusThread.Start();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ServiceBusList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ServiceBusList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSubscription = (this.comboSubscriptionList as ComboBox).SelectedItem as RM.Models.SubscriptionInner;
            var selectedServiceBus = (sender as ComboBox).SelectedItem as RelayNamespaceInner;

            if (selectedServiceBus.Id == null)
            {
                this.lblServiceBusName.Visibility = Visibility.Visible;

                if (!string.IsNullOrEmpty(this.txtServiceBusName.Text))
                {
                    this.btnDone.IsEnabled = true;
                }

                return;
            }
            else
            {
                this.lblServiceBusName.Visibility = Visibility.Collapsed;
                this.btnDone.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the Done control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.progressBar.Visibility = Visibility.Visible;
            this.btnDone.IsEnabled = false;
            var selectedSubscription = (this.comboSubscriptionList as ComboBox).SelectedItem as RM.Models.SubscriptionInner;
            var selectedServiceBus = (this.comboServiceBusList as ComboBox).SelectedItem as RelayNamespaceInner;

            string newBusName = this.txtServiceBusName.Text;
            string rgName = null;
            if (!string.IsNullOrEmpty(selectedServiceBus.Id))
            {
                int startIndex = selectedServiceBus.Id.IndexOf("resourceGroups") + 15;
                rgName = selectedServiceBus.Id.Substring(startIndex, selectedServiceBus.Id.IndexOf('/', startIndex) - startIndex);
            }

            TokenCredentials tokenCredentials = new TokenCredentials(this.userAuthenticator.GetSubscriptionSpecificUserToken(selectedSubscription).AccessToken);
            RelayManagementClient relayManagementClient = new RelayManagementClient(tokenCredentials);
            relayManagementClient.SubscriptionId = selectedSubscription.SubscriptionId;

            List<AuthorizationRuleInner> serviceBusList = new List<AuthorizationRuleInner>();

            // Case 1. When user used existing service bus.
            Thread existingServiceBusThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    List<HybridConnectionInner> hybridConnections = new List<HybridConnectionInner>();
                    hybridConnections.AddRange(relayManagementClient.HybridConnections.ListByNamespaceAsync(rgName, selectedServiceBus.Name).ConfigureAwait(false).GetAwaiter().GetResult());

                    // Create the hybrid connection if one does not exist.
                    if (!hybridConnections.Any(connection => connection.Name.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)))
                    {
                        relayManagementClient.HybridConnections.CreateOrUpdateAsync(rgName, selectedServiceBus.Name, Environment.MachineName, new HybridConnectionInner
                        {
                            RequiresClientAuthorization = false,
                        }).ConfigureAwait(false).GetAwaiter().GetResult();
                    }

                    this.SetApplicationData(rgName, relayManagementClient, selectedServiceBus);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to connect to service bus namespace");

                    this.Dispatcher.Invoke(() => MessageBox.Show("Failed to connect to service bus namespace!!", "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error));
                }
            }));

            // Case 2. When user created a new service bus.
            Thread newServiceBusThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    RM.ResourceManagementClient resourceManagementClient = new RM.ResourceManagementClient(tokenCredentials);
                    resourceManagementClient.SubscriptionId = selectedSubscription.SubscriptionId;

                    RM.Models.ResourceGroupInner resourceGroup = null;

                    try
                    {
                        resourceGroup = resourceManagementClient.ResourceGroups.GetAsync("TunnelRelay").ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (Microsoft.Rest.Azure.CloudException httpEx)
                    {
                        if (httpEx.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            resourceGroup = resourceManagementClient.ResourceGroups.CreateOrUpdateAsync("TunnelRelay", new RM.Models.ResourceGroupInner
                            {
                                Location = "WestUS",
                                Name = "TunnelRelay",
                                Tags = newServiceBus.Tags,
                            }).ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                    }

                    rgName = resourceGroup.Name;

                    if (string.IsNullOrEmpty(newBusName))
                    {
                        MessageBox.Show("Please enter the name for service bus.");
                        this.Dispatcher.Invoke(() =>
                        {
                            this.progressBar.Visibility = Visibility.Hidden;
                            this.btnDone.IsEnabled = true;
                        });
                        return;
                    }

                    if (newBusName.Length < 6)
                    {
                        MessageBox.Show("Name of service bus must be at least 6 characters.");
                        this.Dispatcher.Invoke(() =>
                        {
                            this.progressBar.Visibility = Visibility.Hidden;
                            this.btnDone.IsEnabled = true;
                        });
                        return;
                    }

                    selectedServiceBus = relayManagementClient.Namespaces.CreateOrUpdateAsync(rgName, newBusName, new RelayNamespaceInner
                    {
                        Location = selectedServiceBus.Location,
                        Sku = selectedServiceBus.Sku,
                        Tags = selectedServiceBus.Tags,
                    }).ConfigureAwait(false).GetAwaiter().GetResult();

                    relayManagementClient.HybridConnections.CreateOrUpdateAsync(rgName, newBusName, Environment.MachineName, new HybridConnectionInner
                    {
                        RequiresClientAuthorization = false,
                    }).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.SetApplicationData(rgName, relayManagementClient, selectedServiceBus);
                }
                catch (CloudException cloudEx)
                {
                    this.logger.LogError(cloudEx, "Cloud exception while creating service bus namespace.");

                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(cloudEx.Message, "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                        this.progressBar.Visibility = Visibility.Hidden;
                        this.btnDone.IsEnabled = true;
                    });
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to create new service bus namespace");

                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Failed to create new service bus namespace!!", "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                        this.progressBar.Visibility = Visibility.Hidden;
                        this.btnDone.IsEnabled = true;
                    });
                }
            }));

            if (selectedServiceBus.Id == null)
            {
                newServiceBusThread.Start();
            }
            else
            {
                existingServiceBusThread.Start();
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the ServiceBusName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private void ServiceBusName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty((sender as TextBox).Text))
            {
                this.btnDone.IsEnabled = false;
            }
            else
            {
                this.btnDone.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the Cancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Gets the auth rules for a service bus and writes Application data.
        /// </summary>
        /// <param name="rgName">Name of the rg.</param>
        /// <param name="relayManagementClient">The service bus management client.</param>
        /// <param name="selectedServiceBus">The selected service bus.</param>
        private void SetApplicationData(string rgName, RelayManagementClient relayManagementClient, RelayNamespaceInner selectedServiceBus)
        {
            List<AuthorizationRuleInner> serviceBusAuthRuleList = new List<AuthorizationRuleInner>();
            var resp = relayManagementClient.Namespaces.ListAuthorizationRulesAsync(rgName, selectedServiceBus.Name).ConfigureAwait(false).GetAwaiter().GetResult();
            serviceBusAuthRuleList.AddRange(resp);

            while (!string.IsNullOrEmpty(resp.NextPageLink))
            {
                resp = relayManagementClient.Namespaces.ListAuthorizationRulesNextAsync(resp.NextPageLink).ConfigureAwait(false).GetAwaiter().GetResult();
                serviceBusAuthRuleList.AddRange(resp);
            }

            var selectedAuthRule = serviceBusAuthRuleList.FirstOrDefault(rule => rule.Rights != null && rule.Rights.Contains(AccessRights.Listen) && rule.Rights.Contains(AccessRights.Manage) && rule.Rights.Contains(AccessRights.Send));

            if (selectedAuthRule == null)
            {
                MessageBox.Show("Failed to find a suitable Authorization rule to use. Please create an Authorization rule with Listen, Manage and Send rights and retry the operation");
                this.Dispatcher.Invoke(() =>
                {
                    this.progressBar.Visibility = Visibility.Hidden;
                    this.btnDone.IsEnabled = true;
                });
                return;
            }
            else
            {
                TunnelRelayStateManager.ApplicationData.EnableCredentialEncryption = this.chkEnableEncryption.IsChecked.GetValueOrDefault();
                TunnelRelayStateManager.ApplicationData.HybridConnectionSharedKey = relayManagementClient.Namespaces.ListKeysAsync(
                    rgName,
                    selectedServiceBus.Name,
                    selectedAuthRule.Name).ConfigureAwait(false).GetAwaiter().GetResult().PrimaryKey;
                TunnelRelayStateManager.ApplicationData.HybridConnectionKeyName = relayManagementClient.Namespaces.ListKeysAsync(
                    rgName,
                    selectedServiceBus.Name,
                    selectedAuthRule.Name).ConfigureAwait(false).GetAwaiter().GetResult().KeyName;
                TunnelRelayStateManager.ApplicationData.HybridConnectionUrl = selectedServiceBus.ServiceBusEndpoint;
                TunnelRelayStateManager.ApplicationData.HybridConnectionName = Environment.MachineName;

                this.Dispatcher.Invoke(() =>
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                });
            }
        }

        /// <summary>
        /// Invoked when user unchecks the encryption setting on the UI.
        /// </summary>
        /// <param name="sender">Event sender, in this case it is <see cref="chkEnableEncryption"/>.</param>
        /// <param name="e">Event arguments.</param>
        private void ChkEnableEncryption_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!(sender as CheckBox).IsInitialized)
            {
                return;
            }

            MessageBoxResult messageBoxResult = MessageBox.Show(
                "WARNING: This will disable Azure Service Bus SAS key encryption, if the configuration is copied out of this machine " +
                    "it can be used by someone else to connect to the Azure Service Bus. Do you want to continue?",
                "Key encryption setting",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageBoxResult == MessageBoxResult.No)
            {
                // Undo user's action.
                (sender as CheckBox).IsChecked = true;
            }
        }
    }
}

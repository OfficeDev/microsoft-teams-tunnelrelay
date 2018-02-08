// <copyright file="SelectServiceBus.xaml.cs" company="Microsoft">
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
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ServiceBus.Fluent;
    using Microsoft.Azure.Management.ServiceBus.Fluent.Models;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure;
    using TunnelRelay.Core;
    using RM = Microsoft.Azure.Management.ResourceManager.Fluent;

    /// <summary>
    /// Interaction logic for SelectServiceBus.xaml
    /// </summary>
    public partial class SelectServiceBus : Window
    {
        /// <summary>
        /// The new service bus template.
        /// </summary>
        private static NamespaceModelInner newServiceBus = new NamespaceModelInner("WestUS", null, "Create a new Service Bus")
        {
            Sku = new Sku
            {
                Name = "Basic",
                Tier = "Basic",
            },
            Tags = new Dictionary<string, string>() { { "CreatedBy", "TunnelRelay" } },
        };

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
        private ObservableCollection<NamespaceModelInner> serviceBuses = new ObservableCollection<NamespaceModelInner>();

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
                    Logger.LogError(CallInfo.Site(), ex, "Failed to get user subscriptions");

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
                    ServiceBusManagementClient serviceBusManagementClient = new ServiceBusManagementClient(tokenCredentials);

                    serviceBusManagementClient.SubscriptionId = selectedSubscription.SubscriptionId;

                    List<NamespaceModelInner> serviceBusList = new List<NamespaceModelInner>();
                    var resp = serviceBusManagementClient.Namespaces.List();
                    serviceBusList.AddRange(resp);

                    while (!string.IsNullOrEmpty(resp.NextPageLink))
                    {
                        resp = serviceBusManagementClient.Namespaces.ListNext(resp.NextPageLink);
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
                    Logger.LogError(CallInfo.Site(), ex, "Failed to get list of Service bus namespaces");

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
            var selectedServiceBus = (sender as ComboBox).SelectedItem as NamespaceModelInner;

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
            var selectedServiceBus = (this.comboServiceBusList as ComboBox).SelectedItem as NamespaceModelInner;

            string newBusName = this.txtServiceBusName.Text;
            string rgName = null;
            if (!string.IsNullOrEmpty(selectedServiceBus.Id))
            {
                int startIndex = selectedServiceBus.Id.IndexOf("resourceGroups") + 15;
                rgName = selectedServiceBus.Id.Substring(startIndex, selectedServiceBus.Id.IndexOf('/', startIndex) - startIndex);
            }

            TokenCredentials tokenCredentials = new TokenCredentials(this.userAuthenticator.GetSubscriptionSpecificUserToken(selectedSubscription).AccessToken);
            ServiceBusManagementClient serviceBusManagementClient = new ServiceBusManagementClient(tokenCredentials);
            serviceBusManagementClient.SubscriptionId = selectedSubscription.SubscriptionId;

            List<SharedAccessAuthorizationRuleInner> serviceBusList = new List<SharedAccessAuthorizationRuleInner>();

            // Case 1. When user used existing service bus.
            Thread existingServiceBusThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    var resp = serviceBusManagementClient.Namespaces.ListAuthorizationRules(rgName, selectedServiceBus.Name);
                    serviceBusList.AddRange(resp);

                    while (!string.IsNullOrEmpty(resp.NextPageLink))
                    {
                        resp = serviceBusManagementClient.Namespaces.ListAuthorizationRulesNext(resp.NextPageLink);
                        serviceBusList.AddRange(resp);
                    }

                    var selectedAuthRule = serviceBusList.FirstOrDefault(rule => rule.Rights != null && rule.Rights.Contains(AccessRights.Listen) && rule.Rights.Contains(AccessRights.Manage) && rule.Rights.Contains(AccessRights.Send));

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
                        serviceBusManagementClient.Namespaces.ListKeys(rgName, selectedServiceBus.Name, selectedAuthRule.Name);

                        ApplicationData.Instance.ServiceBusSharedKey = serviceBusManagementClient.Namespaces.ListKeys(rgName, selectedServiceBus.Name, selectedAuthRule.Name).PrimaryKey;
                        ApplicationData.Instance.ServiceBusKeyName = serviceBusManagementClient.Namespaces.ListKeys(rgName, selectedServiceBus.Name, selectedAuthRule.Name).KeyName;
                        ApplicationData.Instance.ServiceBusUrl = selectedServiceBus.ServiceBusEndpoint;

                        this.Dispatcher.Invoke(() =>
                        {
                            MainWindow mainWindow = new MainWindow();
                            mainWindow.Show();
                            this.Close();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(CallInfo.Site(), ex, "Failed to connect to service bus namespace");

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
                        resourceGroup = resourceManagementClient.ResourceGroups.Get("TunnelRelay");
                    }
                    catch (Microsoft.Rest.Azure.CloudException httpEx)
                    {
                        if (httpEx.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            resourceGroup = resourceManagementClient.ResourceGroups.CreateOrUpdate("TunnelRelay", new RM.Models.ResourceGroupInner
                            {
                                Location = "WestUS",
                                Name = "TunnelRelay",
                                Tags = newServiceBus.Tags,
                            });
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

                    selectedServiceBus = serviceBusManagementClient.Namespaces.CreateOrUpdate(rgName, newBusName, new NamespaceModelInner
                    {
                        Location = selectedServiceBus.Location,
                        Sku = selectedServiceBus.Sku,
                        Tags = selectedServiceBus.Tags,
                    });

                    var resp = serviceBusManagementClient.Namespaces.ListAuthorizationRules(rgName, selectedServiceBus.Name);
                    serviceBusList.AddRange(resp);

                    while (!string.IsNullOrEmpty(resp.NextPageLink))
                    {
                        resp = serviceBusManagementClient.Namespaces.ListAuthorizationRulesNext(resp.NextPageLink);
                        serviceBusList.AddRange(resp);
                    }

                    var selectedAuthRule = serviceBusList.FirstOrDefault(rule => rule.Rights != null && rule.Rights.Contains(AccessRights.Listen) && rule.Rights.Contains(AccessRights.Manage) && rule.Rights.Contains(AccessRights.Send));

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
                        ApplicationData.Instance.ServiceBusSharedKey = serviceBusManagementClient.Namespaces.ListKeys(rgName, selectedServiceBus.Name, selectedAuthRule.Name).PrimaryKey;
                        ApplicationData.Instance.ServiceBusKeyName = serviceBusManagementClient.Namespaces.ListKeys(rgName, selectedServiceBus.Name, selectedAuthRule.Name).KeyName;
                        ApplicationData.Instance.ServiceBusUrl = selectedServiceBus.ServiceBusEndpoint;

                        this.Dispatcher.Invoke(() =>
                        {
                            MainWindow mainWindow = new MainWindow();
                            mainWindow.Show();
                            this.Close();
                        });
                    }
                }
                catch (CloudException cloudEx)
                {
                    Logger.LogError(CallInfo.Site(), cloudEx, "Cloud exception while creating service bus namespace.");

                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(cloudEx.Message, "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                        this.progressBar.Visibility = Visibility.Hidden;
                        this.btnDone.IsEnabled = true;
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(CallInfo.Site(), ex, "Failed to create new service bus namespace");

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
    }
}

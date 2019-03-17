﻿// <copyright file="SelectServiceBus.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Azure.Management.Relay.Fluent.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Rest.Azure;
    using TunnelRelay.UI.ResourceManagement;
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
        private readonly UserAuthenticator userAuthenticator;

        /// <summary>
        /// Service bus resource manager.
        /// </summary>
        private readonly ServiceBusResourceManager serviceBusResourceManager;

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
        internal SelectServiceBus(UserAuthenticator authenticationDetails)
        {
            this.ContentRendered += this.Window_ContentRendered;
            this.InitializeComponent();
            this.userAuthenticator = authenticationDetails;
            this.serviceBusResourceManager = new ServiceBusResourceManager(this.userAuthenticator);

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
                    List<RM.Models.SubscriptionInner> subscriptionList = this.userAuthenticator.GetUserSubscriptionsAsync().ConfigureAwait(false).GetAwaiter().GetResult();

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
            RM.Models.SubscriptionInner selectedSubscription = (sender as ComboBox).SelectedItem as RM.Models.SubscriptionInner;

            Thread serviceBusThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    List<RelayNamespaceInner> serviceBusList = this.serviceBusResourceManager.GetRelayNamespacesAsync(selectedSubscription).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.Dispatcher.Invoke(() =>
                    {
                        this.serviceBuses.Clear();

                        // Add a fake service bus. This guides people to create a new one.
                        this.serviceBuses.Add(newServiceBus);
                        serviceBusList.ForEach(sub => this.serviceBuses.Add(sub));
                        this.comboServiceBusList.IsEnabled = true;
                        this.progressBar.Visibility = Visibility.Hidden;

                        this.listBoxSubscriptionLocations.Items.Clear();

                        foreach (RM.Models.Location location in this.userAuthenticator.GetSubscriptionLocations(selectedSubscription))
                        {
                            this.listBoxSubscriptionLocations.Items.Add(location.DisplayName);
                        }

                        this.listBoxSubscriptionLocations.SelectedIndex = 0;
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
            RM.Models.SubscriptionInner selectedSubscription = (this.comboSubscriptionList as ComboBox).SelectedItem as RM.Models.SubscriptionInner;
            RelayNamespaceInner selectedServiceBus = (sender as ComboBox).SelectedItem as RelayNamespaceInner;

            // Selected service bus Id is null when it is the value we added manually i.e. newServiceBus above.
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
            RM.Models.SubscriptionInner selectedSubscription = (this.comboSubscriptionList as ComboBox).SelectedItem as RM.Models.SubscriptionInner;
            RelayNamespaceInner selectedServiceBus = (this.comboServiceBusList as ComboBox).SelectedItem as RelayNamespaceInner;
            string selectedLocation = this.listBoxSubscriptionLocations.SelectedItem.ToString();

            string newBusName = this.txtServiceBusName.Text;

            // Case 1. When user used existing service bus.
            Thread existingServiceBusThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    HybridConnectionDetails hybridConnectionDetails = this.serviceBusResourceManager.GetHybridConnectionAsync(selectedSubscription, selectedServiceBus, Environment.MachineName).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.SetApplicationData(hybridConnectionDetails);
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

                    HybridConnectionDetails hybridConnectionDetails = this.serviceBusResourceManager.CreateHybridConnectionAsync(
                        selectedSubscription,
                        newBusName,
                        Environment.MachineName,
                        selectedLocation).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.SetApplicationData(hybridConnectionDetails);
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

            // If the user selected to new service bus entry we added.
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
        /// Sets the application data based on hybrid connection details.
        /// </summary>
        /// <param name="hybridConnectionDetails">Hybrid connection details.</param>
        private void SetApplicationData(HybridConnectionDetails hybridConnectionDetails)
        {
            bool encryptionEnabled = true;

            this.Dispatcher.Invoke(() =>
            {
                encryptionEnabled = this.chkEnableEncryption.IsChecked.GetValueOrDefault();
            });

            TunnelRelayStateManager.ApplicationData.EnableCredentialEncryption = encryptionEnabled;
            TunnelRelayStateManager.ApplicationData.HybridConnectionSharedKey = hybridConnectionDetails.HybridConnectionSharedKey;
            TunnelRelayStateManager.ApplicationData.HybridConnectionKeyName = hybridConnectionDetails.HybridConnectionKeyName;
            TunnelRelayStateManager.ApplicationData.HybridConnectionUrl = hybridConnectionDetails.ServiceBusUrl;
            TunnelRelayStateManager.ApplicationData.HybridConnectionName = hybridConnectionDetails.HybridConnectionName;

            this.Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            });
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

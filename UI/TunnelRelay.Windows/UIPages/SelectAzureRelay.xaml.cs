// <copyright file="SelectAzureRelay.xaml.cs" company="Microsoft Corporation">
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
    public partial class SelectAzureRelay : Window
    {
        /// <summary>
        /// The new relay template.
        /// </summary>
        private static RelayNamespaceInner newRelay = new RelayNamespaceInner("WestUS", null, "Create a new Azure Relay")
        {
            Sku = new Sku(SkuTier.Standard),
            Tags = new Dictionary<string, string>() { { "CreatedBy", "TunnelRelayv2" } },
        };

        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<SelectAzureRelay> logger = LoggingHelper.GetLogger<SelectAzureRelay>();

        /// <summary>
        /// User authentication manager.
        /// </summary>
        private readonly UserAuthenticator userAuthenticator;

        /// <summary>
        /// Relay resource manager.
        /// </summary>
        private readonly AzureRelayResourceManager relayResourceManager;

        /// <summary>
        /// List of Azure subscription.
        /// </summary>
        private ObservableCollection<RM.Models.SubscriptionInner> subscriptions = new ObservableCollection<RM.Models.SubscriptionInner>();

        /// <summary>
        /// List of Azure relays.
        /// </summary>
        private ObservableCollection<RelayNamespaceInner> relays = new ObservableCollection<RelayNamespaceInner>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectAzureRelay"/> class.
        /// </summary>
        /// <param name="authenticationDetails">The authentication details for the user.</param>
        internal SelectAzureRelay(UserAuthenticator authenticationDetails)
        {
            this.ContentRendered += this.Window_ContentRendered;
            this.InitializeComponent();
            this.userAuthenticator = authenticationDetails;
            this.relayResourceManager = new AzureRelayResourceManager(this.userAuthenticator);

            this.comboSubscriptionList.ItemsSource = this.subscriptions;
            this.comboAzureRelayList.ItemsSource = this.relays;

            // Disable controls to begin with.
            this.comboSubscriptionList.IsEnabled = false;
            this.comboAzureRelayList.IsEnabled = false;
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

            Thread relayThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    List<RelayNamespaceInner> relayList = this.relayResourceManager.GetRelayNamespacesAsync(selectedSubscription).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.Dispatcher.Invoke(() =>
                    {
                        this.relays.Clear();

                        // Add a fake relay. This guides people to create a new one.
                        this.relays.Add(newRelay);
                        relayList.ForEach(sub => this.relays.Add(sub));
                        this.comboAzureRelayList.IsEnabled = true;
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
                    this.logger.LogError(ex, "Failed to get list of Azure Relay namespaces");

                    this.Dispatcher.Invoke(() => MessageBox.Show("Failed to get list of Azure Relay namespaces!!. Exiting", "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error));
                    Application.Current.Shutdown();
                }
            }));

            relayThread.Start();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the AzureRelayList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void AzureRelayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RelayNamespaceInner selectedAzureRelay = (sender as ComboBox).SelectedItem as RelayNamespaceInner;

            // Selected Relay Id is null when it is the value we added manually i.e. newRelay above.
            if (selectedAzureRelay.Id == null)
            {
                this.lblAzureRelayName.Visibility = Visibility.Visible;

                if (!string.IsNullOrEmpty(this.txtAzureRelayName.Text))
                {
                    this.btnDone.IsEnabled = true;
                }

                return;
            }
            else
            {
                this.lblAzureRelayName.Visibility = Visibility.Collapsed;
                this.btnDone.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the Done control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.progressBar.Visibility = Visibility.Visible;
            this.btnDone.IsEnabled = false;
            RM.Models.SubscriptionInner selectedSubscription = (this.comboSubscriptionList as ComboBox).SelectedItem as RM.Models.SubscriptionInner;
            RelayNamespaceInner selectedAzureRelay = (this.comboAzureRelayList as ComboBox).SelectedItem as RelayNamespaceInner;
            string selectedLocation = this.listBoxSubscriptionLocations.SelectedItem.ToString();

            string newRelayName = this.txtAzureRelayName.Text;

            // Case 1. When user used existing Relay.
            Thread existingAzureRelayThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    HybridConnectionDetails hybridConnectionDetails = this.relayResourceManager.GetHybridConnectionAsync(selectedSubscription, selectedAzureRelay, Environment.MachineName).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.SetApplicationData(hybridConnectionDetails);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to connect to relay namespace");

                    this.Dispatcher.Invoke(() => MessageBox.Show("Failed to connect to Azure Relay namespace!!", "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error));
                }
            }));

            // Case 2. When user created a new Relay.
            Thread newAzureRelayThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(newRelayName))
                    {
                        MessageBox.Show("Please enter the name for Azure Relay.");
                        this.Dispatcher.Invoke(() =>
                        {
                            this.progressBar.Visibility = Visibility.Hidden;
                            this.btnDone.IsEnabled = true;
                        });
                        return;
                    }

                    if (newRelayName.Length < 6)
                    {
                        MessageBox.Show("Name of Azure Relay must be at least 6 characters.");
                        this.Dispatcher.Invoke(() =>
                        {
                            this.progressBar.Visibility = Visibility.Hidden;
                            this.btnDone.IsEnabled = true;
                        });
                        return;
                    }

                    HybridConnectionDetails hybridConnectionDetails = this.relayResourceManager.CreateHybridConnectionAsync(
                        selectedSubscription,
                        newRelayName,
                        Environment.MachineName,
                        selectedLocation).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.SetApplicationData(hybridConnectionDetails);
                }
                catch (CloudException cloudEx)
                {
                    this.logger.LogError(cloudEx, "Cloud exception while creating Azure Relay namespace.");

                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(cloudEx.Message, "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                        this.progressBar.Visibility = Visibility.Hidden;
                        this.btnDone.IsEnabled = true;
                    });
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to create new Azure Relay namespace");

                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Failed to create new Azure Relay namespace!!", "Azure Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                        this.progressBar.Visibility = Visibility.Hidden;
                        this.btnDone.IsEnabled = true;
                    });
                }
            }));

            // If the user selected to new Relay entry we added.
            if (selectedAzureRelay.Id == null)
            {
                newAzureRelayThread.Start();
            }
            else
            {
                existingAzureRelayThread.Start();
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the AzureRelayName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private void AzureRelayName_TextChanged(object sender, TextChangedEventArgs e)
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
            TunnelRelayStateManager.ApplicationData.HybridConnectionUrl = hybridConnectionDetails.RelayUrl;
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
                "WARNING: This will disable Azure Relay Shared Access Secret encryption, if the configuration is copied out of this machine " +
                    "it can be used by someone else to connect to the Azure Relay. Do you want to continue?",
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

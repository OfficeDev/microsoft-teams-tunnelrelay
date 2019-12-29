// <copyright file="ServiceBusResourceManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.Relay.Fluent;
    using Microsoft.Azure.Management.Relay.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure;
    using RM = Microsoft.Azure.Management.ResourceManager.Fluent;

    /// <summary>
    /// Service bus resource manager. Encapsulates calls being made to Azure Resource Manager for Service bus.
    /// </summary>
    internal class ServiceBusResourceManager
    {
        private readonly UserAuthenticator userAuthenticator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusResourceManager"/> class.
        /// </summary>
        /// <param name="userAuthenticator">User authenticator.</param>
        public ServiceBusResourceManager(UserAuthenticator userAuthenticator)
        {
            this.userAuthenticator = userAuthenticator ?? throw new ArgumentNullException(nameof(userAuthenticator));
        }

        /// <summary>
        /// Gets the relay namespaces present in a subscription.
        /// </summary>
        /// <param name="subscription">Subscription to get namespaces from.</param>
        /// <returns>List of relay namespaces.</returns>
        public async Task<List<RelayNamespaceInner>> GetRelayNamespacesAsync(SubscriptionInner subscription)
        {
            string accessToken = this.userAuthenticator.GetSubscriptionSpecificUserToken(subscription);

            TokenCredentials tokenCredentials = new TokenCredentials(accessToken);
            RelayManagementClient relayManagementClient = new RelayManagementClient(tokenCredentials)
            {
                SubscriptionId = subscription.SubscriptionId,
            };

            List<RelayNamespaceInner> serviceBusList = new List<RelayNamespaceInner>();
            IPage<RelayNamespaceInner> resp = await relayManagementClient.Namespaces.ListAsync().ConfigureAwait(false);
            serviceBusList.AddRange(resp);

            while (!string.IsNullOrEmpty(resp.NextPageLink))
            {
                resp = await relayManagementClient.Namespaces.ListNextAsync(resp.NextPageLink).ConfigureAwait(false);
                serviceBusList.AddRange(resp);
            }

            return serviceBusList.OrderBy(bus => bus.Name).ToList();
        }

        /// <summary>
        /// Get hybrid connection details async.
        /// </summary>
        /// <param name="subscription">Selected subscription.</param>
        /// <param name="relayNamespace">Selected Service bus namespace.</param>
        /// <param name="hybridConnectionName">Hybrid connection name.</param>
        /// <returns>Hybrid connection details.</returns>
        public async Task<HybridConnectionDetails> GetHybridConnectionAsync(
            SubscriptionInner subscription,
            RelayNamespaceInner relayNamespace,
            string hybridConnectionName)
        {
            string accessToken = this.userAuthenticator.GetSubscriptionSpecificUserToken(subscription);

            TokenCredentials tokenCredentials = new TokenCredentials(accessToken);
            RelayManagementClient relayManagementClient = new RelayManagementClient(tokenCredentials)
            {
                SubscriptionId = subscription.SubscriptionId,
            };

            int startIndex = relayNamespace.Id.IndexOf("resourceGroups", StringComparison.OrdinalIgnoreCase) + 15;
            string rgName = rgName = relayNamespace.Id.Substring(startIndex, relayNamespace.Id.IndexOf('/', startIndex) - startIndex);

            List<HybridConnectionInner> hybridConnections = new List<HybridConnectionInner>();
            hybridConnections.AddRange(await relayManagementClient.HybridConnections.ListByNamespaceAsync(rgName, relayNamespace.Name).ConfigureAwait(false));

            // Create the hybrid connection if one does not exist.
            if (!hybridConnections.Any(connection => connection.Name.Equals(hybridConnectionName, StringComparison.OrdinalIgnoreCase)))
            {
                await relayManagementClient.HybridConnections.CreateOrUpdateAsync(rgName, relayNamespace.Name, hybridConnectionName, new HybridConnectionInner
                {
                    RequiresClientAuthorization = false,
                }).ConfigureAwait(false);
            }

            return await ServiceBusResourceManager.GetHybridConnectionDetailsAsync(rgName, hybridConnectionName, relayManagementClient, relayNamespace).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new hybrid connection and returns it's details.
        /// </summary>
        /// <param name="subscription">Subscription to create the relay under.</param>
        /// <param name="serviceBusName">Service Bus name.</param>
        /// <param name="hybridConnectionName">Hybrid connection name.</param>
        /// <param name="locationName">Location for the new relay.</param>
        /// <returns>Hybrid connection details.</returns>
        public async Task<HybridConnectionDetails> CreateHybridConnectionAsync(
            SubscriptionInner subscription,
            string serviceBusName,
            string hybridConnectionName,
            string locationName)
        {
            string accessToken = this.userAuthenticator.GetSubscriptionSpecificUserToken(subscription);

            TokenCredentials tokenCredentials = new TokenCredentials(accessToken);

            ResourceManagementClient resourceManagementClient = new ResourceManagementClient(tokenCredentials)
            {
                SubscriptionId = subscription.SubscriptionId,
            };

            RelayManagementClient relayManagementClient = new RelayManagementClient(tokenCredentials)
            {
                SubscriptionId = subscription.SubscriptionId,
            };

            ResourceGroupInner resourceGroup = null;
            RelayNamespaceInner relayNamespace = null;

            Location location = this.userAuthenticator.GetSubscriptionLocations(subscription).First(region =>
                region.DisplayName.Equals(locationName, StringComparison.OrdinalIgnoreCase) ||
                region.Name.Equals(locationName, StringComparison.OrdinalIgnoreCase));

            try
            {
                resourceGroup = await resourceManagementClient.ResourceGroups.GetAsync("TunnelRelay").ConfigureAwait(false);
            }
            catch (CloudException httpEx)
            {
                if (httpEx.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    resourceGroup = await resourceManagementClient.ResourceGroups.CreateOrUpdateAsync("TunnelRelay", new ResourceGroupInner
                    {
                        Location = location.Name,
                        Name = "TunnelRelay",
                        Tags = new Dictionary<string, string>() { { "CreatedBy", "TunnelRelayv2" } },
                    }).ConfigureAwait(false);
                }
            }

            string rgName = resourceGroup.Name;

            relayNamespace = await relayManagementClient.Namespaces.CreateOrUpdateAsync(rgName, serviceBusName, new RelayNamespaceInner
            {
                Location = resourceGroup.Location,
                Sku = new Microsoft.Azure.Management.Relay.Fluent.Models.Sku(SkuTier.Standard),
                Tags = new Dictionary<string, string>() { { "CreatedBy", "TunnelRelayv2" } },
            }).ConfigureAwait(false);

            await relayManagementClient.HybridConnections.CreateOrUpdateAsync(rgName, serviceBusName, hybridConnectionName, new HybridConnectionInner
            {
                RequiresClientAuthorization = false,
            }).ConfigureAwait(false);

            return await ServiceBusResourceManager.GetHybridConnectionDetailsAsync(rgName, hybridConnectionName, relayManagementClient, relayNamespace).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the auth rules for a service bus and return <see cref="HybridConnectionDetails"/> instance containing details of hybrid connection.
        /// </summary>
        /// <param name="rgName">Name of the Resource Group.</param>
        /// <param name="hybridConnectionName">Hybrid connection name.</param>
        /// <param name="relayManagementClient">The service bus management client.</param>
        /// <param name="relayNamespace">The selected service bus.</param>
        private static async Task<HybridConnectionDetails> GetHybridConnectionDetailsAsync(
            string rgName,
            string hybridConnectionName,
            RelayManagementClient relayManagementClient,
            RelayNamespaceInner relayNamespace)
        {
            List<AuthorizationRuleInner> serviceBusAuthRuleList = new List<AuthorizationRuleInner>();
            IPage<AuthorizationRuleInner> resp = await relayManagementClient.Namespaces.ListAuthorizationRulesAsync(rgName, relayNamespace.Name).ConfigureAwait(false);
            serviceBusAuthRuleList.AddRange(resp);

            while (!string.IsNullOrEmpty(resp.NextPageLink))
            {
                resp = await relayManagementClient.Namespaces.ListAuthorizationRulesNextAsync(resp.NextPageLink).ConfigureAwait(false);
                serviceBusAuthRuleList.AddRange(resp);
            }

            AuthorizationRuleInner selectedAuthRule = serviceBusAuthRuleList.FirstOrDefault(rule => rule.Rights != null && rule.Rights.Contains(AccessRights.Listen) && rule.Rights.Contains(AccessRights.Manage) && rule.Rights.Contains(AccessRights.Send));

            if (selectedAuthRule == null)
            {
                throw new InvalidOperationException("Failed to find a suitable Authorization rule to use. Please create an Authorization rule with Listen, Manage and Send rights and retry the operation");
            }
            else
            {
                AccessKeysInner keyInfo = await relayManagementClient.Namespaces.ListKeysAsync(
                    rgName,
                    relayNamespace.Name,
                    selectedAuthRule.Name).ConfigureAwait(false);
                return new HybridConnectionDetails
                {
                    HybridConnectionKeyName = keyInfo.KeyName,
                    HybridConnectionName = hybridConnectionName,
                    HybridConnectionSharedKey = keyInfo.PrimaryKey,
                    ServiceBusUrl = relayNamespace.ServiceBusEndpoint,
                };
            }
        }
    }
}

// <copyright file="UserAuthenticator.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.ResourceManagement
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure;

    /// <summary>
    /// User authentication details.
    /// </summary>
    internal class UserAuthenticator
    {
        /// <summary>
        /// The aad login authority.
        /// </summary>
        private const string AADLoginAuthority = "https://login.microsoftonline.com/";

        /// <summary>
        /// The azure aad resource.
        /// </summary>
        private const string AzureAADResource = "https://management.azure.com/";

        /// <summary>
        /// The ps native client id to get desktop tokens.
        /// </summary>
        private const string PSClientId = "1950a258-227b-4e31-a9cf-717495945fc2";

        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<UserAuthenticator> logger;

        /// <summary>
        /// The tenant based token map. This stores tokens Tenant wise.
        /// </summary>
        private readonly ConcurrentDictionary<string, AuthenticationResult> tenantBasedTokenMap = new ConcurrentDictionary<string, AuthenticationResult>();

        /// <summary>
        /// The subscription to tenant map. Stores the list of subscription alongside their tenant ids.
        /// </summary>
        private readonly ConcurrentDictionary<SubscriptionInner, TenantIdDescription> subscriptionToTenantMap = new ConcurrentDictionary<SubscriptionInner, TenantIdDescription>();

        /// <summary>
        /// The subscription to location map.
        /// </summary>
        private readonly ConcurrentDictionary<SubscriptionInner, IEnumerable<Location>> subscriptionToLocationMap = new ConcurrentDictionary<SubscriptionInner, IEnumerable<Location>>();

        /// <summary>
        /// The user identifier.
        /// </summary>
        private UserIdentifier userIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAuthenticator"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public UserAuthenticator(ILogger<UserAuthenticator> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        /// <returns>Task tracking operation.</returns>
        public async Task AuthenticateUserAsync()
        {
            // If user is already authenticated skip authenticating the user.
            if (this.tenantBasedTokenMap.Count == 0)
            {
                this.logger.LogInformation("Logging the user in with Common tenant info");
                AuthenticationResult authToken = await this.AcquireAzureManagementTokenAsync("common", PromptBehavior.RefreshSession).ConfigureAwait(false);

                this.userIdentifier = new UserIdentifier(authToken.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId);
                this.tenantBasedTokenMap[authToken.TenantId] = authToken;
            }
        }

        /// <summary>
        /// Gets the user subscriptions.
        /// User might have subscriptions in various tenants. So the trick is to use any one token to get tenant list and then get tenant based tokens.
        /// Then these are used to fetch subscriptions and other resources.
        /// </summary>
        /// <returns>List of subscriptions.</returns>
        public async Task<List<SubscriptionInner>> GetUserSubscriptionsAsync()
        {
            // If this is the first time we are fetching Subscriptions (now the user might have 0 subscriptions altogether but chances are comparatively low).
            if (this.subscriptionToTenantMap.Count == 0)
            {
                await this.AuthenticateUserAsync().ConfigureAwait(false);

                TokenCredentials tenantCreds = new TokenCredentials(this.tenantBasedTokenMap.First().Value.AccessToken);
                SubscriptionClient tenantClient = new SubscriptionClient(tenantCreds);

                List<TenantIdDescription> tenantList = new List<TenantIdDescription>();

                IPage<TenantIdDescription> tenantListResp = await tenantClient.Tenants.ListAsync().ConfigureAwait(false);
                tenantList.AddRange(tenantListResp);

                while (!string.IsNullOrEmpty(tenantListResp.NextPageLink))
                {
                    tenantListResp = await tenantClient.Tenants.ListNextAsync(tenantListResp.NextPageLink).ConfigureAwait(false);
                    tenantList.AddRange(tenantListResp);
                }

                List<Task> tokenAcquireTasks = new List<Task>();

                // Get tokens for all tenants.
                tenantList.ForEach(
                    (tenant) =>
                    {
                        tokenAcquireTasks.Add(Task.Run(async () =>
                        {
                            // Optimization to skip refetching tokens. AAD tokens live for 1 hour.
                            if (!this.tenantBasedTokenMap.ContainsKey(tenant.TenantId))
                            {
                                this.logger.LogInformation("Get token with '{0}' tenant info", tenant.TenantId);
                                this.tenantBasedTokenMap[tenant.TenantId] = await this.AcquireAzureManagementTokenAsync(tenant.TenantId, PromptBehavior.Never, this.userIdentifier).ConfigureAwait(false);
                            }
                        }));
                    });

                await Task.WhenAll(tokenAcquireTasks).ConfigureAwait(false);

                // Get all subscriptions for given tenants
                List<Task> subscriptionTasks = new List<Task>();
                tenantList.ForEach(
                    (tenant) =>
                     {
                         subscriptionTasks.Add(Task.Run(async () =>
                         {
                             List<SubscriptionInner> subscriptionList = new List<SubscriptionInner>();
                             this.logger.LogTrace("Getting subscriptions for '{0}' tenant.", tenant.TenantId);
                             TokenCredentials subsCreds = new TokenCredentials(this.tenantBasedTokenMap[tenant.TenantId].AccessToken);
                             SubscriptionClient subscriptionClient = new SubscriptionClient(subsCreds);

                             IPage<SubscriptionInner> resp = await subscriptionClient.Subscriptions.ListAsync().ConfigureAwait(false);
                             subscriptionList.AddRange(resp);

                             while (!string.IsNullOrEmpty(resp.NextPageLink))
                             {
                                 resp = await subscriptionClient.Subscriptions.ListNextAsync(resp.NextPageLink).ConfigureAwait(false);
                                 subscriptionList.AddRange(resp);
                             }

                             this.logger.LogTrace("Fetched total of '{0}' subscriptions for tenant '{1}'", subscriptionList.Count, tenant.TenantId);

                             subscriptionList.ForEach(subscription => this.subscriptionToTenantMap[subscription] = tenant);
                         }));
                     });

                await Task.WhenAll(subscriptionTasks).ConfigureAwait(false);

                List<Task> locationTasks = new List<Task>();
                foreach (KeyValuePair<SubscriptionInner, TenantIdDescription> subscription in this.subscriptionToTenantMap)
                {
                    locationTasks.Add(Task.Run(async () =>
                    {
                        TokenCredentials subsCreds = new TokenCredentials(this.tenantBasedTokenMap[subscription.Value.TenantId].AccessToken);
                        SubscriptionClient subscriptionClient = new SubscriptionClient(subsCreds);

                        IEnumerable<Location> locations = await subscriptionClient.Subscriptions.ListLocationsAsync(subscription.Key.SubscriptionId).ConfigureAwait(false);

                        this.subscriptionToLocationMap[subscription.Key] = locations;
                    }));
                }

                await Task.WhenAll(locationTasks).ConfigureAwait(false);
            }

            return this.subscriptionToTenantMap.Keys.OrderBy(subs => subs.DisplayName).ToList();
        }

        /// <summary>
        /// Gets the locations supported by a subscription.
        /// </summary>
        /// <param name="subscription">Selected subscription.</param>
        /// <returns>Locations supported by the subscription.</returns>
        public IEnumerable<Location> GetSubscriptionLocations(SubscriptionInner subscription)
        {
            return this.subscriptionToLocationMap[subscription].OrderBy(loc => loc.Name);
        }

        /// <summary>
        /// Gets the subscription specific user token.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <returns>Authentication token.</returns>
        public AuthenticationResult GetSubscriptionSpecificUserToken(SubscriptionInner subscription)
        {
            return this.tenantBasedTokenMap[this.subscriptionToTenantMap[subscription].TenantId];
        }

        /// <summary>
        /// Acquires the Azure management token asynchronously.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="promptBehavior">The prompt behavior.</param>
        /// <param name="userIdentifier">Optional user for which token is to be fetched.</param>
        /// <returns>AAD authentication result.</returns>
        private async Task<AuthenticationResult> AcquireAzureManagementTokenAsync(
            string tenantId,
            PromptBehavior promptBehavior = PromptBehavior.Never,
            UserIdentifier userIdentifier = null)
        {
            AuthenticationContext authenticationContext = new AuthenticationContext(AADLoginAuthority + tenantId, false, TokenCache.DefaultShared);
            try
            {
                if (userIdentifier == null)
                {
#if NET461
                    return await authenticationContext.AcquireTokenAsync(
                        AzureAADResource,
                        PSClientId,
                        new Uri("urn:ietf:wg:oauth:2.0:oob"),
                        new PlatformParameters(promptBehavior)).ConfigureAwait(false);
#else
                    DeviceCodeResult deviceCodeResult = await authenticationContext.AcquireDeviceCodeAsync(AzureAADResource, PSClientId).ConfigureAwait(false);

                    Console.WriteLine($"Interactive login required. {deviceCodeResult.Message}");

                    await Task.Delay(1000).ConfigureAwait(false);

                    // Open the browser with the url.
                    ProcessStartInfo processStartInfo = new ProcessStartInfo
                    {
                        FileName = deviceCodeResult.VerificationUrl,
                        UseShellExecute = true,
                    };

                    Process.Start(processStartInfo);

                    return await authenticationContext.AcquireTokenByDeviceCodeAsync(deviceCodeResult).ConfigureAwait(false);
#endif
                }
                else
                {
                    return await authenticationContext.AcquireTokenSilentAsync(
                        AzureAADResource,
                        PSClientId,
                        userIdentifier).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to acquire token for tenant Id '{0}'", tenantId);
                throw;
            }
        }
    }
}

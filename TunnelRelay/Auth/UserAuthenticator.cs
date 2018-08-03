// <copyright file="UserAuthenticator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TunnelRelay
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;

    /// <summary>
    /// User authentication details.
    /// </summary>
    public class UserAuthenticator
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
        /// Aap redirection url for local desktop.
        /// </summary>
        private static readonly Uri PSRedirectUrl = new Uri("urn:ietf:wg:oauth:2.0:oob");

        /// <summary>
        /// The user identifier.
        /// </summary>
        private UserIdentifier userIdentifier;

        /// <summary>
        /// The tenant based token map. This stores tokens Tenant wise.
        /// </summary>
        private ConcurrentDictionary<string, AuthenticationResult> tenantBasedTokenMap = new ConcurrentDictionary<string, AuthenticationResult>();

        /// <summary>
        /// The subscription to tenant map. Stores the list of subscription alongside their tenant ids.
        /// </summary>
        private ConcurrentDictionary<SubscriptionInner, TenantIdDescription> subscriptionToTenantMap = new ConcurrentDictionary<SubscriptionInner, TenantIdDescription>();

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        public void AuthenticateUser()
        {
            // If user is already authenticated skip authenticating the user.
            if (this.tenantBasedTokenMap.Count == 0)
            {
                Logger.LogInfo(CallInfo.Site(), "Logging the user in with Common tenant info");
                AuthenticationContext authContext = new AuthenticationContext("https://login.microsoftonline.com/common", false, TokenCache.DefaultShared);

                AuthenticationResult authToken = this.AcquireGraphToken("common", PromptBehavior.RefreshSession);

                this.userIdentifier = new UserIdentifier(authToken.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId);
                this.tenantBasedTokenMap[authToken.TenantId] = authToken;
            }
        }

        /// <summary>
        /// Gets the user subscriptions.
        /// User might have subscriptions in various tenants. So the trick is to use any one token to get tenant list and then get tenant based tokens.
        /// Then these are used to fetch subscriptions and other resources.
        /// </summary>
        /// <returns>List of subscriptions</returns>
        public List<SubscriptionInner> GetUserSubscriptions()
        {
            // If this is the first time we are fetching Subscriptions (now the user might have 0 subscriptions altogether but chances are comparatively low).
            if (this.subscriptionToTenantMap.Count == 0)
            {
                this.AuthenticateUser();

                TokenCredentials tenantCreds = new TokenCredentials(this.tenantBasedTokenMap.First().Value.AccessToken);
                SubscriptionClient tenantClient = new SubscriptionClient(tenantCreds);

                List<TenantIdDescription> tenantList = new List<TenantIdDescription>();

                var tenantListResp = tenantClient.Tenants.ListAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                tenantList.AddRange(tenantListResp);

                while (!string.IsNullOrEmpty(tenantListResp.NextPageLink))
                {
                    tenantListResp = tenantClient.Tenants.ListNextAsync(tenantListResp.NextPageLink).ConfigureAwait(false).GetAwaiter().GetResult();
                    tenantList.AddRange(tenantListResp);
                }

                // Get tokens for all tenants.
                Parallel.ForEach(
                    tenantList,
                    (tenant) =>
                    {
                        // Optimization to skip refetching tokens. AAD tokens live for 1 hour.
                         if (!this.tenantBasedTokenMap.ContainsKey(tenant.TenantId))
                        {
                            Logger.LogInfo(CallInfo.Site(), "Get token with '{0}' tenant info", tenant.TenantId);
                            this.tenantBasedTokenMap[tenant.TenantId] = this.AcquireGraphToken(tenant.TenantId, PromptBehavior.Never, this.userIdentifier);
                        }
                    });

                // Get all subscriptions for given tenants
                Parallel.ForEach(
                     tenantList,
                     (tenant) =>
                     {
                         List<SubscriptionInner> subscriptionList = new List<SubscriptionInner>();
                         Logger.LogVerbose(CallInfo.Site(), "Getting subscriptions for '{0}' tenant.", tenant.TenantId);
                         TokenCredentials subsCreds = new TokenCredentials(this.tenantBasedTokenMap[tenant.TenantId].AccessToken);
                         SubscriptionClient subscriptionClient = new SubscriptionClient(subsCreds);

                         var resp = subscriptionClient.Subscriptions.ListAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                         subscriptionList.AddRange(resp);

                         while (!string.IsNullOrEmpty(resp.NextPageLink))
                         {
                             resp = subscriptionClient.Subscriptions.ListNextAsync(resp.NextPageLink).ConfigureAwait(false).GetAwaiter().GetResult();
                             subscriptionList.AddRange(resp);
                         }

                         Logger.LogVerbose(CallInfo.Site(), "Fetched total of '{0}' subscriptions for tenant '{1}'", subscriptionList.Count, tenant.TenantId);

                         subscriptionList.ForEach(subscription => this.subscriptionToTenantMap[subscription] = tenant);
                     });
            }

            return this.subscriptionToTenantMap.Keys.ToList();
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
        /// Acquires the graph token.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="promptBehavior">The prompt behavior.</param>
        /// <param name="userIdentifier">Optional user for which token is to be fetched.</param>
        /// <returns>AAD authentication result.</returns>
        private AuthenticationResult AcquireGraphToken(string tenantId, PromptBehavior promptBehavior = PromptBehavior.Never, UserIdentifier userIdentifier = null)
        {
            try
            {
                if (userIdentifier == null)
                {
                    return new AuthenticationContext(AADLoginAuthority + tenantId, false, TokenCache.DefaultShared).AcquireTokenAsync(
                        AzureAADResource,
                        PSClientId,
                        PSRedirectUrl,
                        new PlatformParameters(promptBehavior)).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                else
                {
                    return new AuthenticationContext(AADLoginAuthority + tenantId, false, TokenCache.DefaultShared).AcquireTokenAsync(
                        AzureAADResource,
                        PSClientId,
                        PSRedirectUrl,
                        new PlatformParameters(promptBehavior),
                        userIdentifier).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(CallInfo.Site(), ex, "Failed to acquire token for tenant Id '{0}'", tenantId);
                throw;
            }
        }
    }
}

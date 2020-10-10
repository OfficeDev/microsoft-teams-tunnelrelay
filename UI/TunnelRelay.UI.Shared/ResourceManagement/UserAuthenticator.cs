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
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
#if USEADAL
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
#else
    using Microsoft.Identity.Client;
#endif
    using Microsoft.Rest;
    using Microsoft.Rest.Azure;
    using Newtonsoft.Json;

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
        /// The azure aad resource for azure management app.
        /// </summary>
        private const string AzureAADResource = "https://management.core.windows.net/";

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
        private readonly ConcurrentDictionary<string, UserAuthenticationDetails> tenantBasedTokenMap = new ConcurrentDictionary<string, UserAuthenticationDetails>();

        /// <summary>
        /// The subscription to tenant map. Stores the list of subscription alongside their tenant ids.
        /// </summary>
        private readonly ConcurrentDictionary<SubscriptionInner, TenantIdDescription> subscriptionToTenantMap = new ConcurrentDictionary<SubscriptionInner, TenantIdDescription>();

        /// <summary>
        /// The subscription to location map.
        /// </summary>
        private readonly ConcurrentDictionary<SubscriptionInner, IEnumerable<Location>> subscriptionToLocationMap = new ConcurrentDictionary<SubscriptionInner, IEnumerable<Location>>();

#if !USEADAL
        /// <summary>
        /// Public client application instance.
        /// </summary>
        private readonly IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder
            .CreateWithApplicationOptions(new PublicClientApplicationOptions
            {
                ClientId = PSClientId,
                AzureCloudInstance = AzureCloudInstance.AzurePublic,
                RedirectUri = "http://localhost",
            })
            .Build();
#endif

        /// <summary>
        /// The user identifier.
        /// </summary>
        private string userIdentifier;

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
                UserAuthenticationDetails authDetails = await this.AcquireAzureManagementTokenAsync("common", usePrompt: true).ConfigureAwait(false);

                this.userIdentifier = authDetails.Username;
                this.tenantBasedTokenMap[this.GetTenantOnToken(authDetails)] = authDetails;
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
                                try
                                {
                                    this.logger.LogInformation("Get token with '{0}' tenant info", tenant.TenantId);
                                    UserAuthenticationDetails tenantizedToken = await this.AcquireAzureManagementTokenAsync(tenant.TenantId, usePrompt: false, this.userIdentifier).ConfigureAwait(false);
                                    this.tenantBasedTokenMap[this.GetTenantOnToken(tenantizedToken)] = tenantizedToken;
                                }
                                catch (Exception ex)
                                {
                                    this.logger.LogWarning(ex, $"Failed to acquire token for tenant with Id '{tenant.TenantId}'");
                                }
                            }
                        }));
                    });

                await Task.WhenAll(tokenAcquireTasks).ConfigureAwait(false);

                // Get all subscriptions for given tenants
                List<Task> subscriptionTasks = new List<Task>();

                // Generating a filtered tenant list to ensure we have tokens before we make the call.
                IEnumerable<TenantIdDescription> filteredTenantList = tenantList.Where((tenant) => this.tenantBasedTokenMap.ContainsKey(tenant.TenantId));

                foreach (TenantIdDescription tenant in filteredTenantList)
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
                }

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
        public string GetSubscriptionSpecificUserToken(SubscriptionInner subscription)
        {
            return this.tenantBasedTokenMap[this.subscriptionToTenantMap[subscription].TenantId].AccessToken;
        }

        /// <summary>
        /// Gets the tenantId on the issued token.
        /// </summary>
        /// <param name="authenticationResult">Authentication result token.</param>
        /// <returns>Tenant on the token.</returns>
        private string GetTenantOnToken(UserAuthenticationDetails authenticationResult)
        {
            try
            {
                string tokenBody = authenticationResult.AccessToken.Split('.')[1];

                // Add padding to base64 string if needed.
                string paddedString = tokenBody;
                for (int i = 0; i < paddedString.Length % 4; i++)
                {
                    paddedString = paddedString + "=";
                }

                string tenantIdOnToken = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(Convert.FromBase64String(paddedString)))["tid"].ToString();

                if (!tenantIdOnToken.Equals(authenticationResult.TenantId, StringComparison.OrdinalIgnoreCase))
                {
                    this.logger.LogWarning(
                        $"TenantId mismatch!! TenantId in response '{authenticationResult.TenantId}'. TenantId on token '{tenantIdOnToken}'");
                }

                return tenantIdOnToken;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Hit exception during tenantId extraction process");
                return authenticationResult.TenantId;
            }
        }

        /// <summary>
        /// Acquires the Azure management token asynchronously.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="usePrompt">The prompt behavior. If true, raises a prompt, otherwise performs silent login.</param>
        /// <param name="userIdentifier">Optional user for which token is to be fetched.</param>
        /// <returns>AAD authentication result.</returns>
        private async Task<UserAuthenticationDetails> AcquireAzureManagementTokenAsync(
            string tenantId,
            bool usePrompt,
            string userIdentifier = null)
        {
            Guid correlationId = Guid.NewGuid();

            this.logger.LogInformation($"Acquiring token with correlation Id set to '{correlationId}'");
            try
            {
#if USEADAL
                AuthenticationContext authenticationContext = new AuthenticationContext(
                    $"{UserAuthenticator.AADLoginAuthority}{tenantId}",
                    validateAuthority: false,
                    TokenCache.DefaultShared,
                    new AzureAdHttpClientFactory())
                {
                    CorrelationId = correlationId,
                };

                AuthenticationResult authenticationResult = null;

                if (userIdentifier == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Uri redirectUri = DefaultOsBrowserWebUi.UpdateRedirectUri(new Uri("http://localhost"));

                        authenticationResult = await authenticationContext.AcquireTokenAsync(
                            UserAuthenticator.AzureAADResource,
                            UserAuthenticator.PSClientId,
                            redirectUri,
                            new PlatformParameters(
                                usePrompt ? PromptBehavior.SelectAccount : PromptBehavior.Never,
                                new DefaultOsBrowserWebUi(this.logger))).ConfigureAwait(false);
                    }
                    else
                    {
                        DeviceCodeResult deviceCodeResult = await authenticationContext.AcquireDeviceCodeAsync(UserAuthenticator.AzureAADResource, UserAuthenticator.PSClientId).ConfigureAwait(false);

                        Console.WriteLine($"Interactive login required. Please enter {deviceCodeResult.UserCode} when asked for in the browser window. If a browser windows does not open or is not supported, follow the instructions below (these can be followed on any device) {Environment.NewLine}{deviceCodeResult.Message}");
                        await Task.Delay(1000).ConfigureAwait(false);

                        authenticationResult = await authenticationContext.AcquireTokenByDeviceCodeAsync(deviceCodeResult).ConfigureAwait(false);
                    }
                }
                else
                {
                    authenticationResult = await authenticationContext.AcquireTokenSilentAsync(
                        UserAuthenticator.AzureAADResource,
                        UserAuthenticator.PSClientId).ConfigureAwait(false);
                }

                return new UserAuthenticationDetails
                {
                    AccessToken = authenticationResult.AccessToken,
                    TenantId = authenticationResult.TenantId,
                    Username = authenticationResult.UserInfo.DisplayableId,
                };
#else
                AuthenticationResult authenticationResult = null;

                if (userIdentifier == null)
                {
                    if (this.publicClientApplication.IsSystemWebViewAvailable)
                    {
                        authenticationResult = await this.publicClientApplication
                            .AcquireTokenInteractive(new List<string>() { AzureAADResource + "/.default" })
                            .WithAuthority($"{UserAuthenticator.AADLoginAuthority}{tenantId}", validateAuthority: false)
                            .WithCorrelationId(correlationId)
                            .WithPrompt(usePrompt ? Prompt.SelectAccount : Prompt.NoPrompt)
                            .ExecuteAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        authenticationResult = await this.publicClientApplication
                            .AcquireTokenWithDeviceCode(
                                new List<string>() { AzureAADResource + "/.default" },
                                async (deviceCodeResult) =>
                                {
                                    Console.WriteLine($"Interactive login required. Please enter {deviceCodeResult.UserCode} when asked for in the browser window. If a browser windows does not open or is not supported, follow the instructions below (these can be followed on any device) {Environment.NewLine}{deviceCodeResult.Message}");
                                    await Task.Delay(1000).ConfigureAwait(false);

                                    try
                                    {
                                        // Open the browser with the url.
                                        ProcessStartInfo processStartInfo = new ProcessStartInfo
                                        {
                                            FileName = deviceCodeResult.VerificationUrl,
                                            UseShellExecute = true,
                                        };

                                        Process.Start(processStartInfo);
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine($"Could not open the url for verification on this machine. Please open {deviceCodeResult.VerificationUrl} from any machine and enter code {deviceCodeResult.UserCode} when asked to do so to continue. The process will continue automatically once the login verification is done.");
                                    }
                                })
                            .WithAuthority($"{UserAuthenticator.AADLoginAuthority}{tenantId}", validateAuthority: false)
                            .WithCorrelationId(correlationId)
                            .ExecuteAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    authenticationResult = await this.publicClientApplication
                        .AcquireTokenSilent(new List<string>() { AzureAADResource + "/.default" }, userIdentifier)
                        .WithAuthority($"{UserAuthenticator.AADLoginAuthority}{tenantId}", validateAuthority: false)
                        .WithCorrelationId(correlationId)
                        .ExecuteAsync().ConfigureAwait(false);
                }

                return new UserAuthenticationDetails
                {
                    AccessToken = authenticationResult.AccessToken,
                    Username = authenticationResult.Account.Username,
                    TenantId = authenticationResult.TenantId,
                };
#endif
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Failed to acquire token. Correlation Id '{correlationId}'");
                throw;
            }
        }

        /// <summary>
        /// User authentication details.
        /// </summary>
        private class UserAuthenticationDetails
        {
            /// <summary>
            /// Gets or sets the access token.
            /// </summary>
            public string AccessToken { get; set; }

            /// <summary>
            /// Gets or sets the username.
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// Gets or sets the tenant ID.
            /// </summary>
            public string TenantId { get; set; }
        }
    }
}

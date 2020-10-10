// <copyright file="AzureAdHttpClientFactory.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
#if USEADAL
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
#endif

    /// <summary>
    /// Azure AD HTTP client factory.
    /// </summary>
#if USEADAL
    public sealed class AzureAdHttpClientFactory : IHttpClientFactory, IDisposable
#else
    public sealed class AzureAdHttpClientFactory : IDisposable
#endif
    {
        private readonly HttpClient httpClient = new HttpClient(new CodeVerifierBypassDelegatingHandler());

        /// <summary>
        /// Gets the HTTP client to be used for ADAL communication.
        /// </summary>
        /// <returns>HTTP Client to use for communication to AAD.</returns>
#pragma warning disable CA1024 // Use properties where appropriate - Implementing the interface.
        public HttpClient GetHttpClient()
#pragma warning restore CA1024 // Use properties where appropriate - Implementing the interface.
        {
            return this.httpClient;
        }

        /// <summary>
        /// Cleans up underlying resources.
        /// </summary>
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        /// <summary>
        /// Delegating handler which adds the PKeyAuth bypass headers.
        /// </summary>
        private class CodeVerifierBypassDelegatingHandler : DelegatingHandler
        {
            private const string WWWAuthenticateHeader = "WWW-Authenticate";

            private const string PKeyAuthScheme = "PKeyAuth";

            public CodeVerifierBypassDelegatingHandler()
            {
                this.InnerHandler = new HttpClientHandler();
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage httpResponseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized &&
                    httpResponseMessage.Headers.Contains(CodeVerifierBypassDelegatingHandler.WWWAuthenticateHeader) &&
                    httpResponseMessage.Headers.GetValues(CodeVerifierBypassDelegatingHandler.WWWAuthenticateHeader).Single().StartsWith(CodeVerifierBypassDelegatingHandler.PKeyAuthScheme, StringComparison.OrdinalIgnoreCase))
                {
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(request.Method, request.RequestUri)
                    {
                        Content = request.Content,
                    };

                    foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
                    {
                        httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    string pkeyHeader = httpResponseMessage.Headers.GetValues(CodeVerifierBypassDelegatingHandler.WWWAuthenticateHeader).SingleOrDefault();

                    List<string> pkeyParts = pkeyHeader.Split(',').ToList();

                    pkeyParts.RemoveAll(parts => !parts.Trim().StartsWith("Version", StringComparison.OrdinalIgnoreCase) && !parts.Trim().StartsWith("Context", StringComparison.OrdinalIgnoreCase));

                    // Add the bypass header. Part of this is picked up from how MSAL does things.
                    httpRequestMessage.Headers.TryAddWithoutValidation(
                        "Authorization",
                        CodeVerifierBypassDelegatingHandler.PKeyAuthScheme + string.Join(',', pkeyParts));

                    httpResponseMessage = await base.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
                }

                return httpResponseMessage;
            }
        }
    }
}

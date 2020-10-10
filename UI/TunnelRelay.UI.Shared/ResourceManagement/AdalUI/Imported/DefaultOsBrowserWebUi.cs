// <copyright file="DefaultOsBrowserWebUi.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client.Platforms.Shared.DefaultOSBrowser;
    using Microsoft.Identity.Client.Platforms.Shared.NetStdCore;
    using Microsoft.IdentityModel.Clients.ActiveDirectory.Extensibility;

    /// <summary>
    /// Default OS browser WebUI. Copied in parts from MSAL SDK.
    /// </summary>
    internal class DefaultOsBrowserWebUi : ICustomWebUi
    {
        /// <summary>
        /// Default success HTML.
        /// </summary>
        internal const string DefaultSuccessHtml = @"<html>
  <head><title>Authentication Complete</title></head>
  <body>
    Authentication complete. You can return to the application. Feel free to close this browser tab.
  </body>
</html>";

        /// <summary>
        /// Default failure HTML.
        /// </summary>
        internal const string DefaultFailureHtml = @"<html>
  <head><title>Authentication Failed</title></head>
  <body>
    Authentication failed. You can return to the application. Feel free to close this browser tab.
</br></br></br></br>
    Error details: error {0} error_description: {1}
  </body>
</html>";

        private readonly IUriInterceptor uriInterceptor;
        private readonly SystemWebViewOptions webViewOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultOsBrowserWebUi"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public DefaultOsBrowserWebUi(
            ILogger logger)
        {
            this.webViewOptions = new SystemWebViewOptions();

            this.uriInterceptor = new HttpListenerInterceptor(logger);
        }

        /// <summary>
        /// Updates the redirect uri to one on which we can listen.
        /// </summary>
        /// <param name="redirectUri">Redirect uri.</param>
        /// <returns>Updated redirect uri.</returns>
        public static Uri UpdateRedirectUri(Uri redirectUri)
        {
            return FindFreeLocalhostRedirectUri(redirectUri);
        }

        /// <summary>
        /// Acquires the authorization code from authorization url.
        /// </summary>
        /// <param name="authorizationUri">Authorization uri.</param>
        /// <param name="redirectUri">Redirect uri to use.</param>
        /// <returns>Authorization code.</returns>
        public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri)
        {
            return await this.InterceptAuthorizationUriAsync(
                    authorizationUri,
                    redirectUri,
                    CancellationToken.None)
                    .ConfigureAwait(true);
        }

        /// <summary>
        /// Finds a free localhost port and updates the uri based on it.
        /// </summary>
        /// <param name="redirectUri">Redirect uri.</param>
        /// <returns>Updated uri.</returns>
        private static Uri FindFreeLocalhostRedirectUri(Uri redirectUri)
        {
            if (redirectUri.Port > 0 && redirectUri.Port != 80)
            {
                return redirectUri;
            }

            TcpListener listner = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listner.Start();
                int port = ((IPEndPoint)listner.LocalEndpoint).Port;
                return new Uri("http://localhost:" + port);
            }
            finally
            {
                listner?.Stop();
            }
        }

        /// <summary>
        /// Intercepts the authorization uri response and gets the authorization code.
        /// </summary>
        /// <param name="authorizationUri">Authorization uri.</param>
        /// <param name="redirectUri">Redirect uri.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authorization code.</returns>
        private async Task<Uri> InterceptAuthorizationUriAsync(
            Uri authorizationUri,
            Uri redirectUri,
            CancellationToken cancellationToken)
        {
            Func<Uri, Task> defaultBrowserAction = (Uri u) =>
            {
                PlatformProxyShared.StartDefaultOsBrowser(u.AbsoluteUri);
                return Task.CompletedTask;
            };

            Func<Uri, Task> openBrowserAction = defaultBrowserAction;

            cancellationToken.ThrowIfCancellationRequested();
            await openBrowserAction(authorizationUri).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return await this.uriInterceptor.ListenToSingleRequestAndRespondAsync(
                redirectUri.Port,
                this.GetResponseMessage,
                cancellationToken)
            .ConfigureAwait(false);
        }

        private MessageAndHttpCode GetResponseMessage(Uri authCodeUri)
        {
            return this.GetMessage(
                this.webViewOptions?.BrowserRedirectSuccess,
                this.webViewOptions?.HtmlMessageSuccess ?? DefaultSuccessHtml);
        }

        private MessageAndHttpCode GetMessage(Uri redirectUri, string message)
        {
            if (redirectUri != null)
            {
                return new MessageAndHttpCode(HttpStatusCode.Found, redirectUri.ToString());
            }

            return new MessageAndHttpCode(HttpStatusCode.OK, message);
        }
    }
}

// <copyright file="HybridConnectionManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Relay;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Hybrid Connection manager. Manages hybrid connection between Azure and current machine.
    /// </summary>
    public class HybridConnectionManager : IHybridConnectionManager
    {
        private const string ConnectionStringFormat = "Endpoint=sb://{0}/;SharedAccessKeyName={1};SharedAccessKey={2};EntityPath={3}";

        private readonly HybridConnectionListener hybridConnectionListener;

        private readonly IRelayRequestManager relayManager;

        private readonly string relayUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="HybridConnectionManager"/> class.
        /// </summary>
        /// <param name="hybridConnectionManagerOptions">Hybrid connection manager settings.</param>
        /// <param name="relayManager"><see cref="IRelayRequestManager"/> instance to manage requests received over the hybrid connection.</param>
        public HybridConnectionManager(
            IOptions<HybridConnectionManagerOptions> hybridConnectionManagerOptions,
            IRelayRequestManager relayManager)
        {
            if (hybridConnectionManagerOptions?.Value == null)
            {
                throw new ArgumentNullException(nameof(hybridConnectionManagerOptions));
            }

            if (string.IsNullOrEmpty(hybridConnectionManagerOptions.Value.ServiceBusUrlHost))
            {
                throw new ArgumentNullException(nameof(hybridConnectionManagerOptions.Value.ServiceBusUrlHost));
            }

            // Remove the / at the end.
            hybridConnectionManagerOptions.Value.ServiceBusUrlHost = hybridConnectionManagerOptions.Value.ServiceBusUrlHost.TrimEnd('/');
            hybridConnectionManagerOptions.Value.ServiceBusUrlHost = hybridConnectionManagerOptions.Value.ServiceBusUrlHost.Replace("https://", string.Empty);

            if (string.IsNullOrEmpty(hybridConnectionManagerOptions.Value.ConnectionPath))
            {
                throw new ArgumentNullException(nameof(hybridConnectionManagerOptions.Value.ConnectionPath));
            }

            if (string.IsNullOrEmpty(hybridConnectionManagerOptions.Value.ServiceBusKeyName))
            {
                throw new ArgumentNullException(nameof(hybridConnectionManagerOptions.Value.ServiceBusKeyName));
            }

            if (string.IsNullOrEmpty(hybridConnectionManagerOptions.Value.ServiceBusSharedKey))
            {
                throw new ArgumentNullException(nameof(hybridConnectionManagerOptions.Value.ServiceBusSharedKey));
            }

            string connectionString = string.Format(
                ConnectionStringFormat,
                hybridConnectionManagerOptions.Value.ServiceBusUrlHost,
                hybridConnectionManagerOptions.Value.ServiceBusKeyName,
                hybridConnectionManagerOptions.Value.ServiceBusSharedKey,
                hybridConnectionManagerOptions.Value.ConnectionPath);
            this.hybridConnectionListener = new HybridConnectionListener(connectionString);

            // Figure out the prefix.
            this.relayUrl = $"sb://{hybridConnectionManagerOptions.Value.ServiceBusUrlHost}/{hybridConnectionManagerOptions.Value.ConnectionPath}".ToUpperInvariant();
            this.relayManager = relayManager;
        }

        /// <summary>
        /// Opens the hybrid connection between Azure and current machine asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task tracking operation.</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.hybridConnectionListener.RequestHandler = (context) =>
            {
                Task.Run(() => this.HandleProxyRequestAsync(context));
            };

            await this.hybridConnectionListener.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Closes the Hybrid connection asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task tracking operation.</returns>
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return this.hybridConnectionListener.CloseAsync(cancellationToken);
        }

        /// <summary>
        /// Handles the HTTP request received over the hybrid connection.
        /// </summary>
        /// <param name="context">Relayed request context.</param>
        /// <returns>Task tracking operation.</returns>
        private async Task HandleProxyRequestAsync(RelayedHttpListenerContext context)
        {
            RelayRequest relayRequest = new RelayRequest
            {
                Headers = context.Request.Headers,
                HttpMethod = new HttpMethod(context.Request.HttpMethod),
                InputStream = context.Request.InputStream,
                RequestPathAndQuery = context.Request.Url.AbsoluteUri.ToUpperInvariant().Replace(this.relayUrl, string.Empty),
                RequestStartDateTime = DateTimeOffset.Now,
            };

            try
            {
                RelayResponse relayResponse = await this.relayManager.HandleRelayRequestAsync(relayRequest).ConfigureAwait(false);

                context.Response.StatusCode = relayResponse.HttpStatusCode;
                context.Response.StatusDescription = relayResponse.StatusDescription;

                // Copy over outgoing headers.
                foreach (string headerName in relayResponse.Headers.Keys)
                {
                    // Not copying over the Transfer-Encoding header as the communication between server and Relay, and Relay and client are
                    // different connections.
                    if (headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    context.Response.Headers[headerName] = relayResponse.Headers[headerName];
                }

                if (relayResponse.OutputStream != null)
                {
                    await relayResponse.OutputStream.CopyToAsync(context.Response.OutputStream).ConfigureAwait(false);
                }

                await context.Response.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = "Exception occurred at Server";

                byte[] errorMessage = Encoding.UTF8.GetBytes(ex.Message);
                await context.Response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length).ConfigureAwait(false);
            }
        }
    }
}

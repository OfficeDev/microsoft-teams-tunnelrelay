// <copyright file="RelayRequestManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Handles the requests received over the relay.
    /// </summary>
    public sealed class RelayRequestManager : IRelayRequestManager, IDisposable
    {
        private readonly IEnumerable<ITunnelRelayPlugin> tunnelRelayPlugins;

        private readonly IRelayRequestEventListener relayRequestEventListener;

        private readonly HttpClient httpClient = new HttpClient();

        private string internalServiceUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayRequestManager"/> class.
        /// </summary>
        /// <param name="relayRequestManagerOptions">Relay request manager options.</param>
        /// <param name="tunnelRelayPlugins">Instances of the plugins to use.</param>
        /// <param name="relayRequestEventListener">Optional relay request event listener instance.</param>
        public RelayRequestManager(
            IOptionsMonitor<RelayRequestManagerOptions> relayRequestManagerOptions,
            IEnumerable<ITunnelRelayPlugin> tunnelRelayPlugins,
            IRelayRequestEventListener relayRequestEventListener = null)
        {
            this.tunnelRelayPlugins = tunnelRelayPlugins;
            this.relayRequestEventListener = relayRequestEventListener;

            this.UpdateSettings(relayRequestManagerOptions?.CurrentValue);

            relayRequestManagerOptions.OnChange((newOptions) =>
            {
                this.UpdateSettings(newOptions);
            });
        }

        /// <summary>
        /// Handles the incoming request, passes it down to the internal service and returns the response.
        /// </summary>
        /// <param name="relayRequest">Incoming relay request.</param>
        /// <returns>Response from the internal service.</returns>
        public async Task<RelayResponse> HandleRelayRequestAsync(RelayRequest relayRequest)
        {
            string requestId = Guid.NewGuid().ToString();

            // Inform listener that a request has been received.
            if (this.relayRequestEventListener != null)
            {
#pragma warning disable CS4014 // We want eventing pipeline to run in parallel and not block the actual request execution.
                Task.Run(async () =>
                {
                    try
                    {
                        await this.relayRequestEventListener.RequestReceivedAsync(requestId, relayRequest.Clone() as RelayRequest).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Ignoring exceptions from listeners.
                    }
                });
#pragma warning restore CS4014 // We want eventing pipeline to run in parallel and not block the actual request execution.
            }

            HttpRequestMessage httpRequestMessage = await this.ToHttpRequestMessageAsync(relayRequest).ConfigureAwait(false);

            // Pass the request through all the plugins.
            foreach (ITunnelRelayPlugin tunnelRelayPlugin in this.tunnelRelayPlugins)
            {
                httpRequestMessage = await tunnelRelayPlugin.PreProcessRequestToServiceAsync(httpRequestMessage).ConfigureAwait(false);
            }

            HttpResponseMessage httpResponseMessage;

            try
            {
                httpResponseMessage = await this.httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
            }
            catch (HttpRequestException httpException)
            {
                httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadGateway);
                httpResponseMessage.Content = new StringContent(httpException.ToString());
            }

            // Pass the response through all the plugins.
            foreach (ITunnelRelayPlugin tunnelRelayPlugin in this.tunnelRelayPlugins)
            {
                httpResponseMessage = await tunnelRelayPlugin.PostProcessResponseFromServiceAsync(httpResponseMessage).ConfigureAwait(false);
            }

            RelayResponse relayResponse = await this.ToRelayResponseAsync(httpResponseMessage).ConfigureAwait(false);

            if (this.relayRequestEventListener != null)
            {
#pragma warning disable CS4014 // We want eventing pipeline to run in parallel and not slowdown the actual request execution.
                Task.Run(async () =>
                {
                    try
                    {
                        await this.relayRequestEventListener.ResponseSentAsync(requestId, relayResponse.Clone() as RelayResponse).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Ignoring exceptions from listeners.
                    }
                });
#pragma warning restore CS4014 // We want eventing pipeline to run in parallel and not slowdown the actual request execution.
            }

            return relayResponse;
        }

        /// <summary>
        /// Dispose the instance.
        /// </summary>
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        /// <summary>
        /// Copies content headers.
        /// </summary>
        /// <param name="httpContent">Http content.</param>
        /// <param name="headerCollection">Header collection.</param>
        private static void CopyContentHeader(HttpContent httpContent, WebHeaderCollection headerCollection)
        {
            if (headerCollection["Content-Disposition"] != null)
            {
                httpContent.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(headerCollection["Content-Disposition"]);
            }

            if (headerCollection[HttpRequestHeader.ContentLocation] != null)
            {
                httpContent.Headers.ContentLocation = new Uri(headerCollection[HttpRequestHeader.ContentLocation]);
            }

            if (headerCollection[HttpRequestHeader.ContentRange] != null)
            {
                httpContent.Headers.ContentRange = ContentRangeHeaderValue.Parse(headerCollection[HttpRequestHeader.ContentRange]);
            }

            if (headerCollection[HttpRequestHeader.ContentType] != null)
            {
                httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse(headerCollection[HttpRequestHeader.ContentType]);
            }

            if (headerCollection[HttpRequestHeader.Expires] != null)
            {
                httpContent.Headers.Expires = DateTimeOffset.Parse(headerCollection[HttpRequestHeader.Expires]);
            }

            if (headerCollection[HttpRequestHeader.LastModified] != null)
            {
                httpContent.Headers.LastModified = DateTimeOffset.Parse(headerCollection[HttpRequestHeader.LastModified]);
            }

            if (headerCollection[HttpRequestHeader.ContentLength] != null)
            {
                httpContent.Headers.ContentLength = long.Parse(headerCollection[HttpRequestHeader.ContentLength]);
            }
        }

        /// <summary>
        /// Converts <see cref="RelayRequest"/> to <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="relayRequest">Incoming relay request.</param>
        /// <returns>Http request message.</returns>
        private async Task<HttpRequestMessage> ToHttpRequestMessageAsync(RelayRequest relayRequest)
        {
            Uri internalRequestUrl = new Uri(this.internalServiceUrl + "/" + relayRequest.RequestPathAndQuery.TrimStart('/'));

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(relayRequest.HttpMethod, internalRequestUrl);

            // Prepare request content.
            if (relayRequest.InputStream != null && relayRequest.HttpMethod != HttpMethod.Get)
            {
                MemoryStream memoryStream = new MemoryStream((int)relayRequest.InputStream.Length);
                await relayRequest.InputStream.CopyToAsync(memoryStream).ConfigureAwait(false);
                httpRequestMessage.Content = new StreamContent(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                httpRequestMessage.Content.Headers.ContentLength = memoryStream.Length;
                RelayRequestManager.CopyContentHeader(httpRequestMessage.Content, relayRequest.Headers);
            }

            // Try to blindly add the headers. Content headers will get filtered out here.
            foreach (string headerName in relayRequest.Headers.Keys)
            {
                httpRequestMessage.Headers.TryAddWithoutValidation(headerName, relayRequest.Headers[headerName]);
            }

            // Set the correct host header.
            httpRequestMessage.Headers.Host = internalRequestUrl.Host;

            return httpRequestMessage;
        }

        /// <summary>
        /// Converts <see cref="HttpResponseMessage"/> to <see cref="RelayResponse"/>.
        /// </summary>
        /// <param name="httpResponseMessage">Response message.</param>
        /// <returns>Relay response.</returns>
        private async Task<RelayResponse> ToRelayResponseAsync(HttpResponseMessage httpResponseMessage)
        {
            RelayResponse relayResponse = new RelayResponse
            {
                HttpStatusCode = httpResponseMessage.StatusCode,
                StatusDescription = httpResponseMessage.ReasonPhrase,
                Headers = new WebHeaderCollection(),
                RequestEndDateTime = DateTimeOffset.Now,
            };

            // Copy the response headers.
            foreach (KeyValuePair<string, IEnumerable<string>> responseHeader in httpResponseMessage.Headers)
            {
                relayResponse.Headers.Add(responseHeader.Key, string.Join(",", responseHeader.Value));
            }

            if (httpResponseMessage.Content != null)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> responseHeader in httpResponseMessage.Content.Headers)
                {
                    relayResponse.Headers.Add(responseHeader.Key, string.Join(",", responseHeader.Value));
                }

                relayResponse.OutputStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }

            return relayResponse;
        }

        /// <summary>
        /// Updates the settings for current instance.
        /// </summary>
        /// <param name="relayRequestManagerOptions">Relay request manager options.</param>
        private void UpdateSettings(RelayRequestManagerOptions relayRequestManagerOptions)
        {
            if (relayRequestManagerOptions == null)
            {
                throw new ArgumentNullException(nameof(relayRequestManagerOptions));
            }

            if (relayRequestManagerOptions.InternalServiceUrl == null)
            {
                throw new ArgumentNullException(nameof(relayRequestManagerOptions.InternalServiceUrl));
            }

            this.internalServiceUrl = relayRequestManagerOptions.InternalServiceUrl.AbsoluteUri.TrimEnd('/');
        }
    }
}

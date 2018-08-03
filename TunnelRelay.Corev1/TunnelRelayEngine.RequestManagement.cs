// <copyright file="TunnelRelayEngine.RequestManagement.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
// Licensed under the MIT license.
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace TunnelRelay.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Manages processing of incoming requests.
    /// </summary>
    public partial class TunnelRelayEngine
    {
        /// <summary>
        /// The HTTP client.
        /// </summary>
        private static HttpClient httpClient = new HttpClient(new HttpClientHandler() { UseCookies = false });

        /// <summary>
        /// Request event handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RequestEventArgs"/> instance containing the event data.</param>
        public delegate void RequestEventHandler(object sender, RequestEventArgs e);

        /// <summary>
        /// Occurs when request is received.
        /// </summary>
        public static event RequestEventHandler RequestReceived;

        /// <summary>
        /// Occurs when request is updated.
        /// </summary>
        public static event RequestEventHandler RequestUpdated;

        /// <summary>
        /// Gets the required response from underlying service.
        /// </summary>
        /// <param name="operationContext">Current operation context.</param>
        /// <param name="stream">The stream to get body of incoming request..</param>
        /// <returns>Response from underlying service.</returns>
        internal static async Task<Message> GetResponse(WebOperationContext operationContext, Stream stream = null)
        {
            // Buffer the request contents into memory.
            MemoryStream memoryStream = null;

            if (stream != null)
            {
                memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
            }

            var activeRequestContext = new RequestContext
            {
                IncomingRequestStream = memoryStream,
                IncomingWebRequest = operationContext.IncomingRequest,
            };

            activeRequestContext.UIRequestDescription = new RequestDetails
            {
                Method = activeRequestContext.IncomingWebRequest.Method,
                Url = activeRequestContext.IncomingWebRequest.UriTemplateMatch.RequestUri.PathAndQuery.Replace(activeRequestContext.IncomingWebRequest.UriTemplateMatch.RequestUri.Segments[1], string.Empty),
                RequestHeaders = new List<HeaderDetails>(activeRequestContext.IncomingRequestHeaders.GetUIHeaderMap()),
                Timestamp = DateTime.Now.ToString("O"),
                RequestReceiveTime = DateTime.Now,
                RequestData = activeRequestContext.IncomingRequestStream != null ? await activeRequestContext.IncomingRequestStream.ReadToEndAsync() : string.Empty,
                ResponseData = string.Empty,
                ResponseHeaders = new List<HeaderDetails>(),
                StatusCode = "Active",
                Duration = "Active",
            };

            try
            {
                // Fire event for request received.
                TunnelRelayEngine.RequestReceived?.Invoke(ApplicationData.Instance.ProxyBaseUrl, new RequestEventArgs
                {
                    Request = activeRequestContext.UIRequestDescription,
                });

                // Get new Request.
                HttpRequestMessage requestMessage = TunnelRelayEngine.GetInternalRequest(ref activeRequestContext);

                // Execute the request + call plugins.
                DateTime hostedServiceRequestStartTime = DateTime.Now;
                HttpResponseMessage response = await TunnelRelayEngine.ExecuteInternalRequestAsync(requestMessage);
                activeRequestContext.UIRequestDescription.Duration = (DateTime.Now - hostedServiceRequestStartTime).TotalMilliseconds + "ms";

                // Copy data from response and populate UI elements.
                Stream readableStream = null;
                if (response.Content != null)
                {
                    readableStream = await response.Content.ReadAsStreamAsync();
                    activeRequestContext.UIRequestDescription.ResponseData = await new StreamReader(readableStream).ReadToEndAsync();
                    readableStream.Seek(0, SeekOrigin.Begin);
                }
                else
                {
                    activeRequestContext.UIRequestDescription.ResponseData = string.Empty;
                }

                response.Headers.GetUIHeaderMap().ForEach(header => activeRequestContext.UIRequestDescription.ResponseHeaders.Add(header));

                activeRequestContext.UIRequestDescription.StatusCode = response.StatusCode.ToString();

                // Get the Serializable response to be sent back over the wire. This response needs to be serializable by WCF standards.
                Message responseMessage = TunnelRelayEngine.GetOutgoingResponse(operationContext, response, readableStream);

                return responseMessage;
            }
            catch (Exception ex)
            {
                activeRequestContext.UIRequestDescription.StatusCode = "Exception!!";
                activeRequestContext.UIRequestDescription.ExceptionHit = true;
                activeRequestContext.UIRequestDescription.ResponseData = JsonConvert.SerializeObject(ex, Formatting.Indented);
                activeRequestContext.UIRequestDescription.Duration = (DateTime.Now - activeRequestContext.UIRequestDescription.RequestReceiveTime).TotalMilliseconds.ToString() + "ms";
                Message exceptionMessage = WebOperationContext.Current.CreateTextResponse(ex.ToString());
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                return exceptionMessage;
            }
            finally
            {
                TunnelRelayEngine.RequestUpdated?.Invoke(ApplicationData.Instance.ProxyBaseUrl, new RequestEventArgs
                {
                    Request = activeRequestContext.UIRequestDescription,
                });
            }
        }

        /// <summary>
        /// Gets the internal request to be made to hosted service.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>Request to be made to the hosted service.</returns>
        /// <exception cref="NotSupportedException">HTTP Method is not supported</exception>
        private static HttpRequestMessage GetInternalRequest(ref RequestContext requestContext)
        {
            // Url Creation
            // Url comes as https://servicebusnamespace.servicebus.windows.net/MachineName/ActualPath
            // activeRequestContext.IncomingWebRequest.UriTemplateMatch.RequestUri.PathAndQuery gives us MachineName/ActualPath
            // activeRequestContext.IncomingWebRequest.UriTemplateMatch.RequestUri.Segment[0] is / and Segment[1] is MachineName+/ we replace segment[1] with empty string.
            // add the local redirection e.g. https://localhost to it. Thus making https://localhost/ActualPath
            string newUrl = ApplicationData.Instance.RedirectionUrl.TrimEnd('/') +
                requestContext.IncomingWebRequest.UriTemplateMatch.RequestUri.PathAndQuery.Replace(requestContext.IncomingWebRequest.UriTemplateMatch.RequestUri.Segments[1], string.Empty);

            HttpMethod httpMethod;

            switch (requestContext.IncomingWebRequest.Method.ToUpper())
            {
                case "POST":
                    {
                        httpMethod = HttpMethod.Post;
                        break;
                    }

                case "GET":
                    {
                        httpMethod = HttpMethod.Get;
                        break;
                    }

                case "OPTIONS":
                    {
                        httpMethod = HttpMethod.Options;
                        break;
                    }

                case "PUT":
                    {
                        httpMethod = HttpMethod.Put;
                        break;
                    }

                case "DELETE":
                    {
                        httpMethod = HttpMethod.Delete;
                        break;
                    }

                default:
                    {
                        throw new NotSupportedException("HTTP Method is not supported");
                    }
            }

            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, newUrl);
            requestMessage.CopyRequestHeaders(requestContext.IncomingRequestHeaders);

            // Change the host header to the new Authority.
            requestMessage.Headers.Host = new Uri(newUrl).Authority;

            if (requestContext.IncomingRequestStream != null)
            {
                requestContext.IncomingRequestStream.Seek(0, SeekOrigin.Begin);

                requestMessage.Content = new StreamContent(requestContext.IncomingRequestStream);
                requestMessage.CopyContentHeaders(requestContext.IncomingRequestHeaders);
            }

            return requestMessage;
        }

        /// <summary>
        /// Executes the internal request asynchronously.
        /// </summary>
        /// <param name="hostedServiceRequestMessage">The hosted service request message.</param>
        /// <returns>Http response message to be sent back to caller.</returns>
        private static async Task<HttpResponseMessage> ExecuteInternalRequestAsync(HttpRequestMessage hostedServiceRequestMessage)
        {
            foreach (PluginDetails plugin in Plugins)
            {
                if (plugin.IsEnabled)
                {
                    hostedServiceRequestMessage = await plugin.PluginInstance.PreProcessRequestToServiceAsync(hostedServiceRequestMessage);
                }
            }

            HttpResponseMessage response = await httpClient.SendAsync(hostedServiceRequestMessage, HttpCompletionOption.ResponseContentRead);

            foreach (PluginDetails plugin in Plugins)
            {
                if (plugin.IsEnabled)
                {
                    response = await plugin.PluginInstance.PostProcessResponseFromServiceAsync(response);
                }
            }

            return response;
        }

        /// <summary>
        /// Gets the outgoing response.
        /// </summary>
        /// <param name="operationContext">The operation context.</param>
        /// <param name="response">The response.</param>
        /// <param name="responseStream">The response stream.</param>
        /// <returns>Outgoing response.</returns>
        private static Message GetOutgoingResponse(WebOperationContext operationContext, HttpResponseMessage response, Stream responseStream)
        {
            Message responseMessage;

            if (response.Content == null)
            {
                responseMessage = operationContext.CreateTextResponse(string.Empty);
            }
            else
            {
                responseMessage = response.Content.Headers.ContentType == null ?
                    operationContext.CreateStreamResponse(responseStream, "text/plain; charset=us-ascii") :
                    operationContext.CreateStreamResponse(responseStream, response.Content.Headers.ContentType.ToString());
            }

            operationContext.OutgoingResponse.StatusCode = response.StatusCode;
            foreach (var header in response.Headers)
            {
                header.Value.ToList().ForEach(headerVal =>
                    operationContext.OutgoingResponse.Headers.Add(header.Key, headerVal));
            }

            foreach (var header in response.Content.Headers)
            {
                header.Value.ToList().ForEach(headerVal =>
                    operationContext.OutgoingResponse.Headers.Add(header.Key, headerVal));
            }

            return responseMessage;
        }
    }
}

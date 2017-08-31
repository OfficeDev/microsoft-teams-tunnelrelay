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
        private static HttpClient httpClient = new HttpClient();

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
            Stopwatch stopWatch = Stopwatch.StartNew();
            var incomingReq = operationContext.IncomingRequest;
            Dictionary<string, string> headerMap = incomingReq.Headers.GetHeadersMap();

            var requestDetails = new RequestDetails()
            {
                Method = incomingReq.Method,
                Url = incomingReq.UriTemplateMatch.RequestUri.PathAndQuery.Replace(incomingReq.UriTemplateMatch.RequestUri.Segments[1], string.Empty),
                RequestHeaders = new List<HeaderDetails>(headerMap.GetUIHeaderMap()),
                Timestamp = DateTime.Now.ToString("O"),
                RequestReceiveTime = DateTime.Now,
                RequestData = string.Empty,
                ResponseData = string.Empty,
                ResponseHeaders = new List<HeaderDetails>(),
                StatusCode = "Active",
                Duration = "Active",
            };

            try
            {
                TunnelRelayEngine.RequestReceived?.Invoke(ApplicationData.Instance.ProxyBaseUrl, new RequestEventArgs
                {
                    Request = requestDetails,
                });

                // Url Creation
                // Url comes as https://servicebusnamespace.servicebus.windows.net/MachineName/ActualPath
                // incomingReq.UriTemplateMatch.RequestUri.PathAndQuery gives us MachineName/ActualPath
                // incomingReq.UriTemplateMatch.RequestUri.Segment[0] is / and Segment[1] is MachineName+/ we replace segment[1] with empty string.
                // add the local redirection e.g. https://localhost to it. Thus making https://localhost/ActualPath
                string newUrl = ApplicationData.Instance.RedirectionUrl.TrimEnd('/') + incomingReq.UriTemplateMatch.RequestUri.PathAndQuery.Replace(incomingReq.UriTemplateMatch.RequestUri.Segments[1], string.Empty);

                HttpMethod httpMethod;

                if (incomingReq.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethod = HttpMethod.Post;
                }
                else if (incomingReq.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethod = HttpMethod.Get;
                }
                else if (incomingReq.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethod = HttpMethod.Options;
                }
                else if (incomingReq.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethod = HttpMethod.Put;
                }
                else if (incomingReq.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    httpMethod = HttpMethod.Delete;
                }
                else
                {
                    throw new NotSupportedException("TunnelRelay does not support this HTTP method at this time.");
                }

                HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, newUrl);
                requestMessage.CopyRequestHeaders(incomingReq);
                requestMessage.Headers.Host = new Uri(newUrl).Authority;

                if (stream != null)
                {
                    StreamReader stringReader = new StreamReader(stream);
                    string data = stringReader.ReadToEnd();
                    requestDetails.RequestData = data;
                    stringReader.Close();

                    requestMessage.Content = new StringContent(data);
                    requestMessage.CopyContentHeaders(headerMap);
                }

                TunnelRelayEngine.RequestUpdated?.Invoke(ApplicationData.Instance.ProxyBaseUrl, new RequestEventArgs
                {
                    Request = requestDetails,
                });

                foreach (PluginDetails plugin in Plugins)
                {
                    if (plugin.IsEnabled)
                    {
                        requestMessage = await plugin.PluginInstance.PreProcessRequestToServiceAsync(requestMessage);
                    }
                }

                HttpResponseMessage response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);

                foreach (PluginDetails plugin in Plugins)
                {
                    if (plugin.IsEnabled)
                    {
                        response = await plugin.PluginInstance.PostProcessResponseFromServiceAsync(response);
                    }
                }

                Stream readableStream = null;
                if (response.Content != null)
                {
                    readableStream = await response.Content.ReadAsStreamAsync();
                    requestDetails.ResponseData = await new StreamReader(readableStream).ReadToEndAsync();
                    readableStream.Seek(0, SeekOrigin.Begin);
                }
                else
                {
                    requestDetails.ResponseData = string.Empty;
                }

                response.Headers.GetUIHeaderMap().ForEach(header => requestDetails.ResponseHeaders.Add(header));

                requestDetails.StatusCode = response.StatusCode.ToString();
                stopWatch.Start();
                requestDetails.Duration = stopWatch.ElapsedMilliseconds.ToString() + "ms";

                Message responseMessage;

                if (response.Content == null)
                {
                    responseMessage = operationContext.CreateTextResponse(string.Empty);
                }
                else
                {
                    responseMessage = response.Content.Headers.ContentType == null ?
                        operationContext.CreateStreamResponse(readableStream, "text/plain; charset=us-ascii") :
                        operationContext.CreateStreamResponse(readableStream, response.Content.Headers.ContentType.ToString());
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
            catch (Exception ex)
            {
                requestDetails.StatusCode = "Exception!!";
                requestDetails.ExceptionHit = true;
                requestDetails.ResponseData = JsonConvert.SerializeObject(ex, Formatting.Indented);
                requestDetails.Duration = (DateTime.Now - requestDetails.RequestReceiveTime).TotalMilliseconds.ToString() + "ms";
                Message exceptionMessage = WebOperationContext.Current.CreateTextResponse(ex.ToString());
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                return exceptionMessage;
            }
            finally
            {
                TunnelRelayEngine.RequestUpdated?.Invoke(ApplicationData.Instance.ProxyBaseUrl, new RequestEventArgs
                {
                    Request = requestDetails,
                });
            }
        }
    }
}

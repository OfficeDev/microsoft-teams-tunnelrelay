// <copyright file="Helpers.cs" company="Microsoft">
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

namespace TunnelRelay
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel.Web;

    /// <summary>
    /// Helper routines.
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Gets the headers for UI to show.
        /// </summary>
        /// <param name="headerCollection">The header collection.</param>
        /// <returns>List of headers for UI to show.</returns>
        public static List<HeaderDetails> GetUIHeaderMap(this HttpResponseHeaders headerCollection)
        {
            var headerCol = new List<HeaderDetails>();
            headerCollection.ToList().ForEach(header =>
                headerCol.Add(new HeaderDetails
                {
                    HeaderName = header.Key,
                    HeaderValue = header.Value.First(),
                }));

            return headerCol;
        }

        /// <summary>
        /// Gets the headers for UI to show.
        /// </summary>
        /// <param name="headerCollection">The header collection.</param>
        /// <returns>List of headers for UI to show.</returns>
        public static List<HeaderDetails> GetUIHeaderMap(this Dictionary<string, string> headerCollection)
        {
            var headerCol = new List<HeaderDetails>();
            headerCollection.ToList().ForEach(header =>
                headerCol.Add(new HeaderDetails
                {
                    HeaderName = header.Key,
                    HeaderValue = header.Value,
                }));

            return headerCol;
        }

        /// <summary>
        /// Gets the headers map.
        /// </summary>
        /// <param name="headerCollection">The header collection.</param>
        /// <returns>Header map.</returns>
        public static Dictionary<string, string> GetHeadersMap(this WebHeaderCollection headerCollection)
        {
            Dictionary<string, string> headerMap = new Dictionary<string, string>();
            headerCollection.AllKeys.ToList().ForEach(header =>
                headerMap.Add(header, headerCollection[header]));
            return headerMap;
        }

        /// <summary>
        /// Copies the valid headers.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="incomingRequest">The incoming request.</param>
        public static void CopyRequestHeaders(this HttpRequestMessage request, IncomingWebRequestContext incomingRequest)
        {
            foreach (string header in incomingRequest.Headers.AllKeys)
            {
                request.Headers.TryAddWithoutValidation(header, incomingRequest.Headers[header]);
            }
        }

        /// <summary>
        /// Copies content headers.
        /// </summary>
        /// <param name="httpRequest">Outgoing request.</param>
        /// <param name="headerMap">Incoming request header map.</param>
        public static void CopyContentHeaders(this HttpRequestMessage httpRequest, Dictionary<string, string> headerMap)
        {
            httpRequest.Content.Headers.ContentDisposition =
                headerMap.ContainsKey("Content-Disposition") ?
                    ContentDispositionHeaderValue.Parse(headerMap["Content-Disposition"]) :
                    httpRequest.Content.Headers.ContentDisposition;

            httpRequest.Content.Headers.ContentLocation =
                headerMap.ContainsKey("Content-Location") ?
                    new Uri(headerMap["Content-Location"]) :
                    httpRequest.Content.Headers.ContentLocation;

            httpRequest.Content.Headers.ContentRange =
                headerMap.ContainsKey("Content-Range") ?
                    ContentRangeHeaderValue.Parse(headerMap["Content-Range"]) :
                    httpRequest.Content.Headers.ContentRange;

            httpRequest.Content.Headers.ContentType =
                headerMap.ContainsKey("Content-Type") ?
                    MediaTypeHeaderValue.Parse(headerMap["Content-Type"]) :
                    httpRequest.Content.Headers.ContentType;

            httpRequest.Content.Headers.Expires =
                headerMap.ContainsKey("Expires") ?
                    DateTimeOffset.Parse(headerMap["Expires"]) :
                    httpRequest.Content.Headers.Expires;

            httpRequest.Content.Headers.LastModified =
                headerMap.ContainsKey("Last-Modified") ?
                    DateTimeOffset.Parse(headerMap["Last-Modified"]) :
                    httpRequest.Content.Headers.LastModified;
        }
    }
}

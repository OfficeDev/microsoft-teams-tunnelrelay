// <copyright file="HeaderAdditionPlugin.cs" company="Microsoft">
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

namespace TunnelRelay.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Plugin to add or replace headers.
    /// </summary>
    /// <seealso cref="TunnelRelay.PluginEngine.IRedirectionPlugin" />
    public class HeaderAdditionPlugin : IRedirectionPlugin
    {
        /// <summary>
        /// The headers to remove.
        /// </summary>
        private Dictionary<string, string> headersToAdd = new Dictionary<string, string>();

        /// <summary>
        /// The plugin data.
        /// </summary>
        private string pluginData;

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string PluginName { get => "HeaderReplacementPlugin"; }

        /// <summary>
        /// Gets or sets the plugin data.
        /// </summary>
        [PluginSetting("HeaderNames", "Headers should be separated by newline and should be in format \r\n HeaderName: HeaderValue")]
        public string PluginData
        {
            get
            {
                return this.pluginData;
            }

            set
            {
                this.pluginData = value;

                string[] headers = this.pluginData.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                headers.ToList().ForEach(header =>
                {
                    string[] headerSplits = header.Split(':');
                    this.headersToAdd[headerSplits[0].Trim()] = headerSplits[1].Trim();
                });
            }
        }

        /// <summary>
        /// Gets the help text.
        /// </summary>
        public string HelpText => "Adds or replaces header. Headers should be separated by newline and should be in format \r\n HeaderName: HeaderValue";

        /// <summary>
        /// Performes required processing after response is received from service asynchronously.
        /// </summary>
        /// <param name="webResponse">The web response.</param>
        /// <returns>
        /// Processed http web response.
        /// </returns>
        public Task<HttpResponseMessage> PostProcessResponseFromServiceAsync(HttpResponseMessage webResponse)
        {
            return Task.FromResult(webResponse);
        }

        /// <summary>
        /// Performes required processing before request is made to service asynchronously.
        /// </summary>
        /// <param name="webRequest">The web request.</param>
        /// <returns>
        /// Processed http web request.
        /// </returns>
        public Task<HttpRequestMessage> PreProcessRequestToServiceAsync(HttpRequestMessage webRequest)
        {
            foreach (var headerToAdd in this.headersToAdd)
            {
                if (webRequest.Headers.Contains(headerToAdd.Key))
                {
                    webRequest.Headers.Remove(headerToAdd.Key);
                }

                webRequest.Headers.TryAddWithoutValidation(headerToAdd.Key, headerToAdd.Value);
            }

            return Task.FromResult(webRequest);
        }
    }
}

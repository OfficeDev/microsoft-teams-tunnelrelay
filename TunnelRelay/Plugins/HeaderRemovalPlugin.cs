// <copyright file="HeaderRemovalPlugin.cs" company="Microsoft">
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
    /// Removed unrequired headers.
    /// </summary>
    /// <seealso cref="TunnelRelay.PluginEngine.IRedirectionPlugin" />
    internal class HeaderRemovalPlugin : IRedirectionPlugin
    {
        /// <summary>
        /// The headers to remove.
        /// </summary>
        private List<string> headersToRemove = new List<string>();

        /// <summary>
        /// The plugin data.
        /// </summary>
        private string pluginData;

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string PluginName => "HeaderRemovalPlugin";

        /// <summary>
        /// Gets or sets the plugin data.
        /// </summary>
        public string PluginData
        {
            get
            {
                return this.pluginData;
            }

            set
            {
                this.pluginData = value;
                this.headersToRemove = this.pluginData.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }

        /// <summary>
        /// Gets the help text.
        /// </summary>
        public string HelpText
        {
            get
            {
                return "Enter headers to remove separated by newline";
            }
        }

        /// <summary>
        /// Performes required processing after response is received from service.
        /// </summary>
        /// <param name="webResponse">The web response.</param>
        /// <returns>
        /// Processed http web response.
        /// </returns>
        public HttpResponseMessage PostProcessResponseFromService(HttpResponseMessage webResponse)
        {
            return webResponse;
        }

        /// <summary>
        /// Performes required processing before request is made to service.
        /// </summary>
        /// <param name="webRequest">The web request.</param>
        /// <returns>
        /// Processed http web request.
        /// </returns>
        public HttpRequestMessage PreProcessRequestToService(HttpRequestMessage webRequest)
        {
            this.headersToRemove.ForEach(headerToRemove =>
            {
                if (webRequest.Headers.Contains(headerToRemove))
                {
                    webRequest.Headers.Remove(headerToRemove);
                }
            });

            return webRequest;
        }
    }
}

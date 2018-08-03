// <copyright file="HeaderRemovalPlugin.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// Removed unrequired headers.
    /// </summary>
    /// <seealso cref="TunnelRelay.PluginEngine.ITunnelRelayPlugin" />
    public class HeaderRemovalPlugin : ITunnelRelayPlugin
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
        [PluginSetting("HeadersToRemove", "Comma separated header named to removed.")]
        public string PluginData
        {
            get
            {
                return this.pluginData;
            }

            set
            {
                this.pluginData = value;
                this.headersToRemove = this.pluginData.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(str => str.Trim()).ToList();
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
        /// Initializes this instance.
        /// </summary>
        /// <returns>Task tracking operation.</returns>
        public Task InitializeAsync()
        {
            return Task.FromResult(0);
        }

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
            this.headersToRemove.ForEach(headerToRemove =>
            {
                if (webRequest.Headers.Contains(headerToRemove))
                {
                    webRequest.Headers.Remove(headerToRemove);
                }
            });

            return Task.FromResult(webRequest);
        }
    }
}

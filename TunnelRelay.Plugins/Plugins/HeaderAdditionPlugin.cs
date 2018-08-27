// <copyright file="HeaderAdditionPlugin.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// Plugin to add or replace headers.
    /// </summary>
    /// <seealso cref="TunnelRelay.PluginEngine.ITunnelRelayPlugin" />
    public class HeaderAdditionPlugin : ITunnelRelayPlugin
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
            foreach (var headerToAdd in this.headersToAdd)
            {
                if (webRequest.Headers.Contains(headerToAdd.Key))
                {
                    webRequest.Headers.Remove(headerToAdd.Key);
                }

                bool val = webRequest.Headers.TryAddWithoutValidation(headerToAdd.Key, headerToAdd.Value);
            }

            return Task.FromResult(webRequest);
        }
    }
}

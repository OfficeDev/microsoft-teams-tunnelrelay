// <copyright file="ITunnelRelayPlugin.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.PluginEngine
{
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for developing plugins.
    /// </summary>
    public interface ITunnelRelayPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// Gets the help text.
        /// </summary>
        string HelpText { get; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <returns>Task tracking operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Performes required processing before request is made to service asynchronously.
        /// </summary>
        /// <param name="webRequest">The web request.</param>
        /// <returns>Processed http web request.</returns>
        Task<HttpRequestMessage> PreProcessRequestToServiceAsync(HttpRequestMessage webRequest);

        /// <summary>
        /// Performes required processing after response is received from service asynchronously.
        /// </summary>
        /// <param name="webResponse">The web response.</param>
        /// <returns>Processed http web response.</returns>
        Task<HttpResponseMessage> PostProcessResponseFromServiceAsync(HttpResponseMessage webResponse);
    }
}

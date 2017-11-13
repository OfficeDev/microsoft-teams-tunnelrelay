// <copyright file="ITunnelRelayPlugin.cs" company="Microsoft">
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

namespace TunnelRelay.PluginEngine
{
    using System.Net;
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
        Task Initialize();

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

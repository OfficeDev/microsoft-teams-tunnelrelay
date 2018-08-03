// <copyright file="IRelayRequestManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System.Threading.Tasks;

    /// <summary>
    /// Manages request received over the hybrid connection.
    /// </summary>
    public interface IRelayRequestManager
    {
        /// <summary>
        /// Handles the incoming request, passes it down to the internal service and returns the response.
        /// </summary>
        /// <param name="relayRequest">Incoming relay request.</param>
        /// <returns>Response from the internal service.</returns>
        Task<RelayResponse> HandleRelayRequestAsync(RelayRequest relayRequest);

        /// <summary>
        /// Updates the internal service url used to send requests.
        /// </summary>
        /// <param name="internalServiceUrl">New internal service url.</param>
        void UpdateInternalServiceUrl(string internalServiceUrl);
    }
}
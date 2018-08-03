// <copyright file="IRelayRequestEventListener.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System.Threading.Tasks;

    /// <summary>
    /// Listener for events for requests received over the hybrid connection relay.
    /// </summary>
    public interface IRelayRequestEventListener
    {
        /// <summary>
        /// Called when a request is received and is being processed by the relay.
        /// </summary>
        /// <param name="requestId">Unique request Id for the request.</param>
        /// <param name="relayRequest">Request received over the relay.</param>
        /// <returns>Task tracking operation.</returns>
        Task RequestReceivedAsync(string requestId, RelayRequest relayRequest);

        /// <summary>
        /// Called when request processing is done and response is being sent back.
        /// </summary>
        /// <param name="requestId">Unique request Id for the request.</param>
        /// <param name="relayResponse">Response received from the called service.</param>
        /// <returns>Task tracking operation.</returns>
        Task ResponseSentAsync(string requestId, RelayResponse relayResponse);
    }
}

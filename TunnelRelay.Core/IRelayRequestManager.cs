// <copyright file="IRelayRequestManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
  using System.Net.Http;
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
    /// Allows for re-issueing of a previously captured request.
    /// </summary>
    /// <param name="relayRequest"> relayrequest which was captured previously.</param>
    /// <param name="requestId">the id associated with the relay request when it was relayed by the Service Bus Relay.</param>
    /// <returns>HttpResponseMessage from the re-issued request.</returns>
    Task<HttpResponseMessage> RedirectRequestFromManagerAsync(RelayRequest relayRequest, string requestId);
    }
}
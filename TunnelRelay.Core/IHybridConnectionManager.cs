// <copyright file="IHybridConnectionManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Hybrid connection manager to manage connection between Azure and local machine.
    /// </summary>
    public interface IHybridConnectionManager
    {
        /// <summary>
        /// Closes the hybrid connection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task tracking operation.</returns>
        Task CloseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Opens the connections and prepares the request handler.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task tracking operation.</returns>
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}
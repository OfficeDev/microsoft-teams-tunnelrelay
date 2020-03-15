// <copyright file="HybridConnectionManagerOptions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    /// <summary>
    /// Hybrid connection manager options.
    /// </summary>
    public class HybridConnectionManagerOptions
    {
        /// <summary>
        /// Gets or sets the relay host.
        /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings - Isn't actually a URI.
        public string AzureRelayUrlHost { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings - Isn't actually a URI.

        /// <summary>
        /// Gets or sets the name of the relay key.
        /// </summary>
        public string AzureRelayKeyName { get; set; }

        /// <summary>
        /// Gets or sets the service Relay key.
        /// </summary>
        public string AzureRelaySharedKey { get; set; }

        /// <summary>
        /// Gets or sets the Connection path. This is the entity path of the hybrid connection.
        /// </summary>
        public string ConnectionPath { get; set; }
    }
}

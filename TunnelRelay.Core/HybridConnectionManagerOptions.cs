// <copyright file="HybridConnectionManagerOptions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System;

    /// <summary>
    /// Hybrid connection manager options.
    /// </summary>
    public class HybridConnectionManagerOptions
    {
        /// <summary>
        /// Gets or sets the service bus host.
        /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings - Isn't actually a URI.
        public string ServiceBusUrlHost { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings - Isn't actually a URI.

        /// <summary>
        /// Gets or sets the name of the service bus key.
        /// </summary>
        public string ServiceBusKeyName { get; set; }

        /// <summary>
        /// Gets or sets the service bus shared key.
        /// </summary>
        public string ServiceBusSharedKey { get; set; }

        /// <summary>
        /// Gets or sets the Connection path. This is the entity path of the hybrid connection.
        /// </summary>
        public string ConnectionPath { get; set; }
    }
}

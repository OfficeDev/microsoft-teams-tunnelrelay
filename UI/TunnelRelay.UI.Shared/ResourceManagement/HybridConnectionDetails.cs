// <copyright file="HybridConnectionDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.ResourceManagement
{
    /// <summary>
    /// Relay details.
    /// </summary>
    internal class HybridConnectionDetails
    {
        /// <summary>
        /// Gets or sets the Relay URL.
        /// </summary>
        public string RelayUrl { get; set; }

        /// <summary>
        /// Gets or sets the hybrid connection name.
        /// </summary>
        public string HybridConnectionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the hybrid connection key.
        /// </summary>
        public string HybridConnectionKeyName { get; set; }

        /// <summary>
        /// Gets or sets the hybrid connection shared key.
        /// </summary>
        public string HybridConnectionSharedKey { get; set; }
    }
}

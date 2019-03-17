// <copyright file="RelayRequestManagerOptions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System;

    /// <summary>
    /// Settings for <see cref="RelayRequestManager"/>.
    /// </summary>
    public class RelayRequestManagerOptions
    {
        /// <summary>
        /// Gets or sets the internal service url.
        /// </summary>
        public Uri InternalServiceUrl { get; set; }
    }
}

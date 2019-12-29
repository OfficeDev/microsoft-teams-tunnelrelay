// <copyright file="RequestEventArgs.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
    using System;

    /// <summary>
    /// Request received event arguments.
    /// </summary>
    /// <seealso cref="EventArgs" />
    public class RequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the request.
        /// </summary>
        public RequestDetails Request { get; internal set; }
    }
}

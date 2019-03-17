// <copyright file="RequestConsoleDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Console
{
    using System;

    /// <summary>
    /// Stores console level details for each request received.
    /// </summary>
    internal class RequestConsoleDetails
    {
        /// <summary>
        /// Gets or sets the start time of the request.
        /// </summary>
        public DateTime RequestStartTime { get; set; }

        /// <summary>
        /// Gets or sets the row at which the console line for this request was written.
        /// </summary>
        public int RequestConsoleTopCursor { get; set; }

        /// <summary>
        /// Gets or sets the column at which the console line was being written.
        /// Tells till what point the text is already written to allow console entry to start from ahead of it.
        /// </summary>
        public int RequestConsoleLeftCursor { get; set; }
    }
}

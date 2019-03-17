// <copyright file="NullScope.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.Logger
{
    using System;

    /// <summary>
    /// An empty scope without any logic.
    /// </summary>
    internal class NullScope : IDisposable
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="NullScope"/> class from being created.
        /// </summary>
        private NullScope()
        {
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static NullScope Instance { get; } = new NullScope();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
#pragma warning disable CA1063 // Implement IDisposable Correctly - Nothing at all to dispose.
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly - Nothing at all to dispose.
        {
        }
    }
}

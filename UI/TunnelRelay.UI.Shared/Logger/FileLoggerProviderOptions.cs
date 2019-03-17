// <copyright file="FileLoggerProviderOptions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.Logger
{
    /// <summary>
    /// Options for <see cref="FileLoggerProvider"/>.
    /// </summary>
    internal class FileLoggerProviderOptions
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; set; }
    }
}

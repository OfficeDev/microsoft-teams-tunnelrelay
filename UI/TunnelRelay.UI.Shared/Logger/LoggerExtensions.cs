// <copyright file="LoggerExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Microsoft.Extensions.Logging
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using TunnelRelay.UI.Logger;

    /// <summary>
    /// Logger extensions.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Adds the file logger.
        /// </summary>
        /// <param name="loggingBuilder">The logging builder.</param>
        /// <returns>The logging builder with new provider added to it.</returns>
        public static ILoggingBuilder AddFileLogger(this ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

            return loggingBuilder;
        }
    }
}

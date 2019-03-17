// <copyright file="LoggingHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using TunnelRelay.UI.Logger;

    /// <summary>
    /// Logging helper.
    /// </summary>
    internal class LoggingHelper
    {
        private static IServiceProvider serviceProvider = new ServiceCollection()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.Services.Configure<FileLoggerProviderOptions>((fileLoggerProviderOptions) =>
                {
                    fileLoggerProviderOptions.FileName = "TunnelRelayWindows.log";
                });

                loggingBuilder.AddFileLogger();
            })
            .BuildServiceProvider();

        /// <summary>
        /// Gets a logger.
        /// </summary>
        /// <typeparam name="T">Type of logger.</typeparam>
        /// <returns>Logger for logging traces to.</returns>
        public static ILogger<T> GetLogger<T>()
        {
            return serviceProvider.GetRequiredService<ILogger<T>>();
        }
    }
}

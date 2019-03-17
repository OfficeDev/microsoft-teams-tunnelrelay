// <copyright file="LoggingHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Logging helper.
    /// </summary>
    internal class LoggingHelper
    {
        private static IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes static members of the <see cref="LoggingHelper"/> class.
        /// </summary>
        static LoggingHelper()
        {
            ServiceCollection serviceDescriptors = new ServiceCollection();

            serviceDescriptors.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFileLogger();
            });

            serviceProvider = serviceDescriptors.BuildServiceProvider();
        }

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

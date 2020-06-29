// <copyright file="StreamLogger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.Logger
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Stream logger.
    /// </summary>
    /// <seealso cref="ILogger" />
    internal class StreamLogger : ILogger
    {
        private readonly TextWriter textWriter;
        private readonly string categoryName;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamLogger"/> class.
        /// </summary>
        /// <param name="textWriter">The text writer.</param>
        /// <param name="categoryName">Name of the category.</param>
        /// <exception cref="ArgumentNullException">streamWriter.</exception>
        public StreamLogger(TextWriter textWriter, string categoryName)
        {
            this.textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
            this.categoryName = categoryName;
        }

        /// <summary>
        /// Gets or sets the external scope provider.
        /// </summary>
        internal IExternalScopeProvider ExternalScopeProvider { get; set; }

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <typeparam name="TState">New state to be added.</typeparam>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>
        /// An IDisposable that ends the logical operation scope on dispose.
        /// </returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return this.ExternalScopeProvider != null ? this.ExternalScopeProvider.Push(state) : NullScope.Instance;
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">level to be checked.</param>
        /// <returns>
        ///   <c>true</c> if enabled.
        /// </returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <typeparam name="TState">State information.</typeparam>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <c>string</c> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Intentionally leaving Scope out. If needed can be added later.
            string message = $"{logLevel} - {this.categoryName} - {formatter(state, exception)}";

            if (exception != null)
            {
                message = message + "Exception - " + exception;
            }

            try
            {
                this.textWriter.WriteLine(message);
            }
#pragma warning disable CS0168 // Variable is declared but never used - Kept here for debugging purposes.
            catch (Exception ex)
#pragma warning restore CS0168 // Variable is declared but never used - Kept here for debugging purposes.
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }
        }
    }
}

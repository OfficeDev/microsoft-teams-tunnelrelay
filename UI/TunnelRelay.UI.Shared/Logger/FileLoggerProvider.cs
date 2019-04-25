// <copyright file="FileLoggerProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.Logger
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// File logger provider.
    /// </summary>
    /// <seealso cref="ILoggerProvider" />
    internal class FileLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Name of the log file name.
        /// </summary>
        private readonly string logFileName;

        /// <summary>
        /// The lock object used for synchronizing stream opening.
        /// </summary>
        private object lockObject = new object();

        /// <summary>
        /// The stream writer.
        /// </summary>
        private StreamWriter streamWriter = null;

        /// <summary>
        /// The external scope provider to allow setting scope data in messages.
        /// </summary>
        private IExternalScopeProvider externalScopeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLoggerProvider"/> class.
        /// </summary>
        /// <param name="fileLoggerProviderOptions">Logger options.</param>
        public FileLoggerProvider(IOptions<FileLoggerProviderOptions> fileLoggerProviderOptions)
        {
            if (string.IsNullOrEmpty(fileLoggerProviderOptions?.Value?.FileName))
            {
                throw new ArgumentNullException(nameof(fileLoggerProviderOptions));
            }

            this.logFileName = fileLoggerProviderOptions.Value.FileName;
        }

        /// <summary>
        /// Creates a new <see cref="ILogger" /> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>An <see cref="ILogger"/> instance to be used for logging.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            if (this.streamWriter == null)
            {
                lock (this.lockObject)
                {
                    if (this.streamWriter == null)
                    {
                        // Opening in shared mode because other logger instances are also using the same file. So all of them can write to the same file.
                        this.streamWriter = new StreamWriter(new FileStream(this.logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            AutoFlush = true,
                            NewLine = Environment.NewLine,
                        };
                    }
                }
            }

            return new StreamLogger(this.streamWriter, categoryName)
            {
                ExternalScopeProvider = this.externalScopeProvider,
            };
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sets the scope provider. This method also updates all the existing logger to also use the new ScopeProvider.
        /// </summary>
        /// <param name="externalScopeProvider">The external scope provider.</param>
        public void SetScopeProvider(IExternalScopeProvider externalScopeProvider)
        {
            this.externalScopeProvider = externalScopeProvider;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="releasedManagedResources">Release managed resources.</param>
        protected virtual void Dispose(bool releasedManagedResources)
        {
            if (this.streamWriter != null)
            {
                this.streamWriter.Flush();
                this.streamWriter.Close();
                this.streamWriter.Dispose();
            }
        }
    }
}

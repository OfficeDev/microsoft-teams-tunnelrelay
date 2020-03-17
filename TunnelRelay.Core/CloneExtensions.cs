// <copyright file="CloneExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Extensions to clone classes which do not implement <see cref="ICloneable"/> interface.
    /// </summary>
    internal static class CloneExtensions
    {
        /// <summary>
        /// Clones a stream and returns new stream with position set to 0.
        /// </summary>
        /// <param name="stream">Stream to clone.</param>
        /// <returns>Cloned stream.</returns>
        /// <remarks>
        /// The original stream position will be set to 0 if the stream is seekable. Otherwise the position will be set to end.
        /// </remarks>
        internal static Stream Clone(this Stream stream)
        {
            if (stream == null)
            {
                return null;
            }

            long currentPosition = stream.Position;

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            else if (currentPosition != 0)
            {
                throw new InvalidOperationException("Can't clone unseekable stream with position not at 0");
            }

            MemoryStream memoryStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            // If we can reset the stream position to 0.
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            return memoryStream;
        }

        /// <summary>
        /// Clones the WebHeaderCollection instance.
        /// </summary>
        /// <param name="webHeaderCollection">Instance to clone.</param>
        /// <returns>Cloned instance.</returns>
        internal static WebHeaderCollection Clone(this WebHeaderCollection webHeaderCollection)
        {
            if (webHeaderCollection == null)
            {
                return null;
            }

            WebHeaderCollection clonedCollection = new WebHeaderCollection();

            foreach (string headerName in webHeaderCollection.Keys)
            {
                clonedCollection[headerName] = webHeaderCollection[headerName];
            }

            return clonedCollection;
        }
    }
}

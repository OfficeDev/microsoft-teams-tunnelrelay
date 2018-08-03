// <copyright file="RelayRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;

    /// <summary>
    /// Request received over Hybrid connection relay.
    /// </summary>
    public class RelayRequest : ICloneable
    {
        /// <summary>
        /// Gets the HTTP method on the incoming request.
        /// </summary>
        public HttpMethod HttpMethod { get; internal set; }

        /// <summary>
        /// Gets the incoming headers.
        /// </summary>
        public WebHeaderCollection Headers { get; internal set; }

        /// <summary>
        /// Gets the incoming stream.
        /// </summary>
        public Stream InputStream { get; internal set; }

        /// <summary>
        /// Gets the relative url for the incoming request.
        /// </summary>
        public string RelativeUrl { get; internal set; }

        /// <summary>
        /// Clones the current instance.
        /// </summary>
        /// <returns>Cloned instance.</returns>
        public object Clone()
        {
            return new RelayRequest
            {
                HttpMethod = new HttpMethod(this.HttpMethod.ToString()),
                Headers = this.Headers.Clone(),
                InputStream = this.InputStream.Clone(),
                RelativeUrl = this.RelativeUrl.Clone() as string,
            };
        }
    }
}

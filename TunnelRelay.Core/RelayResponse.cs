// <copyright file="RelayResponse.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
    using System;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Relay response received from the service.
    /// </summary>
    public class RelayResponse : ICloneable
    {
        /// <summary>
        /// Gets the HTTP status code for the response.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; internal set; }

        /// <summary>
        /// Gets the reason phrase for the status.
        /// </summary>
        public string StatusDescription { get; internal set; }

        /// <summary>
        /// Gets the outgoing headers.
        /// </summary>
        public WebHeaderCollection Headers { get; internal set; }

        /// <summary>
        /// Gets the output stream.
        /// </summary>
        public Stream OutputStream { get; internal set; }

        /// <summary>
        /// Gets the time at which request ended.
        /// </summary>
        public DateTimeOffset RequestEndDateTime { get; internal set; }

        /// <summary>
        /// Clones the current instance.
        /// </summary>
        /// <returns>Cloned instance.</returns>
        public object Clone()
        {
            return new RelayResponse
            {
                HttpStatusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), this.HttpStatusCode.ToString()),
                StatusDescription = this.StatusDescription.Clone() as string,
                Headers = this.Headers.Clone(),
                OutputStream = this.OutputStream.Clone(),
                RequestEndDateTime = new DateTimeOffset(this.RequestEndDateTime.DateTime),
            };
        }
    }
}

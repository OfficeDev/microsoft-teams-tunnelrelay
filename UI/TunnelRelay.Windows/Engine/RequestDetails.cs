// <copyright file="RequestDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request Details for UI.
    /// </summary>
    public class RequestDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDetails"/> class.
        /// </summary>
        public RequestDetails()
        {
            this.ResponseHeaders = new List<HeaderDetails>();
        }

        /// <summary>
        /// Gets or sets the HTTP method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the request receive time.
        /// </summary>
        public DateTime RequestReceiveTime { get; set; }

        /// <summary>
        /// Gets or sets the request headers.
        /// </summary>
        public List<HeaderDetails> RequestHeaders { get; set; }

        /// <summary>
        /// Gets or sets the request data.
        /// </summary>
        public string RequestData { get; set; }

        /// <summary>
        /// Gets or sets the response headers.
        /// </summary>
        public List<HeaderDetails> ResponseHeaders { get; set; }

        /// <summary>
        /// Gets or sets the response data.
        /// </summary>
        public string ResponseData { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public string Duration { get; set; }
    }
}

// <copyright file="RequestDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
// Licensed under the MIT license.
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace TunnelRelay
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net;

    /// <summary>
    /// Request Details for UI.
    /// </summary>
    internal class RequestDetails
    {
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
        /// Gets or sets a value indicating whether exception was hit during exeution.
        /// </summary>
        public bool ExceptionHit { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the request receive time.
        /// </summary>
        public DateTime RequestReceiveTime { get; set; }

        /// <summary>
        /// Gets or sets the request headers.
        /// </summary>
        public ObservableCollection<HeaderDetails> RequestHeaders { get; set; }

        /// <summary>
        /// Gets or sets the request data.
        /// </summary>
        public string RequestData { get; set; }

        /// <summary>
        /// Gets or sets the request sender.
        /// </summary>
        public string RequestSender { get; set; }

        /// <summary>
        /// Gets or sets the response headers.
        /// </summary>
        public ObservableCollection<HeaderDetails> ResponseHeaders { get; set; }

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

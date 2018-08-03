// <copyright file="RequestContext.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TunnelRelay.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Request context for internal handling of request, response, routing etc.
    /// </summary>
    internal class RequestContext
    {
        private Dictionary<string, string> incomingRequestHeaders;

        /// <summary>
        /// Gets or sets the incoming web request.
        /// </summary>
        public IncomingWebRequestContext IncomingWebRequest { get; set; }

        /// <summary>
        /// Gets or sets the incoming request stream.
        /// </summary>
        public MemoryStream IncomingRequestStream { get; set; }

        /// <summary>
        /// Gets the incoming request headers in simplified dictionary.
        /// </summary>
        public Dictionary<string, string> IncomingRequestHeaders
        {
            get
            {
                if (this.incomingRequestHeaders == null)
                {
                    // Not handling NullRef for now.
                    this.incomingRequestHeaders = this.IncomingWebRequest.Headers.GetHeadersMap();
                }

                return this.incomingRequestHeaders;
            }
        }

        /// <summary>
        /// Gets or sets the UI request description.
        /// </summary>
        public RequestDetails UIRequestDescription { get; set; }
    }
}

// <copyright file="WCFContract.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using TunnelRelay.PluginEngine;

    /// <summary>
    /// WCF contract for handling requests.
    /// </summary>
    [ServiceContract]
    internal class WCFContract
    {
        /// <summary>
        /// Services the GET request.
        /// </summary>
        /// <returns>Response from underlying service.</returns>
        [OperationContract]
        [WebGet(UriTemplate = "*")]
        private async Task<Message> GetRequest()
        {
            return await ApplicationEngine.GetResponse(WebOperationContext.Current);
        }

        /// <summary>
        /// Services the POST request.
        /// </summary>
        /// <param name="pStream">Incoming request stream.</param>
        /// <returns>Response from underlying service.</returns>
        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        private async Task<Message> PostRequest(Stream pStream)
        {
            return await ApplicationEngine.GetResponse(WebOperationContext.Current, pStream);
        }

        /// <summary>
        /// Services the OPTIONS request.
        /// </summary>
        /// <param name="pStream">Incoming request stream.</param>
        /// <returns>Response from underlying service.</returns>
        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "OPTIONS", BodyStyle = WebMessageBodyStyle.Bare)]
        private async Task<Message> OptionsRequest(Stream pStream)
        {
            return await ApplicationEngine.GetResponse(WebOperationContext.Current, pStream);
        }

        /// <summary>
        /// Services the PUT request.
        /// </summary>
        /// <param name="pStream">Incoming request stream.</param>
        /// <returns>Response from underlying service.</returns>
        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "PUT", BodyStyle = WebMessageBodyStyle.Bare)]
        private async Task<Message> PutRequest(Stream pStream)
        {
            return await ApplicationEngine.GetResponse(WebOperationContext.Current, pStream);
        }

        /// <summary>
        /// Services the DELETE request.
        /// </summary>
        /// <returns>Response from underlying service.</returns>
        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "DELETE")]
        private async Task<Message> DeleteRequest()
        {
            return await ApplicationEngine.GetResponse(WebOperationContext.Current);
        }
    }
}

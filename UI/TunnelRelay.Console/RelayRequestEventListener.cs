// <copyright file="RelayRequestEventListener.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Console
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TunnelRelay.Core;

    /// <summary>
    /// Listens to event when request are being relayed.
    /// </summary>
    internal class RelayRequestEventListener : IRelayRequestEventListener
    {
        private readonly Dictionary<string, RequestConsoleDetails> requestConsoleDetailsMap = new Dictionary<string, RequestConsoleDetails>();
        private readonly object lockObject = new object();
        private int requestCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayRequestEventListener"/> class.
        /// </summary>
        public RelayRequestEventListener()
        {
            this.requestCount = Console.CursorTop + 1;
        }

        /// <summary>
        /// Processes an event when request is received.
        /// </summary>
        /// <param name="requestId">Unique Id of the request.</param>
        /// <param name="relayRequest">Relay request.</param>
        /// <returns>Task tracking request.</returns>
        public Task RequestReceivedAsync(string requestId, RelayRequest relayRequest)
        {
            lock (this.lockObject)
            {
                Console.SetCursorPosition(0, this.requestCount);

                Console.Write(string.Format(
                    "{0} - {1}",
                    relayRequest.HttpMethod,
                    relayRequest.RequestPathAndQuery));

                this.requestConsoleDetailsMap[requestId] = new RequestConsoleDetails
                {
                    RequestStartTime = DateTime.Now,
                    RequestConsoleTopCursor = this.requestCount,
                    RequestConsoleLeftCursor = Console.CursorLeft,
                };

                this.requestCount += 2;
                Console.WriteLine();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes an event when response is sent.
        /// </summary>
        /// <param name="requestId">Unique Id of the request.</param>
        /// <param name="relayResponse">Relay response.</param>
        /// <returns>Task tracking operation.</returns>
        public Task ResponseSentAsync(string requestId, RelayResponse relayResponse)
        {
            lock (this.lockObject)
            {
                RequestConsoleDetails requestConsoleDetails = this.requestConsoleDetailsMap[requestId];
                this.requestConsoleDetailsMap.Remove(requestId);

                Console.SetCursorPosition(
                    0,
                    requestConsoleDetails.RequestConsoleTopCursor + 1);

                Console.Write(string.Format(
                    " -----> {0} - {1}",
                    relayResponse.HttpStatusCode.ToString(),
                    (DateTime.Now - requestConsoleDetails.RequestStartTime).TotalMilliseconds));
                Console.WriteLine();
            }

            return Task.CompletedTask;
        }
    }
}

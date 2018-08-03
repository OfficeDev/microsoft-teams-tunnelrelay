using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TunnelRelay.Core;

namespace TunnelRelay.Console.Shared
{
    internal class RelayRequestEventListener : IRelayRequestEventListener
    {
        Dictionary<string, RequestConsoleDetails> requestConsoleDetailsMap = new Dictionary<string, RequestConsoleDetails>();
        int requestCount = 0;
        object lockObject = new object();

        public Task RequestReceivedAsync(string requestId, RelayRequest relayRequest)
        {
            lock (lockObject)
            {
                System.Console.Write(string.Format(
                    "{0} - {1} - {2}",
                    relayRequest.HttpMethod,
                    relayRequest.RelativeUrl,
                    "Running"));

                requestConsoleDetailsMap[requestId] = new RequestConsoleDetails
                {
                    RequestStartTime = DateTime.Now,
                    RequestConsoleTopCursor = requestCount,
                    RequestConsoleLeftCursor = System.Console.CursorLeft - 7,
                };

                requestCount = requestCount + 1;
                System.Console.WriteLine();
            }

            return Task.CompletedTask;
        }

        public Task ResponseSentAsync(string requestId, RelayResponse relayResponse)
        {
            lock (lockObject)
            {
                RequestConsoleDetails requestConsoleDetails = requestConsoleDetailsMap[requestId];
                requestConsoleDetailsMap.Remove(requestId);

                System.Console.SetCursorPosition(
                    requestConsoleDetails.RequestConsoleLeftCursor,
                    requestConsoleDetails.RequestConsoleTopCursor);

                System.Console.Write(string.Format(
                    "{0} - {1}",
                    relayResponse.HttpStatusCode.ToString(),
                    (DateTime.Now - requestConsoleDetails.RequestStartTime).TotalMilliseconds));
                System.Console.SetCursorPosition(0, requestCount + 1);
            }

            return Task.CompletedTask;
        }
    }
}

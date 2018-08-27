namespace TunnelRelay.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TunnelRelay.Core;

    internal class RelayRequestEventListener : IRelayRequestEventListener
    {
        public Task RequestReceivedAsync(string requestId, RelayRequest relayRequest)
        {
            throw new NotImplementedException();
        }

        public Task ResponseSentAsync(string requestId, RelayResponse relayResponse)
        {
            throw new NotImplementedException();
        }
    }
}

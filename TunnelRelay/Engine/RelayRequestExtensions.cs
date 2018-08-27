using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TunnelRelay.Engine
{
    internal static class RelayRequestExtensions
    {
        public static List<HeaderDetails> GetHeaderMap(this WebHeaderCollection headerCollection)
        {
            List<HeaderDetails> headers = new List<HeaderDetails>();

            foreach (string headerName in headerCollection.AllKeys)
            {
                headers.Add(new HeaderDetails
                {
                    HeaderName = headerName,
                    HeaderValue = headerCollection[headerName]
                });
            }

            return headers;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace TunnelRelay.Console.Shared
{
    internal class RequestConsoleDetails
    {
        public DateTime RequestStartTime { get; set; }

        public int RequestConsoleTopCursor { get; set; }

        public int RequestConsoleLeftCursor { get; set; }
    }
}

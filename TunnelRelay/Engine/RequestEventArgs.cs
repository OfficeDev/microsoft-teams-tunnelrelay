namespace TunnelRelay.Engine
{
    using System;

    /// <summary>
    /// Request received event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class RequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the request.
        /// </summary>
        public RequestDetails Request { get; internal set; }
    }
}

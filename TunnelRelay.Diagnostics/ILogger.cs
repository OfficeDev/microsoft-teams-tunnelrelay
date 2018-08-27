namespace TunnelRelay.Diagnostics
{
    using System;

    /// <summary>
    /// Logging interface.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="callInfo">Caller information.</param>
        /// <param name="message">String message to log.</param>
        void LogInfo(CallInfo callInfo, string message);

        /// <summary>
        /// Logs the verbose.
        /// </summary>
        /// <param name="callInfo">Caller information.</param>
        /// <param name="message">String message to log.</param>
        void LogVerbose(CallInfo callInfo, string message);

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="callInfo">Caller information.</param>
        /// <param name="message">String message to log.</param>
        void LogWarning(CallInfo callInfo, string message);

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="callInfo">Caller information.</param>
        /// <param name="message">String message to log.</param>
        void LogError(CallInfo callInfo, string message);

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Uninitializes this instance.
        /// </summary>
        void Uninitialize();
    }
}

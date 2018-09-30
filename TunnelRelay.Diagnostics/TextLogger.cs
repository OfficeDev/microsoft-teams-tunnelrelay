namespace TunnelRelay.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Text logger to log into a text file.
    /// </summary>
    /// <seealso cref="ILogger" />
    public class TextLogger : ILogger
    {
        /// <summary>
        /// The stream writer to write into text logs.
        /// </summary>
        private StreamWriter streamWriter = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextLogger"/> class.
        /// </summary>
        public TextLogger()
        {
            this.streamWriter = new StreamWriter(Path.Combine(
                Path.GetTempPath(),
                string.Format("TR{0}.Log", DateTime.Now.ToString("yyyyMMddHHmmss"))));
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TextLogger"/> class.
        /// </summary>
        ~TextLogger()
        {
            this.streamWriter.Close();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// Uninitializes this instance.
        /// </summary>
        public void Uninitialize()
        {
            this.streamWriter.Flush();
            this.streamWriter.Close();
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="message">The message to log.</param>
        public void LogError(CallInfo callInfo, string message)
        {
            this.Log(callInfo, TraceLevel.Error, message);
        }

        /// <summary>
        /// Logs the inmessageion.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="message">The message to log.</param>
        public void LogInfo(CallInfo callInfo, string message)
        {
            this.Log(callInfo, TraceLevel.Info, message);
        }

        /// <summary>
        /// Logs the verbose.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="message">The message to log.</param>
        public void LogVerbose(CallInfo callInfo, string message)
        {
            this.Log(callInfo, TraceLevel.Verbose, message);
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="message">The message to log.</param>
        public void LogWarning(CallInfo callInfo, string message)
        {
            this.Log(callInfo, TraceLevel.Warning, message);
        }

        /// <summary>
        /// Logs the specified inmessageion.
        /// </summary>
        /// <param name="callInfo">The call inmessageion.</param>
        /// <param name="errorLevel">The error level.</param>
        /// <param name="message">Message to log.</param>
        private void Log(CallInfo callInfo, TraceLevel errorLevel, string message)
        {
            this.streamWriter.WriteLine(string.Format("{0} - {1} - {2}", callInfo.ToString(), errorLevel.ToString(), message));
        }
    }
}

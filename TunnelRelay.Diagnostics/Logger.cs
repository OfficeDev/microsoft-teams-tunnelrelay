namespace TunnelRelay.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Manages logging for the application.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// The loggers.
        /// </summary>
        private static HashSet<ILogger> loggers = new HashSet<ILogger>();

        /// <summary>
        /// Registers the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public static void RegisterLogger(ILogger logger)
        {
            logger.Initialize();
            loggers.Add(logger);
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogError(CallInfo callInfo, string messageFormat, params object[] parameters)
        {
            Log(callInfo, TraceLevel.Error, messageFormat, parameters);
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogError(CallInfo callInfo, Exception exception, string messageFormat = null, params object[] parameters)
        {
            Log(callInfo, TraceLevel.Error, messageFormat == null ? exception.ToString() : (messageFormat + " \r\nException:" + exception.ToString()), parameters);
        }

        /// <summary>
        /// Logs the inmessageion.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogInfo(CallInfo callInfo, string messageFormat, params object[] parameters)
        {
            Log(callInfo, TraceLevel.Info, messageFormat, parameters);
        }

        /// <summary>
        /// Logs the verbose.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogVerbose(CallInfo callInfo, string messageFormat, params object[] parameters)
        {
            Log(callInfo, TraceLevel.Verbose, messageFormat, parameters);
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogWarning(CallInfo callInfo, string messageFormat, params object[] parameters)
        {
            Log(callInfo, TraceLevel.Warning, messageFormat, parameters);
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogWarning(CallInfo callInfo, Exception exception, string messageFormat = null, params object[] parameters)
        {
            Log(callInfo, TraceLevel.Warning, messageFormat == null ? exception.ToString() : (messageFormat + " \r\nException:" + exception.ToString()), parameters);
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public static void Close()
        {
            foreach (var logger in loggers)
            {
                logger.Uninitialize();
            }
        }

        /// <summary>
        /// Logs the specified inmessageion.
        /// </summary>
        /// <param name="callInfo">The call inmessageion.</param>
        /// <param name="errorLevel">The error level.</param>
        /// <param name="messageFormat">Message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        private static void Log(CallInfo callInfo, TraceLevel errorLevel, string messageFormat, params object[] parameters)
        {
            string messageString = parameters?.Length == 0 ? messageFormat : string.Format(messageFormat, parameters);

            foreach (var logger in loggers)
            {
                try
                {
                    switch (errorLevel)
                    {
                        case TraceLevel.Error:
                            logger.LogError(callInfo, messageString);
                            break;
                        case TraceLevel.Info:
                            logger.LogInfo(callInfo, messageString);
                            break;
                        case TraceLevel.Verbose:
                            logger.LogVerbose(callInfo, messageString);
                            break;
                        case TraceLevel.Warning:
                            logger.LogWarning(callInfo, messageString);
                            break;
                    }
                }
                catch
                {
                }
            }
        }
    }
}

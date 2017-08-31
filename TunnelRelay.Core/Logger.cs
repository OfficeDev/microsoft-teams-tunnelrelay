// <copyright file="Logger.cs" company="Microsoft">
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

namespace TunnelRelay.Core
{
    using System;
    using System.Collections.Generic;

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
            Log(callInfo, ErrorLevel.Error, messageFormat, parameters);
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
            Log(callInfo, ErrorLevel.Error, messageFormat == null ? exception.ToString() : (messageFormat + " \r\nException:" + exception.ToString()), parameters);
        }

        /// <summary>
        /// Logs the inmessageion.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogInfo(CallInfo callInfo, string messageFormat, params object[] parameters)
        {
            Log(callInfo, ErrorLevel.Info, messageFormat, parameters);
        }

        /// <summary>
        /// Logs the verbose.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogVerbose(CallInfo callInfo, string messageFormat, params object[] parameters)
        {
            Log(callInfo, ErrorLevel.Verbose, messageFormat, parameters);
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="messageFormat">Format of the message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        public static void LogWarning(CallInfo callInfo, string messageFormat, params object[] parameters)
        {
            Log(callInfo, ErrorLevel.Warning, messageFormat, parameters);
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
            Log(callInfo, ErrorLevel.Warning, messageFormat == null ? exception.ToString() : (messageFormat + " \r\nException:" + exception.ToString()), parameters);
        }

        /// <summary>
        /// Logs the specified inmessageion.
        /// </summary>
        /// <param name="callInfo">The call inmessageion.</param>
        /// <param name="errorLevel">The error level.</param>
        /// <param name="messageFormat">Message to log.</param>
        /// <param name="parameters">Parameters for the format string.</param>
        private static void Log(CallInfo callInfo, ErrorLevel errorLevel, string messageFormat, params object[] parameters)
        {
            string messageString = parameters?.Length == 0 ? messageFormat : string.Format(messageFormat, parameters);

            foreach (var logger in loggers)
            {
                try
                {
                    switch (errorLevel)
                    {
                        case ErrorLevel.Error:
                            logger.LogError(callInfo, messageString);
                            break;
                        case ErrorLevel.Info:
                            logger.LogInfo(callInfo, messageString);
                            break;
                        case ErrorLevel.Verbose:
                            logger.LogVerbose(callInfo, messageString);
                            break;
                        case ErrorLevel.Warning:
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

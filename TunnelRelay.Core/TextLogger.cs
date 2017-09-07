// <copyright file="TextLogger.cs" company="Microsoft">
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
    using System.IO;

    /// <summary>
    /// Error level enum.
    /// </summary>
    internal enum ErrorLevel
    {
        /// <summary>
        /// The verbose.
        /// </summary>
        Verbose,

        /// <summary>
        /// The inmessageion.
        /// </summary>
        Info,

        /// <summary>
        /// The warning.
        /// </summary>
        Warning,

        /// <summary>
        /// The error.
        /// </summary>
        Error,
    }

    /// <summary>
    /// Text logger to log into a text file.
    /// </summary>
    /// <seealso cref="TunnelRelay.Core.ILogger" />
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
            this.streamWriter = new StreamWriter(string.Format("TR{0}.Log", DateTime.Now.ToString("yyyyMMddHHmmss")));
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
            this.Log(callInfo, ErrorLevel.Error, message);
        }

        /// <summary>
        /// Logs the inmessageion.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="message">The message to log.</param>
        public void LogInfo(CallInfo callInfo, string message)
        {
            this.Log(callInfo, ErrorLevel.Info, message);
        }

        /// <summary>
        /// Logs the verbose.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="message">The message to log.</param>
        public void LogVerbose(CallInfo callInfo, string message)
        {
            this.Log(callInfo, ErrorLevel.Verbose, message);
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="callInfo">Caller inmessageion.</param>
        /// <param name="message">The message to log.</param>
        public void LogWarning(CallInfo callInfo, string message)
        {
            this.Log(callInfo, ErrorLevel.Warning, message);
        }

        /// <summary>
        /// Logs the specified inmessageion.
        /// </summary>
        /// <param name="callInfo">The call inmessageion.</param>
        /// <param name="errorLevel">The error level.</param>
        /// <param name="message">Message to log.</param>
        private void Log(CallInfo callInfo, ErrorLevel errorLevel, string message)
        {
            this.streamWriter.WriteLine(string.Format("{0} - {1} - {2}", callInfo.ToString(), errorLevel.ToString(), message));
        }
    }
}

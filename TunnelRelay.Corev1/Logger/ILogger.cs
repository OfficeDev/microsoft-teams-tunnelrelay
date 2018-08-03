// <copyright file="ILogger.cs" company="Microsoft">
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

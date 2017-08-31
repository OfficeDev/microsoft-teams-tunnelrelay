// <copyright file="CallInfo.cs" company="Microsoft">
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
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// Holds information such as file name, line number, and method name of the caller.
    /// </summary>
    public struct CallInfo
    {
        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string ComponentName { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the line number.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the method name.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Builds a CallInfo object with the necessary details for logging.
        /// </summary>
        /// <param name="memberName">CallerMemberName parameter.</param>
        /// <param name="sourceFilePath">CallerFilePath parameter.</param>
        /// <param name="sourceLineNumber">CallerLineNumber parameter.</param>
        /// <returns>A CallInfo object.</returns>
        public static CallInfo Site(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            CallInfo callInfo = new CallInfo
            {
                MethodName = memberName,
                FilePath = sourceFilePath,
                LineNumber = sourceLineNumber,
                ComponentName = Path.GetFileName(sourceFilePath),
            };

            return callInfo;
        }

        /// <summary>
        /// Returns a formatted string.
        /// </summary>
        /// <returns>A formatted string.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.FilePath + ":");
            sb.Append(this.LineNumber + " ");
            sb.Append(this.MethodName);

            return sb.ToString();
        }
    }
}

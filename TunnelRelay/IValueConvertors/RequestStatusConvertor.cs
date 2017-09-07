// <copyright file="RequestStatusConvertor.cs" company="Microsoft">
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

namespace TunnelRelay
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Data;
    using System.Windows.Media;
    using TunnelRelay.Core;

    /// <summary>
    /// Decides the color of row in secret management.
    /// </summary>
    /// <seealso cref="System.Windows.Data.IValueConverter" />
    internal class RequestStatusConvertor : IValueConverter
    {
        /// <summary>Converts a value.</summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RequestDetails requestData = ((KeyValuePair<string, RequestDetails>)value).Value;

            if (!Enum.TryParse(requestData?.StatusCode, out HttpStatusCode statusCode))
            {
                if (requestData?.StatusCode.Equals("Active") == true)
                {
                    return Brushes.Green;
                }
                else if (requestData?.StatusCode.Equals("Exception!!") == true)
                {
                    return Brushes.Red;
                }
                else
                {
                    return Brushes.Purple;
                }
            }

            if (requestData != null)
            {
                if ((int)statusCode >= 200 && (int)statusCode < 300)
                {
                    return Brushes.Blue;
                }
                else if ((int)statusCode >= 400 && (int)statusCode < 500)
                {
                    return Brushes.DarkOrange;
                }
                else if ((int)statusCode >= 500)
                {
                    return Brushes.Red;
                }
                else
                {
                    return Brushes.Purple;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>Converts a value.</summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <exception cref="NotImplementedException">Convert back not allowed</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Convert back not allowed");
        }
    }
}

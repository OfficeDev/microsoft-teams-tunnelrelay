// <copyright file="RequestStatusConvertor.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Windows.Data;
    using System.Windows.Media;
    using TunnelRelay.Windows.Engine;

    /// <summary>
    /// Decides the color of row in secret management.
    /// </summary>
    /// <seealso cref="IValueConverter" />
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
                if (requestData?.StatusCode.Equals("Active", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Brushes.Green;
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
        /// <exception cref="NotImplementedException">Convert back not allowed.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Convert back not allowed");
        }
    }
}

// <copyright file="RelayRequestExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Net.Http.Headers;

  /// <summary>
  /// Relay request extensions.
  /// </summary>
  internal static class RelayRequestExtensions
  {
    /// <summary>
    /// Converts header collection into UI format.
    /// </summary>
    /// <param name="headerCollection">Header collection.</param>
    /// <returns>Headers in UI format.</returns>
    public static List<HeaderDetails> GetHeaderMap(this WebHeaderCollection headerCollection)
    {
      List<HeaderDetails> headers = new List<HeaderDetails>();

      foreach (string headerName in headerCollection.AllKeys)
      {
        headers.Add(new HeaderDetails
        {
          HeaderName = headerName,
          HeaderValue = headerCollection[headerName],
        });
      }

      return headers;
    }

    /// <summary>
    /// Converts httpResponseHeaders collection into UI format.
    /// </summary>
    /// <param name="headerCollection">Header collection.</param>
    /// <returns>Headers in UI format.</returns>
    public static List<HeaderDetails> GetHeaderMap(this HttpResponseHeaders headerCollection)
    {
      List<HeaderDetails> headers = new List<HeaderDetails>();

      foreach (var headerName in headerCollection)
      {
        headers.Add(new HeaderDetails
        {
          HeaderName = headerName.Key,
          HeaderValue = string.Join(';', headerName.Value),
        });
      }

      return headers;
    }
  }
}

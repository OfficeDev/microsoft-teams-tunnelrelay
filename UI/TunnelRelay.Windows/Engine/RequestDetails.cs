// <copyright file="RequestDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using TunnelRelay.Core;

  /// <summary>
  /// Request Details for UI.
  /// </summary>
  public class RequestDetails : ICloneable
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestDetails"/> class.
    /// </summary>
    public RequestDetails()
    {
      this.ResponseHeaders = new List<HeaderDetails>();
      this.ReissueLog = new List<string>();
    }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string Method { get; set; }

#pragma warning disable CA1056 // Uri properties should not be strings - Serialized by XAML.
    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    public string Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings - Serialized by XAML.

    /// <summary>
    /// Gets or sets the status code.
    /// </summary>
    public string StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the request receive time.
    /// </summary>
    public DateTime RequestReceiveTime { get; set; }

    /// <summary>
    /// Gets or sets the request headers.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only - Data transfer object.
    public List<HeaderDetails> RequestHeaders { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only - Data transfer object.

    /// <summary>
    /// Gets or sets the request data.
    /// </summary>
    public string RequestData { get; set; }

    /// <summary>
    /// Gets or sets the response headers.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only - Data transfer object.
    public List<HeaderDetails> ResponseHeaders { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only - Data transfer object.

    /// <summary>
    /// Gets or sets the response data.
    /// </summary>
    public string ResponseData { get; set; }

    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public string Duration { get; set; }

    /// <summary>
    /// Gets or sets Original RelayRequest from where the RequestDetails were captured.
    /// </summary>
    public RelayRequest RelayRequest { get; set; }

    /// <summary>
    /// Gets or sets the response headers.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only - Data transfer object.
    public List<string> ReissueLog { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only - Data transfer object.

    /// <summary>
    /// Tracks if this request has been reissued, which must use a different request Id.
    /// </summary>
    /// <param name="requestId">id of the request - which default to a NewGuid.</param>
    public void AddToReissueLog(string requestId)
    {
      if (string.IsNullOrWhiteSpace(requestId))
      {
        throw new ArgumentNullException($"{nameof(requestId)} cannot be null");
      }

      var testForDuplicate = this.ReissueLog.Any(pId => pId == requestId);
      if (testForDuplicate == false)
      {
        this.ReissueLog.Add(requestId);
      }
    }

    /// <summary>
    /// Clones the current instance.
    /// </summary>
    /// <returns>Cloned instance.</returns>
    public object Clone()
    {
      var cloneResponseHeaders = new List<HeaderDetails>();
      cloneResponseHeaders.AddRange(this.ResponseHeaders.Select(pHeader => new HeaderDetails()
      {
        HeaderName = pHeader.HeaderName,
        HeaderValue = pHeader.HeaderValue,
      }));

      return new RequestDetails()
      {
        Duration = this.Duration,
        Method = this.Method,
        ReissueLog = new List<string>(),
        RelayRequest = this.RelayRequest.Clone() as RelayRequest,
        RequestData = this.RequestData,
        RequestHeaders = cloneResponseHeaders,
        RequestReceiveTime = this.RequestReceiveTime,
        ResponseData = this.ResponseData,
        ResponseHeaders = this.ResponseHeaders,
        StatusCode = this.StatusCode,
        Url = this.Url,
      };
    }
  }
}

// <copyright file="RelayRequestManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Core
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Net;
  using System.Net.Http;
  using System.Net.Http.Headers;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;
  using TunnelRelay.PluginEngine;

  /// <summary>
  /// Handles the requests received over the relay.
  /// </summary>
  public sealed class RelayRequestManager : IRelayRequestManager, IDisposable
  {
    private readonly IEnumerable<ITunnelRelayPlugin> tunnelRelayPlugins;

    private readonly ILogger<RelayRequestManager> logger;

    private readonly IRelayRequestEventListener relayRequestEventListener;

    private readonly HttpClient httpClient;

    private string internalServiceUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayRequestManager"/> class.
    /// </summary>
    /// <param name="httpClient">Http client.</param>
    /// <param name="relayRequestManagerOptions">Relay request manager options.</param>
    /// <param name="tunnelRelayPlugins">Instances of the plugins to use.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="relayRequestEventListener">Optional relay request event listener instance.</param>
    public RelayRequestManager(
        HttpClient httpClient,
        IOptionsMonitor<RelayRequestManagerOptions> relayRequestManagerOptions,
        IEnumerable<ITunnelRelayPlugin> tunnelRelayPlugins,
        ILogger<RelayRequestManager> logger,
        IRelayRequestEventListener relayRequestEventListener = null)
    {
      this.httpClient = httpClient;
      this.tunnelRelayPlugins = tunnelRelayPlugins;
      this.logger = logger;
      this.relayRequestEventListener = relayRequestEventListener;

      this.UpdateSettings(relayRequestManagerOptions?.CurrentValue);

      relayRequestManagerOptions.OnChange((newOptions) =>
      {
        this.UpdateSettings(newOptions);
      });
    }

    /// <summary>
    /// Handles the incoming request, passes it down to the internal service and returns the response.
    /// </summary>
    /// <param name="relayRequest">Incoming relay request.</param>
    /// <returns>Response from the internal service.</returns>
    public async Task<RelayResponse> HandleRelayRequestAsync(RelayRequest relayRequest)
    {
      if (relayRequest is null)
      {
        throw new ArgumentNullException(nameof(relayRequest));
      }

      string requestId = Guid.NewGuid().ToString();

      this.logger.LogTrace("Received request with Id '{0}'", requestId);

      // Inform listener that a request has been received.
      if (this.relayRequestEventListener != null)
      {
        RelayRequest clonedRequest = relayRequest.Clone() as RelayRequest;
#pragma warning disable CS4014 // We want eventing pipeline to run in parallel and not block the actual request execution.
        Task.Run(async () =>
        {
          try
          {
            await this.relayRequestEventListener.RequestReceivedAsync(requestId, clonedRequest).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            // Ignoring exceptions from listeners.
            this.logger.LogWarning("Relay request event listener failed for request Id '{0}' during 'RequestReceivedAsync' phase with exception '{1}'. Ignoring.", requestId, ex);
          }
        });
#pragma warning restore CS4014 // We want eventing pipeline to run in parallel and not block the actual request execution.
      }

      // rfr:extracted method - to just handle a RelayedRequest - which I want to create from this list box (?)
      HttpResponseMessage httpResponseMessage = await this.RedirectRequestFromManagerAsync(relayRequest, requestId).ConfigureAwait(false);

      RelayResponse relayResponse = null;

      try
      {
        relayResponse = await this.ToRelayResponseAsync(httpResponseMessage).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        this.logger.LogError("Failed to convert outgoing response into relay response for request '{0}'. Error '{1}'", requestId, ex);
      }

      if (this.relayRequestEventListener != null)
      {
        RelayResponse clonedResponse = relayResponse.Clone() as RelayResponse;
#pragma warning disable CS4014 // We want eventing pipeline to run in parallel and not slowdown the actual request execution.
        Task.Run(async () =>
        {
          try
          {
            await this.relayRequestEventListener.ResponseSentAsync(requestId, clonedResponse).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            // Ignoring exceptions from listeners.
            this.logger.LogWarning("Relay request event listener failed for request Id '{0}' during 'ResponseSentAsync' phase with exception '{1}'. Ignoring.", requestId, ex);
          }
        });
#pragma warning restore CS4014 // We want eventing pipeline to run in parallel and not slowdown the actual request execution.
      }

      return relayResponse;
    }

    /// <summary>
    /// Dispose the instance.
    /// </summary>
    public void Dispose()
    {
      this.httpClient.Dispose();
    }

    /// <summary>
    /// Allows for re-issueing of a previously captured request.
    /// </summary>
    /// <param name="relayRequest"> relayrequest which was captured previously.</param>
    /// <param name="requestId">the id associated with the relay request when it was relayed by the Service Bus Relay.</param>
    /// <returns>HttpResponseMessage from the re-issued request.</returns>
    public async Task<HttpResponseMessage> RedirectRequestFromManagerAsync(RelayRequest relayRequest, string requestId)
    {
      HttpRequestMessage httpRequestMessage = null;

      relayRequest = relayRequest ?? throw new ArgumentNullException($"{nameof(relayRequest)} cannot be null");
      if (string.IsNullOrWhiteSpace(requestId))
      {
        throw new ArgumentNullException($"{nameof(requestId)}");
      }

      try
      {
        httpRequestMessage = await this.ToHttpRequestMessageAsync(relayRequest, requestId).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        this.logger.LogError("CRITICAL ERROR!!! Failed to convert Relay request into outgoing request. Error - '{0}'. Request Id - '{1}'", ex, requestId);
        throw;
      }

      // Pass the request through all the plugins.
      foreach (ITunnelRelayPlugin tunnelRelayPlugin in this.tunnelRelayPlugins)
      {
        try
        {
          httpRequestMessage = await tunnelRelayPlugin.PreProcessRequestToServiceAsync(httpRequestMessage).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          this.logger.LogError("CRITICAL ERROR!!! Plugin '{0}' failed with error - '{1}' for request Id '{2}' during 'PreProcessRequestToServiceAsync' phase", tunnelRelayPlugin.PluginName, ex, requestId);
          throw;
        }
      }

      HttpResponseMessage httpResponseMessage;

      try
      {
        this.logger.LogTrace("Making external request to server for request Id '{0}'", requestId);
        httpResponseMessage = await this.httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
        this.logger.LogTrace("Received response from server for request Id '{0}'", requestId);
      }
      catch (HttpRequestException httpException)
      {
        this.logger.LogError("Hit exception while sending request to server for request Id '{0}'. Error '{1}'", requestId, httpException);
        httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
          Content = new StringContent(httpException.ToString()),
        };
      }
      catch (Exception ex)
      {
        this.logger.LogError("Hit critical exception while processing request '{0}'. Error '{1}'", requestId, ex);
        throw;
      }

      // Pass the response through all the plugins.
      foreach (ITunnelRelayPlugin tunnelRelayPlugin in this.tunnelRelayPlugins)
      {
        try
        {
          httpResponseMessage = await tunnelRelayPlugin.PostProcessResponseFromServiceAsync(httpResponseMessage).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          this.logger.LogError("CRITICAL ERROR!!! Plugin '{0}' failed with error - '{1}' for request Id '{2}' during 'PostProcessResponseFromServiceAsync' phase", tunnelRelayPlugin.PluginName, ex, requestId);
          throw;
        }
      }

      return httpResponseMessage;
    }

    /// <summary>
    /// Copies content headers.
    /// </summary>
    /// <param name="httpContent">Http content.</param>
    /// <param name="headerCollection">Header collection.</param>
    private static void CopyContentHeader(HttpContent httpContent, WebHeaderCollection headerCollection)
    {
      if (headerCollection["Content-Disposition"] != null)
      {
        httpContent.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(headerCollection["Content-Disposition"]);
      }

      if (headerCollection[HttpRequestHeader.ContentLocation] != null)
      {
        httpContent.Headers.ContentLocation = new Uri(headerCollection[HttpRequestHeader.ContentLocation]);
      }

      if (headerCollection[HttpRequestHeader.ContentRange] != null)
      {
        httpContent.Headers.ContentRange = ContentRangeHeaderValue.Parse(headerCollection[HttpRequestHeader.ContentRange]);
      }

      if (headerCollection[HttpRequestHeader.ContentType] != null)
      {
        httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse(headerCollection[HttpRequestHeader.ContentType]);
      }

      if (headerCollection[HttpRequestHeader.Expires] != null)
      {
        httpContent.Headers.Expires = DateTimeOffset.Parse(headerCollection[HttpRequestHeader.Expires], CultureInfo.InvariantCulture);
      }

      if (headerCollection[HttpRequestHeader.LastModified] != null)
      {
        httpContent.Headers.LastModified = DateTimeOffset.Parse(headerCollection[HttpRequestHeader.LastModified], CultureInfo.InvariantCulture);
      }

      if (headerCollection[HttpRequestHeader.ContentLength] != null)
      {
        httpContent.Headers.ContentLength = long.Parse(headerCollection[HttpRequestHeader.ContentLength], CultureInfo.InvariantCulture);
      }
    }

    /// <summary>
    /// Converts <see cref="RelayRequest"/> to <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="relayRequest">Incoming relay request.</param>
    /// <param name="requestId">Request Id.</param>
    /// <returns>Http request message.</returns>
    private async Task<HttpRequestMessage> ToHttpRequestMessageAsync(RelayRequest relayRequest, string requestId)
    {
      Uri internalRequestUrl = new Uri(this.internalServiceUrl + "/" + relayRequest.RequestPathAndQuery.TrimStart('/'));

      HttpRequestMessage httpRequestMessage = new HttpRequestMessage(relayRequest.HttpMethod, internalRequestUrl);

      // Prepare request content.
      // Caveat here! .NetFX uses WebRequest behind HttpClient so it does not allow body content in GET requests
      // but NetCore uses an entirely different stack and thus allows body in GET.
      // Once ADAL Library starts allowing UI based auth in NetCore we can get rid of NetFX altogether and allow users
      // to pass request body in GET too.
      if (relayRequest.InputStream != null && relayRequest.HttpMethod != HttpMethod.Get && relayRequest.HttpMethod != HttpMethod.Head)
      {
        MemoryStream memoryStream = new MemoryStream((int)relayRequest.InputStream.Length);
        await relayRequest.InputStream.CopyToAsync(memoryStream).ConfigureAwait(false);
        httpRequestMessage.Content = new StreamContent(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        httpRequestMessage.Content.Headers.ContentLength = memoryStream.Length;
        RelayRequestManager.CopyContentHeader(httpRequestMessage.Content, relayRequest.Headers);

        if (httpRequestMessage.Content.Headers.ContentLength != memoryStream.Length)
        {
          this.logger.LogWarning(
              "Content-Length header mismatch for request Id '{0}'. Value from request '{1}'. Value from actual content stream '{2}'",
              requestId,
              httpRequestMessage.Content.Headers.ContentLength.GetValueOrDefault(),
              memoryStream.Length);
        }
      }

      // Try to blindly add the headers. Content headers will get filtered out here.
      foreach (string headerName in relayRequest.Headers.Keys)
      {
        httpRequestMessage.Headers.TryAddWithoutValidation(headerName, relayRequest.Headers[headerName]);
      }

      // Set the correct host header.
      httpRequestMessage.Headers.Host = internalRequestUrl.Host;

      return httpRequestMessage;
    }

    /// <summary>
    /// Converts <see cref="HttpResponseMessage"/> to <see cref="RelayResponse"/>.
    /// </summary>
    /// <param name="httpResponseMessage">Response message.</param>
    /// <returns>Relay response.</returns>
    private async Task<RelayResponse> ToRelayResponseAsync(HttpResponseMessage httpResponseMessage)
    {
      RelayResponse relayResponse = new RelayResponse
      {
        HttpStatusCode = httpResponseMessage.StatusCode,
        StatusDescription = httpResponseMessage.ReasonPhrase,
        Headers = new WebHeaderCollection(),
        RequestEndDateTime = DateTimeOffset.Now,
      };

      // Copy the response headers.
      foreach (KeyValuePair<string, IEnumerable<string>> responseHeader in httpResponseMessage.Headers)
      {
        relayResponse.Headers.Add(responseHeader.Key, string.Join(",", responseHeader.Value));
      }

      if (httpResponseMessage.Content != null)
      {
        foreach (KeyValuePair<string, IEnumerable<string>> responseHeader in httpResponseMessage.Content.Headers)
        {
          relayResponse.Headers.Add(responseHeader.Key, string.Join(",", responseHeader.Value));
        }

        relayResponse.OutputStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
      }

      return relayResponse;
    }

    /// <summary>
    /// Updates the settings for current instance.
    /// </summary>
    /// <param name="relayRequestManagerOptions">Relay request manager options.</param>
    private void UpdateSettings(RelayRequestManagerOptions relayRequestManagerOptions)
    {
      if (relayRequestManagerOptions == null)
      {
        throw new ArgumentNullException(nameof(relayRequestManagerOptions));
      }

      if (relayRequestManagerOptions.InternalServiceUrl == null)
      {
        throw new ArgumentNullException(nameof(relayRequestManagerOptions), "Internal service url can't be null or empty.");
      }

      this.internalServiceUrl = relayRequestManagerOptions.InternalServiceUrl.AbsoluteUri.TrimEnd('/');
    }
  }
}

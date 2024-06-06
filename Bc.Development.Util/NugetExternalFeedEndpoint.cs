using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Bc.Development.Util
{
  /// <summary>
  /// Represents a Nuget external feed endpoint.
  /// </summary>
  public class NugetExternalFeedEndpoint
  {
    /// <summary>
    /// Load the endpoint configuration for the given URI.
    /// </summary>
    /// <param name="uri">The URI to load the configuration for.</param>
    /// <param name="authString">
    /// The JSON string containing the endpoint configuration. If this is not specified, the value of the
    /// VSS_NUGET_EXTERNAL_FEED_ENDPOINTS environment variable will be used.
    /// </param>
    /// <returns>The endpoint configuration for the given URI.</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public static NugetExternalFeedEndpoint LoadForUri(string uri, string authString = null)
    {
      var all = Load(authString);
      var item = all.FirstOrDefault(e => uri.ToLowerInvariant().StartsWith(e.Endpoint.ToLowerInvariant()));
      return item ?? throw new KeyNotFoundException($"No endpoint configuration found for URI '{uri}'.");
    }

    /// <summary>
    /// Loads all endpoint configurations from the given JSON string.
    /// </summary>
    /// <param name="authString">
    /// The JSON string containing the endpoint configuration. If this is not specified, the value of the
    /// VSS_NUGET_EXTERNAL_FEED_ENDPOINTS environment variable will be used.
    /// </param>
    /// <returns></returns>
    public static NugetExternalFeedEndpoint[] Load(string authString = null)
    {
      if (authString == null)
        authString = Environment.GetEnvironmentVariable("VSS_NUGET_EXTERNAL_FEED_ENDPOINTS");
      try
      {
        var ja = JObject.Parse(authString);
        var endpoints = new List<NugetExternalFeedEndpoint>();
        foreach (var endpoint in ja["endpointCredentials"])
        {
          var endpointUri = endpoint.Value<string>("endpoint");
          var username = endpoint.Value<string>("username");
          var password = endpoint.Value<string>("password");
          var creds = !string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password)
            ? new NetworkCredential(username, password)
            : null;
          endpoints.Add(new NugetExternalFeedEndpoint(endpointUri, creds));
        }

        return endpoints.ToArray();
      }
      catch (Exception ex)
      {
        throw new FormatException("The provided JSON string is not a valid Nuget endpoint configuration string.", ex);
      }
    }

    /// <summary>
    /// The endpoint URI.
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// The credentials for the endpoint.
    /// </summary>
    public NetworkCredential Credential { get; }


    private NugetExternalFeedEndpoint(string endpoint, NetworkCredential cred)
    {
      Endpoint = endpoint;
      Credential = cred;
    }
  }
}
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Bc.Development.DevOps
{
  internal static class Helpers
  {
    private const string VssAuthorizationEndpoint = "X-VSS-AuthorizationEndpoint";

    public static async Task<Uri> GetAuthorizationEndpoint(this Uri uri)
    {
      var headers = await GetResponseHeadersAsync(uri);
      foreach (var endpoint in headers.GetValues(VssAuthorizationEndpoint))
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var parsedEndpoint))
          return parsedEndpoint;
      return null;
    }

    private static async Task<HttpResponseHeaders> GetResponseHeadersAsync(Uri uri)
    {
      using (var httpClient = new HttpClient())
      using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
      {
        using (var response = await httpClient.SendAsync(request))
        {
          return response.Headers;
        }
      }
    }

    public static async Task<Uri> GetAadAuthorityUriAsync(this Uri uri)
    {
      var headers = await GetResponseHeadersAsync(uri);
      var bearerHeaders = headers.WwwAuthenticate.Where(x => x.Scheme.Equals("Bearer", StringComparison.Ordinal));

      foreach (var param in bearerHeaders)
      {
        if (param.Parameter == null)
        {
          // MSA-backed accounts don't expose a parameter
          continue;
        }

        var equalSplit = param.Parameter.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
        if (equalSplit.Length == 2)
        {
          if (equalSplit[0].Equals("authorization_uri", StringComparison.OrdinalIgnoreCase))
          {
            if (Uri.TryCreate(equalSplit[1], UriKind.Absolute, out var parsedUri))
              return parsedUri;
          }
        }
      }

      // Return the common tenant
      const string aadBase = "https://login.microsoftonline.com";
      const string tenant = "organizations";
      return new Uri($"{aadBase}/{tenant}");
    }
  }
}
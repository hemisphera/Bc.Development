using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bc.Development.DevOps.Caching;
using Newtonsoft.Json;

namespace Bc.Development.DevOps
{
  /// <summary>
  /// Provides a client for creating VSTS session tokens from an access token.
  /// </summary>
  public class VstsSessionTokenClient
  {
    private const string DefaultTokenScope = "vso.packaging_write vso.packaging_manage";

    private readonly IVstsSessionTokenCache _itemCache;

    private readonly SemaphoreSlim _itemCacheLock = new SemaphoreSlim(1, 1);

    private Uri VstsUri { get; }

    /// <summary>
    /// The scope for which to create tokens.
    /// </summary>
    public string Scope { get; }


    /// <summary>
    /// </summary>
    /// <param name="vstsUri">The URI to the Azure DevOps organization.</param>
    /// <param name="tokenCache">An optional session token cache to use for caching generated session tokens.</param>
    /// <param name="scope">The scopes for which to get the session token.</param>
    /// 
    public VstsSessionTokenClient(Uri vstsUri, IVstsSessionTokenCache tokenCache = null, string scope = null)
    {
      VstsUri = vstsUri;
      Scope = String.IsNullOrEmpty(scope) ? DefaultTokenScope : scope;
      _itemCache = tokenCache ?? new MemoryVstsSessionTokenCache();
    }


    private HttpRequestMessage CreateRequest(Uri uri, string accessToken, DateTime? validTo = null)
    {
      var request = new HttpRequestMessage(HttpMethod.Post, uri);
      request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

      var tokenRequest = new VstsSessionToken
      {
        DisplayName = "Feed Access",
        Scope = Scope,
        ValidTo = validTo
      };

      request.Content = new StringContent(
        JsonConvert.SerializeObject(tokenRequest),
        Encoding.UTF8,
        "application/json");

      return request;
    }


    /// <summary>
    /// Creates a new session token (or returns a cached token) for the specified access token.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="validTo">The token validity.</param>
    /// <returns></returns>
    public async Task<VstsSessionToken> GetSessionToken(
      string accessToken,
      DateTime? validTo = null)
    {
      try
      {
        await _itemCacheLock.WaitAsync();
        var hash = GetAccessTokenHash(accessToken);
        _itemCache.Prune();
        var foundItem = _itemCache.Get(hash);
        if (foundItem != null) return foundItem;
        foundItem = await CreateSessionTokenInternal(accessToken, VstsTokenType.Compact, validTo);
        _itemCache.Add(hash, foundItem);
        return foundItem;
      }
      finally
      {
        _itemCacheLock.Release();
      }
    }

    private static string GetAccessTokenHash(string accessToken)
    {
      var sha1 = new SHA1Managed();
      var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(accessToken));
      return String.Join("", bytes.Select(b => b.ToString("x2")));
    }

    private async Task<VstsSessionToken> CreateSessionTokenInternal(
      string accessToken,
      VstsTokenType tokenType, DateTime? validTo = null)
    {
      var endTime = validTo ?? DateTime.UtcNow + TimeSpan.FromHours(2);

      var spsEndpoint = await Helpers.GetAuthorizationEndpoint(VstsUri);
      if (spsEndpoint == null)
        return null;

      var uriBuilder = new UriBuilder(spsEndpoint)
      {
        Query = $"tokenType={tokenType}&api-version=5.0-preview.1"
      };

      uriBuilder.Path = uriBuilder.Path.TrimEnd('/') + "/_apis/Token/SessionTokens"; // undocumented?

      foreach (var t in new DateTime?[] { endTime, null })
      {
        using (var request = CreateRequest(uriBuilder.Uri, accessToken, endTime))
        using (var client = new HttpClient())
        using (var response = await client.SendAsync(request))
        {
          if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
          {
            continue;
          }

          response.EnsureSuccessStatusCode();
          var serializedResponse = await response.Content.ReadAsStringAsync();

          try
          {
            return JsonConvert.DeserializeObject<VstsSessionToken>(serializedResponse);
          }
          catch (Exception ex)
          {
            throw new Exception($"Error deserializing VSTS session token response HTTP '{response.StatusCode}'. Authentication failed?", ex);
          }
        }
      }

      throw new InvalidOperationException();
    }
  }
}
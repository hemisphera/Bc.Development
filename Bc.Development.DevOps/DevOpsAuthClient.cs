using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Bc.Development.DevOps
{
  /// <summary>
  /// Azure DevOps authentication client.
  /// </summary>
  public class DevOpsAuthClient : IDisposable
  {
    private const string Resource = "499b84ac-1321-427f-aa17-267ca6975798/.default";

    private const string ClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";


    private readonly SemaphoreSlim _cacheHelperLock = new SemaphoreSlim(1, 1);

    private MsalCacheHelper _cacheHelper;

    private readonly HttpClient _client;

    private readonly string _tokenCachePath;

    private readonly bool _clientDisposable;


    /// <summary>
    /// </summary>
    /// <param name="client">A HTTP Client to use for makeing requests.</param>
    /// <param name="tokenCachePath">An optional path to a file where to cache tokens.</param>
    public DevOpsAuthClient(HttpClient client, string tokenCachePath = null)
    {
      _client = client;
      _tokenCachePath = tokenCachePath;
    }

    /// <summary>
    /// </summary>
    /// <param name="tokenCachePath">An optional path to a file where to cache tokens.</param>
    public DevOpsAuthClient(string tokenCachePath = null)
      : this(new HttpClient(), tokenCachePath)
    {
      _clientDisposable = true;
    }


    private async Task<IPublicClientApplication> CreatePca(Uri endpointUri, string tokenCachePath = null)
    {
      var authority = await endpointUri.GetAadAuthorityUriAsync();
      var helper = await GetMsalCacheHelperAsync(tokenCachePath);
      var client = PublicClientApplicationBuilder
        .Create(ClientId)
        .WithAuthority(authority)
        .WithRedirectUri("http://localhost")
        .Build();
      helper?.RegisterCache(client.UserTokenCache);
      return client;
    }

    private async Task<MsalCacheHelper> GetMsalCacheHelperAsync(string tokenCachePath = null)
    {
      if (String.IsNullOrEmpty(tokenCachePath)) return null;

      if (Directory.Exists(tokenCachePath))
      {
        tokenCachePath = Path.Combine(tokenCachePath, ".tokencache");
      }

      // There are options to set up the cache correctly using StorageCreationProperties on other OS's but that will need to be tested
      // for now only support windows
      await _cacheHelperLock.WaitAsync();
      try
      {
        if (_cacheHelper == null)
        {
          var tokenCacheFolder = Path.GetDirectoryName(tokenCachePath);
          var tokenCacheFilename = Path.GetFileName(tokenCachePath);
          Directory.CreateDirectory(tokenCacheFolder);
          var builder = new StorageCreationPropertiesBuilder(tokenCacheFilename, tokenCacheFolder)
            .WithCacheChangedEvent(ClientId);
          var creationProps = builder.Build();
          _cacheHelper = await MsalCacheHelper.CreateAsync(creationProps);
        }

        return _cacheHelper;
      }
      finally
      {
        _cacheHelperLock.Release();
      }
    }


    /// <summary>
    /// Interactively acquires an access token for an Azure DevOps organization.
    /// </summary>
    /// <param name="endpointUri">The endpoint for which to get the token.</param>
    /// <returns>The access token.</returns>
    public async Task<string> AcquireTokenWithUi(Uri endpointUri)
    {
      var scopes = new[] { Resource };
      var publicClient = await CreatePca(endpointUri, _tokenCachePath);

      var acc = await publicClient.GetAccountsAsync();
      try
      {
        var token = await publicClient
          .AcquireTokenSilent(scopes, acc.FirstOrDefault())
          .ExecuteAsync();
        return token.AccessToken;
      }
      catch (MsalUiRequiredException)
      {
        // do nothing
      }

      try
      {
        var token = await publicClient
          .AcquireTokenInteractive(scopes)
          .WithPrompt(Prompt.SelectAccount)
          .WithUseEmbeddedWebView(false)
          .ExecuteAsync();
        return token.AccessToken;
      }
      catch (MsalServiceException e)
      {
        if (e.ErrorCode.Contains(MsalError.AuthenticationCanceledError))
          return null;
        throw;
      }
    }


    /// <inheritdoc />
    public void Dispose()
    {
      _cacheHelperLock?.Dispose();
      if (_clientDisposable)
      {
        _client?.Dispose();
      }
    }
  }
}
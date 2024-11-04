using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;
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

    private Dictionary<Uri, IPublicClientApplication> Items { get; } = new Dictionary<Uri, IPublicClientApplication>();

    /// <summary>
    /// Specifies if the embedded web view should be used for interactive authentication.
    /// </summary>
    public bool UseEmbeddedWebView { get; set; } = false;


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

    /// <summary>
    /// Retrieves or creates a PCA for the given Uri.
    /// </summary>
    /// <param name="endpointUri">The endpoint URI</param>
    /// <param name="tokenCachePath">The Cache Folder Path</param>
    /// <returns></returns>
    public async Task<IPublicClientApplication> Get(Uri endpointUri, string tokenCachePath = null)
    {
      if (!Items.TryGetValue(endpointUri, out var pca))
      {
        pca = await CreatePca(endpointUri, tokenCachePath);
        Items.Add(endpointUri, pca);
      }
      return pca;
    }


    /// <summary>
    /// Returns the first account username, if any, in the cache for the given AAD tenant.
    /// </summary>
    /// <param name="endpointUri">The endpoint URI</param>
    /// <param name="tokenCachePath">The Cache Folder Path</param>
    /// <returns></returns>
    public async Task<IAccount> GetAccount(Uri endpointUri, string tokenCachePath = null)
    {
      if (!Items.TryGetValue(endpointUri, out var pca)) return null;
      var acc = await pca.GetAccountsAsync();
      return acc.FirstOrDefault();
    }


    private async Task<IPublicClientApplication> CreatePca(Uri endpointUri, string tokenCachePath = null)
    {
      var authority = await endpointUri.GetAadAuthorityUriAsync();
      var helper = await GetMsalCacheHelperAsync(tokenCachePath);
      var pcab = PublicClientApplicationBuilder
        .Create(ClientId)
        .WithAuthority(authority)
        .WithRedirectUri("http://localhost");
      if (UseEmbeddedWebView)
        pcab = pcab.WithWindowsEmbeddedBrowserSupport();

      var client = pcab.Build();
      helper?.RegisterCache(client.UserTokenCache);
      return client;
    }

    private async Task<MsalCacheHelper> GetMsalCacheHelperAsync(string tokenCachePath = null)
    {
      if (string.IsNullOrEmpty(tokenCachePath)) return null;

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
          if (!string.IsNullOrEmpty(tokenCacheFolder))
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
    /// Acquires an access token for an Azure DevOps organization.
    /// This will attempt to read the token from the cache first and if it fails, it will prompt the user for authentication (if allowed).
    /// If the user cancels the authentication prompt, this will return null.
    /// </summary>
    /// <param name="endpointUri">The endpoint for which to get the token.</param>
    /// <param name="allowUi">Specifies if interative authentication is allowed.</param>
    /// <returns>The access token.</returns>
    public async Task<string> AcquireToken(Uri endpointUri, bool allowUi = true)
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

      if (!allowUi) return null;

      try
      {
        var token = await publicClient
          .AcquireTokenInteractive(scopes)
          .WithPrompt(Prompt.SelectAccount)
          .WithUseEmbeddedWebView(UseEmbeddedWebView)
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

    /// <summary>
    /// Interactively acquires an access token for an Azure DevOps organization.
    /// </summary>
    /// <param name="endpointUri">The endpoint for which to get the token.</param>
    /// <returns>The access token.</returns>
    [Obsolete("Replaced by 'AcquireToken'")]
    public Task<string> AcquireTokenWithUi(Uri endpointUri)
    {
      return AcquireToken(endpointUri);
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
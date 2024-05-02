using System;
using System.Net;
using System.Threading.Tasks;

namespace Bc.Development.DevOps
{
  /// <summary>
  /// A provider for feed credentials.
  /// </summary>
  public class AuthenticationProvider
  {
    /// <summary>
    /// The URI to authenticate against.
    /// </summary>
    public Uri EndpointUri { get; }

    /// <summary>
    /// Specifies whether interactive authentication is allowed.
    /// </summary>
    public bool AllowUi { get; set; }

    /// <summary>
    /// Specifies if the embedded web view should be used for interactive authentication.
    /// </summary>
    public bool UseEmbeddedWebView { get; set; }

    /// <summary>
    /// Allows specifying a pre-configured credential.
    /// This has only effect if the authentication type is <see cref="DevOps.AuthenticationType.Credentials"/>.
    /// </summary>
    public NetworkCredential ConfiguredCredential { get; set; }

    /// <summary>
    /// Specifies the authentication type to use.
    /// </summary>
    public AuthenticationType AuthenticationType { get; set; }

    /// <summary>
    /// A callback that will be invoked to prompt the user for credentials.
    /// This will be used if 'Credentials' is set as the authentication type and no 'ConfiguredCredential' is provided.
    /// </summary>
    public Func<AuthenticationProvider, Task<CredentialResponse>> PromptCallback { get; set; }

    /// <summary>
    /// Specifies the full path to a token cache file. This will be used to cache OAuth access tokens.
    /// If this is not specified, tokens will not be cached.
    /// </summary>
    public string AccessTokenCachePath { get; set; }

    /// <summary>
    /// Specifies the session token cache to use for caching generated session tokens.
    /// If this is not specified, session tokens will not be cached. 
    /// </summary>
    public IVstsSessionTokenCache SessionTokenCache { get; set; }

    /// <summary>
    /// Specifies the token validity for new session tokens.
    /// </summary>
    public TimeSpan? SessionTokenValidity { get; set; }


    /// <summary>
    /// </summary>
    /// <param name="endpointUri"></param>
    /// <param name="authenticationType"></param>
    public AuthenticationProvider(Uri endpointUri, AuthenticationType authenticationType = AuthenticationType.None)
    {
      EndpointUri = endpointUri;
      AuthenticationType = authenticationType;
    }


    /// <summary>
    /// Retrieves credentials. 
    /// </summary>
    /// <returns>The credential response.</returns>
    public virtual async Task<CredentialResponse> GetCredentials()
    {
      if (AuthenticationType == AuthenticationType.DevOpsToken)
      {
        return CredentialResponse.Create(
          new NetworkCredential(
            "DevOpsToken",
            await GetDevOpsSystemAccessToken()
          ));
      }

      if (AuthenticationType == AuthenticationType.Msal || AuthenticationType == AuthenticationType.BearerToken)
      {
        var sessionTokenCredential = await AcquireSessionToken();
        return CredentialResponse.Create(sessionTokenCredential);
      }

      if (AuthenticationType == AuthenticationType.Credentials)
      {
        if (ConfiguredCredential != null) CredentialResponse.Create(ConfiguredCredential);
        if (PromptCallback != null)
          return await PromptCallback(this);
      }

      if (AuthenticationType == AuthenticationType.None)
        return CredentialResponse.Create(null);

      return CredentialResponse.CreateFailure();
    }

    private async Task<string> GetAccessToken()
    {
      if (AuthenticationType == AuthenticationType.BearerToken)
      {
        return ConfiguredCredential?.Password;
      }

      if (AuthenticationType == AuthenticationType.Msal)
      {
        var ah = new DevOpsAuthClient(AccessTokenCachePath)
        {
          UseEmbeddedWebView = UseEmbeddedWebView
        };
        return await ah.AcquireToken(EndpointUri, AllowUi);
      }

      return string.Empty;
    }

    private async Task<NetworkCredential> AcquireSessionToken()
    {
      var accessToken = await GetAccessToken();
      if (string.IsNullOrEmpty(accessToken)) return null;

      var sessionTokenClient = new VstsSessionTokenClient(EndpointUri, SessionTokenCache);
      var validity = SessionTokenValidity != null ? DateTime.Now.Add(SessionTokenValidity.Value) : (DateTime?)null;
      var sessionToken = await sessionTokenClient.GetSessionToken(accessToken, validity);
      return new NetworkCredential("vsts", sessionToken.Token);
    }

    private async Task<string> GetDevOpsSystemAccessToken()
    {
      // try to use the token that was specified as the password, if any
      var accessToken = ConfiguredCredential?.Password;
      if (!string.IsNullOrEmpty(accessToken)) return await Task.FromResult(accessToken);

      // otherwise try to read the system variable that is specified by "UserName", or the 'System.AccessToken' variable
      var variableName = "SYSTEM_ACCESSTOKEN";
      if (!string.IsNullOrEmpty(ConfiguredCredential?.UserName))
        variableName = ConfiguredCredential.UserName;
      accessToken = Environment.GetEnvironmentVariable(variableName);
      return await Task.FromResult(accessToken);
    }
  }
}
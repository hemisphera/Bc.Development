namespace Bc.Development.DevOps
{
  /// <summary>
  /// An enum specifying an authentication type.
  /// </summary>
  public enum AuthenticationType
  {
    /// <summary>
    /// The feed does not require authentication.
    /// </summary>
    None,

    /// <summary>
    /// The feed uses user and password. Unless provided as part of the configuration,
    /// the user is interactively queried.
    /// </summary>
    Credentials,

    /// <summary>
    /// The feed uses OAuth / MSAL to authenticate.
    /// This might be interactive, if no cached token is available.
    /// </summary>
    Msal,

    /// <summary>
    /// Use a bearer token to exchange it for a SessionToken.
    /// The bearer token should be provided as the password in the credential configuration.
    /// </summary>
    BearerToken,

    /// <summary>
    /// The provided credential is treated as the System.AccessToken available while running a build,
    /// and is used to generate a SessionToken.
    /// </summary>
    DevOpsToken
  }
}
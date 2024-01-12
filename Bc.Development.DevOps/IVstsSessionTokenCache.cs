namespace Bc.Development.DevOps
{
  /// <summary>
  /// Interface for providing caching of VSTS session tokens.
  /// </summary>
  public interface IVstsSessionTokenCache
  {
    /// <summary>
    /// Remove all expired tokens from the cache.
    /// </summary>
    void Prune();

    /// <summary>
    /// Get the session token from the cache.
    /// </summary>
    /// <param name="hash">The hashed access token for which to retrieve the session token.</param>
    /// <returns></returns>
    VstsSessionToken Get(string hash);

    /// <summary>
    /// Add a session token to the cache.
    /// </summary>
    /// <param name="hash">The hashed access token for which to add the session token.</param>
    /// <param name="token">The session token to store.</param>
    void Add(string hash, VstsSessionToken token);
  }
}
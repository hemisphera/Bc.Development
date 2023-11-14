using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// Provides access to the credentials cached locally in the AL language extension.
  /// </summary>
  public class CachedCredentialReader
  {
    /// <summary>
    /// The filename to the credential cache.
    /// </summary>
    public string Filename { get; }


    private CachedCredentialReader(string filename)
    {
      Filename = filename;
    }

    /// <summary>
    /// Creates a credential reader from a given cache file.
    /// </summary>
    /// <param name="filename">The full path to the credential cache file.</param>
    /// <returns>The credential reader.</returns>
    public static CachedCredentialReader Get(string filename)
    {
      return new CachedCredentialReader(filename);
    }

    /// <summary>
    /// Creates a credential reader from the username/password cache file of a given folder.
    /// </summary>
    /// <param name="folder">The full path to folder of the AL language extension.</param>
    /// <returns>The credential reader.</returns>
    public static CachedCredentialReader GetUserPasswordCache(string folder)
    {
      const string filename = "UserPasswordCache.dat";
      var fullPath = Path.Combine(folder, filename);
      return !File.Exists(fullPath) ? null : new CachedCredentialReader(filename);
    }

    /// <summary>
    /// Creates a credential reader from the server info cache file of a given folder.
    /// </summary>
    /// <param name="folder">The full path to folder of the AL language extension.</param>
    /// <returns>The credential reader.</returns>
    public static CachedCredentialReader GetServerInfoCache(string folder)
    {
      const string filename = "ServerInfoCache.dat";
      var fullPath = Path.Combine(folder, filename);
      return !File.Exists(fullPath) ? null : new CachedCredentialReader(filename);
    }

    /// <summary>
    /// Creates a credential reader from the token cache file of a given folder.
    /// </summary>
    /// <param name="folder">The full path to folder of the AL language extension.</param>
    /// <returns>The credential reader.</returns>
    public static CachedCredentialReader GetTokenKeyCache(string folder)
    {
      const string filename = "TokenKeyCache.dat";
      var fullPath = Path.Combine(folder, filename);
      return !File.Exists(fullPath) ? null : new CachedCredentialReader(filename);
    }


    private static Uri NormalizeServerUri(Uri serverUri)
    {
      return new UriBuilder
      {
        Scheme = serverUri.Scheme,
        Host = serverUri.Host
      }.Uri;
    }

    /// <summary>
    /// Creates a credential key for a given server uri and server instance.
    /// </summary>
    /// <param name="serverUri">The server URI.</param>
    /// <param name="serverInstance">The server instance.</param>
    /// <returns>The credential key.</returns>
    public static string GetUserPasswordCredentialKey(Uri serverUri, string serverInstance)
    {
      return NormalizeServerUri(serverUri).ToString().ToLowerInvariant() + "_" + serverInstance.ToLowerInvariant();
    }

    /// <summary>
    /// Reads the stored credential for a given key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The credential or null if not found.</returns>
    public NetworkCredential TryGetCredential(string key)
    {
      var dictionary = new UserProtectedFileStorage(Filename).Read<Dictionary<string, UsernamePassword>>();
      if (dictionary == null)
        return null;
      return dictionary.TryGetValue(key, out var savedCredentials) ? savedCredentials.AsNetworkCredential() : null;
    }

    /// <summary>
    /// List all keys in the credential cache.
    /// </summary>
    /// <returns>The keys.</returns>
    public IEnumerable<string> ListKeys()
    {
      var dictionary = new UserProtectedFileStorage(Filename).Read<Dictionary<string, UsernamePassword>>();
      return dictionary.Keys;
    }
  }
}
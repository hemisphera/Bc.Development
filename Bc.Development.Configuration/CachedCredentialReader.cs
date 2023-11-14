using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
      return !File.Exists(fullPath) ? null : new CachedCredentialReader(fullPath);
    }


    /// <summary>
    /// Returns the cached credential for the given server and servcer instance from any found AL language extension.
    /// </summary>
    /// <param name="serverUri">The server URI.</param>
    /// <param name="serverInstance">The server instance.</param>
    /// <returns>The found credential, or null if none was found.</returns>
    public static NetworkCredential GetUserPasswordCredential(Uri serverUri, string serverInstance)
    {
      var extensions = AlLanguageExtension.Enumerate();
      foreach (var extension in extensions.OrderByDescending(e => e.FileVersion))
      {
        var credential = GetUserPasswordCredential(serverUri, serverInstance, extension.Folder.FullName);
        if (credential != null) return credential;
      }

      return null;
    }

    /// <summary>
    /// Returns the cached credential for the given server and servcer instance from the given AL language extension.
    /// </summary>
    /// <param name="serverUri">The server URI.</param>
    /// <param name="serverInstance">The server instance.</param>
    /// <param name="alLanguageFolder">The folder of the AL language extension.</param>
    /// <returns>The found credential, or null if none was found.</returns>
    public static NetworkCredential GetUserPasswordCredential(Uri serverUri, string serverInstance, string alLanguageFolder)
    {
      var key = GetUserPasswordCredentialKey(serverUri, serverInstance);
      var cache = GetUserPasswordCache(alLanguageFolder);
      return cache?.TryGetCredential(key);
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
      return !File.Exists(fullPath) ? null : new CachedCredentialReader(fullPath);
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
      return !File.Exists(fullPath) ? null : new CachedCredentialReader(fullPath);
    }

    /// <summary>
    /// Creates a credential key for a given server uri and server instance.
    /// </summary>
    /// <param name="serverUri">The server URI.</param>
    /// <param name="serverInstance">The server instance.</param>
    /// <returns>The credential key.</returns>
    public static string GetUserPasswordCredentialKey(Uri serverUri, string serverInstance)
    {
      return serverUri.NormalizeBcServerUri().ToString().ToLowerInvariant() + "_" + serverInstance.ToLowerInvariant();
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
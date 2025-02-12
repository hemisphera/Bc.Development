using System;
using System.Linq;
using Bc.Development.Util;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// The key of an AL credential cache entry for user password.
  /// </summary>
  public readonly struct AlUserPasswordCacheKey : IEquatable<AlUserPasswordCacheKey>
  {
    /// <summary>
    /// The URI of the server.
    /// </summary>
    public Uri ServerUri { get; }

    /// <summary>
    /// The server instance.
    /// </summary>
    public string ServerInstance { get; }

    /// <summary>
    /// The credential cache key.
    /// </summary>
    public string Key { get; }


    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    public AlUserPasswordCacheKey(string key)
    {
      Key = key;
      var parts = key.Split('_');
      ServerInstance = parts[parts.Length - 1];
      ServerUri = new Uri(string.Join("_", parts.Take(parts.Length - 1)));
    }

    /// <summary>
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="serverInstance"></param>
    public AlUserPasswordCacheKey(Uri uri, string serverInstance)
    {
      ServerUri = uri;
      ServerInstance = serverInstance;
      Key = GetKey(uri, serverInstance);
    }


    private static string GetKey(Uri uri, string serverInstance)
    {
      var serverPart = uri.NormalizeBcServerUri().ToString().ToLowerInvariant().TrimEnd('/');
      return serverPart + "_" + serverInstance.ToLowerInvariant();
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return Key;
    }

    /// <inheritdoc />
    public bool Equals(AlUserPasswordCacheKey other)
    {
      return Key == other.Key;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is AlUserPasswordCacheKey other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return Key != null ? Key.GetHashCode() : 0;
    }


    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static implicit operator AlUserPasswordCacheKey(string key)
    {
      return new AlUserPasswordCacheKey(key);
    }
  }
}
using System.Collections.Generic;
using System.Net;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// Extension methods.
  /// </summary>
  public static class Extensions
  {
    /// <summary>
    /// Returns the first user password credential found in any of the local caches.
    /// </summary>
    /// <param name="caches">The caches to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>The found entry, or null if the entry was not found.</returns>
    public static NetworkCredential? GetUserPasswordCredential(this IEnumerable<AlUserPasswordCache> caches, AlUserPasswordCacheKey key)
    {
      foreach (var cache in caches)
      {
        var credential = cache.Get(key);
        if (credential != null) return credential;
      }

      return null;
    }
  }
}
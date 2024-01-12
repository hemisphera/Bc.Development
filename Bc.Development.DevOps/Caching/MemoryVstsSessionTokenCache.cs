using System;
using System.Collections.Generic;
using System.Linq;

namespace Bc.Development.DevOps.Caching
{
  /// <summary>
  /// VSTS session token cache with in-memory storage.
  /// </summary>
  public class MemoryVstsSessionTokenCache : IVstsSessionTokenCache
  {
    private readonly Dictionary<string, VstsSessionToken> _cache = new Dictionary<string, VstsSessionToken>();

    /// <inheritdoc />
    public void Prune()
    {
      var maxTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(10));
      var expiredKeys = _cache
        .Where(kvp => kvp.Value.ValidTo < maxTime)
        .Select(kvp => kvp.Key).ToList();
      foreach (var expiredKey in expiredKeys)
      {
        _cache.Remove(expiredKey);
      }
    }

    /// <inheritdoc />
    public VstsSessionToken Get(string hash)
    {
      return _cache.TryGetValue(hash, out var token) ? token : null;
    }

    /// <inheritdoc />
    public void Add(string hash, VstsSessionToken token)
    {
      _cache[hash] = token;
    }
  }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Bc.Development.DevOps.Caching
{
  /// <summary>
  /// Cache for VSTS session tokens with secure file storage.
  /// </summary>
  public class SecureFileSessionTokenCache : IVstsSessionTokenCache
  {
    private readonly string _path;

    private object _syncRoot = new object();

    private readonly IDataProtector _dataProtector;


    /// <summary>
    /// </summary>
    /// <param name="path">The path to the cache file.</param>
    public SecureFileSessionTokenCache(string path)
    {
      _path = path;
      var fi = new FileInfo(path);
      if (!fi.Directory.Exists) fi.Directory.Create();
      var provider = DataProtectionProvider.Create("D7F6C090-198D-4550-BA27-5C7AB91F3327");
      _dataProtector = provider.CreateProtector(new[]
      {
        "1B7161D8-1265-4602-B0EE-F0B4D9EF0046",
        fi.FullName
      });
    }

    private Dictionary<string, VstsSessionToken> ReadFile()
    {
      lock (_syncRoot)
        try
        {
          var fileContent = Encoding.UTF8.GetString(_dataProtector.Unprotect(File.ReadAllBytes(_path)));
          return JsonConvert.DeserializeObject<Dictionary<string, VstsSessionToken>>(fileContent);
        }
        catch
        {
          return new Dictionary<string, VstsSessionToken>();
        }
    }

    private void WriteFile(IDictionary<string, VstsSessionToken> cache)
    {
      lock (_syncRoot)
      {
        var fileContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cache));
        File.WriteAllBytes(_path, _dataProtector.Protect(fileContent));
      }
    }

    /// <inheritdoc />
    public void Prune()
    {
      var cache = ReadFile();

      var maxTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(10));
      var expiredKeys = cache
        .Where(kvp => kvp.Value.ValidTo < maxTime)
        .Select(kvp => kvp.Key).ToList();
      foreach (var expiredKey in expiredKeys)
      {
        cache.Remove(expiredKey);
      }

      WriteFile(cache);
    }

    /// <inheritdoc />
    public VstsSessionToken Get(string hash)
    {
      var cache = ReadFile();
      return cache.TryGetValue(hash, out var token) ? token : null;
    }

    /// <inheritdoc />
    public void Add(string hash, VstsSessionToken token)
    {
      var cache = ReadFile();
      cache[hash] = token;
      WriteFile(cache);
    }
  }
}
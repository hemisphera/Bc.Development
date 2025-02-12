using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// Provides access to the credentials cached locally in the AL language extension.
  /// </summary>
  public class AlUserPasswordCache
  {
    /// <summary>
    /// The file of the cache.
    /// </summary>
    public FileInfo File { get; }

    private readonly Dictionary<AlUserPasswordCacheKey, NetworkCredential> _dictionary = new Dictionary<AlUserPasswordCacheKey, NetworkCredential>();


    /// <summary>
    /// Opens the cache in the folder of the AL language extension.
    /// </summary>
    /// <param name="folder">The folder of the AL language extension.</param>
    /// <returns>The cache.</returns>
    public static AlUserPasswordCache OpenFolder(string folder)
    {
      const string filename = "UserPasswordCache.dat";
      var fullPath = Path.Combine(folder, filename);
      return OpenFile(fullPath);
    }

    /// <summary>
    /// Opens the cache in the specified file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The cache.</returns>
    public static AlUserPasswordCache OpenFile(string filePath)
    {
      return new AlUserPasswordCache(filePath);
    }

    /// <summary>
    /// Enumerates all caches present in all the installed AL language extensions.
    /// </summary>
    /// <returns>The list of caches.</returns>
    public static AlUserPasswordCache[] EnumerateCaches()
    {
      var caches = AlLanguageExtension.Enumerate()
        .Select(ext => OpenFolder(ext.Folder.FullName));
      return caches
        .Where(c => c.File.Exists)
        .OrderByDescending(c => c.File.LastWriteTime)
        .ToArray();
    }


    private AlUserPasswordCache(string filename)
    {
      File = new FileInfo(filename);
      Refresh();
    }


    /// <summary>
    /// Refreshes the cache from the file.
    /// </summary>
    public void Refresh()
    {
      File.Refresh();
      if (!File.Exists) return;
      var storage = new UserProtectedFileStorage(File.FullName);
      _dictionary.Clear();

      var content = storage.Read();
      if (content == null) return;
      var jo = JObject.Parse(Encoding.UTF8.GetString(content));
      foreach (var item in jo.Properties())
      {
        var value = item.Value as JObject;
        if (value == null) continue;
        var cred = new NetworkCredential(value.Value<string>("Username"), value.Value<string>("Password"));
        _dictionary.Add(new AlUserPasswordCacheKey(item.Name), cred);
      }
    }

    /// <summary>
    /// Saves the cache to the file.
    /// </summary>
    public void Save()
    {
      var storage = new UserProtectedFileStorage(File.FullName);
      File.Directory?.Create();
      var jo = new JObject();
      foreach (var kvp in _dictionary)
      {
        jo.Add(new JProperty(kvp.Key.Key, new JObject
        {
          { "Username", kvp.Value.UserName },
          { "Password", kvp.Value.Password }
        }));
      }

      storage.Write(Encoding.UTF8.GetBytes(jo.ToString()));
    }

    /// <summary>
    /// Returns all keys present in the cache.
    /// </summary>
    /// <returns>The list of keys.</returns>
    public AlUserPasswordCacheKey[] GetKeys()
    {
      return _dictionary.Keys.ToArray();
    }

    /// <summary>
    /// Returns the credential associated with the key. If the key is not present, returns null.
    /// </summary>
    /// <param name="key">The key to look for.</param>
    /// <returns>The credential or null.</returns>
    public NetworkCredential? Get(AlUserPasswordCacheKey key)
    {
      return _dictionary.TryGetValue(key, out var cred) ? cred : null;
    }

    /// <summary>
    /// Sets the credential associated with the key.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="credential">The credential to set.</param>
    public void Set(AlUserPasswordCacheKey key, NetworkCredential credential)
    {
      _dictionary[key] = credential;
    }

    /// <summary>
    /// Deletes the credential associated with the key.
    /// </summary>
    /// <param name="key">The key to delete.</param>
    /// <returns>True if the key was present and deleted, false otherwise.</returns>
    public bool Delete(AlUserPasswordCacheKey key)
    {
      return _dictionary.Remove(key);
    }
  }
}
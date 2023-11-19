using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bc.Development.Configuration;

namespace Bc.Development.Artifacts
{
  /// <summary>
  /// The local artifact cache.
  /// </summary>
  public class LocalArtifactCache
  {
    /// <summary>
    /// Lists all locally available artifacts.
    /// </summary>
    /// <returns>The artifacts.</returns>
    public static async Task<IAsyncEnumerable<BcArtifact>> Enumerate()
    {
      var config = await BcContainerHelperConfiguration.Load();
      var di = new DirectoryInfo(config.BcArtifactsCacheFolder);
      var folders = di.GetDirectories()
        .SelectMany(type => { return type.GetDirectories().SelectMany(version => version.GetDirectories()); });
      return folders.ToAsyncEnumerable()
        .SelectAwait(async folder => await BcArtifact.FromLocalFolder(folder.FullName));
    }

    /// <summary>
    /// Cleans up the local artifact cache, removing artifacts not used for the specified time.
    /// </summary>
    /// <param name="maxAge">The maximum age.</param>
    public static async Task Cleanup(TimeSpan maxAge)
    {
      var artifacts = await Enumerate();
      await artifacts.WhereAwait(async f =>
      {
        var lastUsed = await f.GetLastUsedDate();
        return lastUsed.HasValue && lastUsed.Value < DateTime.Now - maxAge;
      }).ForEachAwaitAsync(async artifact =>
      {
        var localFolder = await artifact.GetLocalFolder();
        if (localFolder.Exists) localFolder.Delete(true);
      });
    }
  }
}
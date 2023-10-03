using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bc.Development.Configuration;

namespace Bc.Development.Artifacts
{

  public class LocalArtifactCache
  {

    public static async Task<IAsyncEnumerable<BcArtifact>> Enumerate()
    {
      var config = await BcContainerHelperConfiguration.Load();
      var di = new DirectoryInfo(config.BcArtifactsCacheFolder);
      var folders = di.GetDirectories()
        .SelectMany(type =>
        {
          return type.GetDirectories().SelectMany(version => version.GetDirectories());
        });
      return folders.ToAsyncEnumerable()
        .SelectAwait(async folder => await BcArtifact.FromLocalFolder(folder.FullName));
    }

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

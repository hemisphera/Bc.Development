using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bc.Development.Configuration;

namespace Bc.Development.Artifacts
{

  public class LocalArtifactCache
  {

    public static BcArtifact[] Enumerate()
    {
      var di = new DirectoryInfo(BcContainerHelperConfiguration.Current.BcArtifactsCacheFolder);
      var folders = di.GetDirectories()
        .SelectMany(type =>
        {
          return type.GetDirectories().SelectMany(version => version.GetDirectories());
        });
      return folders.Select(folder => BcArtifact.FromLocalFolder(folder.FullName)).ToArray();
    }

    public static async Task Cleanup(TimeSpan maxAge)
    {
      var folders = await Enumerate().ToAsyncEnumerable().WhereAwait(async f =>
      {
        var lastUsed = await f.GetLastUsedDate();
        return lastUsed.HasValue && lastUsed.Value < DateTime.Now - maxAge;
      }).ToArrayAsync();

      foreach (var folder in folders)
        Directory.Delete(folder.LocalFolder, true);
    }

  }

}

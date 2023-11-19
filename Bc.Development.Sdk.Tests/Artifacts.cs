using System.Runtime.CompilerServices;
using Bc.Development.Artifacts;
using Bc.Development.Configuration;

namespace Bc.Development.Sdk.Tests;

public class Artifacts
{
  [Fact]
  public async Task GetLocalFolder()
  {
    const string uri = "https://bcartifacts.azureedge.net/onprem/22.0.54157.55195/w1";
    var artifacts = BcArtifact.FromUri(new Uri(uri));
    var localFolder = await artifacts.GetLocalFolder();
    Assert.NotNull(localFolder);
  }

  [Fact]
  public async Task DownloadArtifacts()
  {
    var reader = new ArtifactReader(ArtifactType.OnPrem);
    var latest = await reader.GetLatest("23.0", "it", false);
    await ArtifactDownloader.Download(latest, true);
  }

  [Fact]
  public async Task DownloadAlc()
  {
    var all = await RemoteAlLanguageExtension.Enumerate();
    var latest = all.MaxBy(a => a.Version);
    if (latest != null)
    {
      var ext = await latest.Download("c:\\temp\\alc");
    }
  }
}
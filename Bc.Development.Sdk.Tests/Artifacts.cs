using System.Diagnostics;
using Bc.Development.Artifacts;
using Bc.Development.Configuration;
using Xunit.Abstractions;

namespace Bc.Development.Sdk.Tests;

public class Artifacts
{
  private readonly ITestOutputHelper _output;

  public Artifacts(ITestOutputHelper output)
  {
    _output = output;
  }

  [Fact]
  public async Task Reader()
  {
    foreach (var i in Enumerable.Range(0, 3))
    {
      var sw = Stopwatch.StartNew();
      var ar = new ArtifactReader(ArtifactType.OnPrem);
      var artifacts = await ar.GetAllRemote();
      _output.WriteLine(sw.Elapsed.ToString());
    }
  }

  [Theory]
  [InlineData("https://bcartifacts-exdbf9fwegejdqak.b02.azurefd.net/onprem/24.0.16410.18056/w1")]
  [InlineData("https://bcartifacts.azureedge.net/onprem/24.0.16410.18056/w1")]
  public void FromUri(string uri)
  {
    var af = BcArtifact.FromUri(new Uri(uri));
    Assert.Equal(ArtifactStorageAccount.BcArtifacts, af.StorageAccount);
    Assert.Equal("w1", af.Country, StringComparer.OrdinalIgnoreCase);
    Assert.Equal(Version.Parse("24.0.16410.18056"), af.Version);
    Assert.Equal(ArtifactType.OnPrem, af.Type);
  }

  [Theory]
  [InlineData("w1", "24.0", false)]
  [InlineData("w1", "24.0", true)]
  public async Task CreateArtifactUri(string country, string version, bool useCdn)
  {
    var reader = new ArtifactReader(ArtifactType.OnPrem)
    {
      UseCdn = useCdn
    };
    var af = await reader.GetLatest(version, country);
  }

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
  public async Task EnumerateAlc()
  {
    var langs = AlLanguageExtension.Enumerate(null, "C:\\_mytemp\\alc");
  }

  [Fact]
  public async Task DownloadAlc()
  {
    var all = await RemoteAlLanguageExtension.Enumerate();
    var latest = all.MaxBy(a => a.Version);
    if (latest != null)
    {
      var ext = await latest.Download("c:\\temp\\alc");
      Assert.NotEmpty(ext);
    }
  }
}
using System.Diagnostics;
using Bc.Development.Artifacts;
using Bc.Development.Configuration;
using Hsp.Extensions.Io;
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
    foreach (var unused in Enumerable.Range(0, 3))
    {
      var sw = Stopwatch.StartNew();
      var ar = new ArtifactReader(ArtifactType.OnPrem);
      var items = await ar.GetAllRemote();
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

  [Theory]
  [InlineData("23.0", "it")]
  [InlineData("24.2", "de")]
  public async Task DownloadArtifacts(string version, string country)
  {
    var reader = new ArtifactReader(ArtifactType.OnPrem);
    var latest = await reader.GetLatest(version, country);
    await ArtifactDownloader.Download(latest);
  }

  [Fact]
  public async Task GetCountries()
  {
    var reader = new ArtifactReader(ArtifactType.OnPrem);
    var countries = await reader.GetAllCountries();
    Assert.NotEmpty(countries);
  }

  [Fact]
  public async Task GetPlatform()
  {
    var reader = new ArtifactReader(ArtifactType.OnPrem);
    var platforms = await reader.GetPlatforms();
    Assert.NotEmpty(platforms);
  }

  [Theory]
  [InlineData("C:\\_mytemp\\alc")]
  [InlineData(null)]
  public void EnumerateAlc(string? sourceFolder)
  {
    var langs = AlLanguageExtension.Enumerate(null, sourceFolder);
  }

  [Fact]
  public async Task DownloadAlc()
  {
    using var tf = new TempFolder();
    var all = await RemoteAlLanguageExtension.Enumerate();
    var latest = all.MaxBy(a => a.Version);
    Assert.NotNull(latest);
    var ext = await latest.Download(tf.FolderPath);
    Assert.NotEmpty(ext);
  }

  [Fact]
  public async Task DownloadArtifact()
  {
    var reader = new ArtifactReader(ArtifactType.OnPrem);
    var latest = await reader.GetLatest("25.1", "it");
    Assert.NotNull(latest);
    var result = await ArtifactDownloader.Download(latest);
  }
}
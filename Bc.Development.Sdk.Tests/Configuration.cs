using System.Text;
using System.Text.Json.Nodes;
using Bc.Development.Configuration;
using Bogus;
using Hsp.Extensions.Io;

namespace Bc.Development.Sdk.Tests;

public class Configuration
{
  private readonly Faker _faker = new();

  [Fact]
  public void Folders()
  {
    var config = new BcContainerHelperConfiguration(); // create empty instance that loads from nowhere
    Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), config.HostHelperFolder);
  }

  [Fact]
  public void SetAndGetConfigurations()
  {
    const string folder = @"C:\Temp";
    var config = new BcContainerHelperConfiguration
    {
      HostHelperFolder = folder
    };
    Assert.Equal(folder, config.HostHelperFolder);
  }

  [Fact]
  public async Task LoadFromFile()
  {
    using (var tf = new TempFolder())
    {
      var cacheFolder = _faker.Random.Word();
      var jo = new JsonObject
      {
        ["bcartifactsCacheFolder"] = cacheFolder,
        ["sandboxContainersAreMultitenantByDefault"] = true
      };
      var fn = tf.WriteFile("config.json", Encoding.UTF8.GetBytes(jo.ToJsonString()));

      var configuration = new BcContainerHelperConfiguration(fn);
      Assert.True(await configuration.LoadFromFile());
      Assert.Equal(cacheFolder, configuration.BcArtifactsCacheFolder);
    }
  }

  [Fact]
  public void LoadCredentialCache()
  {
    _ = AlUserPasswordCache.EnumerateCaches();
  }
}
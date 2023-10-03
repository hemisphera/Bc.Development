using System.Runtime.CompilerServices;
using Bc.Development.Artifacts;

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

}
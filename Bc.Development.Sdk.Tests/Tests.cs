using Bc.Development.Util;
using Xunit.Abstractions;

namespace Bc.Development.Sdk.Tests;

public class Tests
{
  private readonly ITestOutputHelper _output;

  public Tests(ITestOutputHelper output)
  {
    _output = output;
  }


  [Fact]
  public void Credential()
  {
    const string str = "{\"endpointCredentials\":[{\"endpoint\":\"http://bc22-rtm\",\"username\":\"admin\",\"password\":\"Password123!\"}]}";
    var f = NugetExternalFeedEndpoint.LoadForUri("http://bc22-rtm/bc22", str);
    Assert.NotNull(f);
    Assert.Throws<KeyNotFoundException>(() => NugetExternalFeedEndpoint.LoadForUri("http://bc23-rtm/bc22", str));
    Assert.Throws<FormatException>(() => NugetExternalFeedEndpoint.LoadForUri("http://bc23-rtm/bc22", "blah"));
  }
}
using Bc.Development.Configuration;

namespace Bc.Development.Sdk.Tests
{

  public class Configuration
  {

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

  }

}

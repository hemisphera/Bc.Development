using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Bc.Development.Configuration
{

  [JsonObject(MemberSerialization.OptIn)]
  public class BcContainerHelperConfiguration
  {

    private static readonly string FilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
      "BcContainerHelper",
      "BcContainerHelper.config.json");

    public static BcContainerHelperConfiguration Current { get; } = new BcContainerHelperConfiguration();


    public string BcArtifactsCacheFolder { get; set; }


    private BcContainerHelperConfiguration()
    {
      BcArtifactsCacheFolder = "c:\\bcartifacts.cache";
      Load();
    }


    public void Load()
    {
      if (!File.Exists(FilePath)) return;
      
      var content = File.ReadAllText(FilePath, Encoding.UTF8);
      JsonConvert.PopulateObject(content, this);
    }

  }

}
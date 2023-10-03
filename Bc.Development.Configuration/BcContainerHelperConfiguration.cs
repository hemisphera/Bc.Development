using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bc.Development.Configuration
{

  [JsonObject(MemberSerialization.OptIn)]
  public class BcContainerHelperConfiguration
  {

    private static BcContainerHelperConfiguration _instance;

    private static readonly string FilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
      "BcContainerHelper",
      "BcContainerHelper.config.json");


    public static async Task<BcContainerHelperConfiguration> Load(bool force = false)
    {
      if (_instance != null && !force) return _instance;
      _instance = new BcContainerHelperConfiguration();
      if (!File.Exists(FilePath)) return _instance;

      using (var fs = File.OpenText(FilePath))
      {
        var jsonContents = await fs.ReadToEndAsync();
        _instance._innerObject = JObject.Parse(jsonContents);
      }
      return _instance;
    }



    private JObject _innerObject;

    public string BcArtifactsCacheFolder
    {
      get => GetSettingValue("bcartifactsCacheFolder", "c:\\bcartifacts.cache");
      set => _innerObject["bcartifactsCacheFolder"] = value;
    }

    public string HostHelperFolder
    {
      get => GetSettingValue("hostHelperFolder", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "BcContainerHelper"));
      set => _innerObject["hostHelperFolder"] = value;
    }


    private BcContainerHelperConfiguration()
    {
      InitDefaults();
    }

    private void InitDefaults()
    {

      BcArtifactsCacheFolder = "c:\\bcartifacts.cache";
    }

    private T GetSettingValue<T>(string key, T defaultValue = default)
    {
      var token = _innerObject[key];
      return token == null ? defaultValue : token.Value<T>();
    }

    public async Task Save()
    {
      using (var fs = File.CreateText(FilePath))
      {
        var jsonContents = _innerObject.ToString(Formatting.Indented);
        await fs.WriteAsync(jsonContents);
      }
    }

  }

}
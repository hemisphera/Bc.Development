using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// BcContainerHelper configuration 
  /// </summary>
  [JsonObject(MemberSerialization.OptIn)]
  public class BcContainerHelperConfiguration
  {
    private static BcContainerHelperConfiguration? _instance;

    private static readonly string FilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      "BcContainerHelper",
      "BcContainerHelper.config.json");


    /// <summary>
    /// Load the configuration from disk. This will cache the configuration in memory, once loaded.
    /// </summary>
    /// <param name="force">Force reload from disk, even if the configuration was already loaded.</param>
    /// <returns>An instance of the configuration.</returns>
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


    private JObject _innerObject = new JObject();

    /// <summary>
    /// The folder where BC artifacts are stored
    /// </summary>
    public string? BcArtifactsCacheFolder
    {
      get => GetSettingValue("bcartifactsCacheFolder", "c:\\bcartifacts.cache");
      set => SetSettingValue("bcartifactsCacheFolder", value);
    }

    /// <summary>
    /// The BcContainerHelper host helper folder.
    /// </summary>
    public string? HostHelperFolder
    {
      get => GetSettingValue("hostHelperFolder", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BcContainerHelper"));
      set => SetSettingValue("hostHelperFolder", value);
    }


    /// <summary>
    /// </summary>
    public BcContainerHelperConfiguration()
    {
    }


    private string? GetSettingValue(string key, string? defaultValue = null)
    {
      var token = _innerObject[key];
      return token == null ? defaultValue : token.Value<string>();
    }

    private void SetSettingValue(string key, string? value)
    {
      if (_innerObject.ContainsKey(key))
        _innerObject[key] = new JValue(value);
      else
        _innerObject.Add(key, new JValue(value));
    }

    /// <summary>
    /// Write the configuration to disk.
    /// </summary>
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
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
  public class BcContainerHelperConfiguration
  {
    private static BcContainerHelperConfiguration? _instance;

    private static readonly string DefaultFilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      "BcContainerHelper",
      "BcContainerHelper.config.json");


    private readonly FileInfo _file;


    /// <summary>
    /// Load the configuration from disk. This will cache the configuration in memory, once loaded.
    /// </summary>
    /// <param name="force">Force reload from disk, even if the configuration was already loaded.</param>
    /// <returns>An instance of the configuration.</returns>
    public static async Task<BcContainerHelperConfiguration> Load(bool force = false)
    {
      if (_instance != null && !force) return _instance;
      _instance = new BcContainerHelperConfiguration();
      await _instance.LoadFromFile();
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
    public BcContainerHelperConfiguration(string? filePath = null)
    {
      _file = new FileInfo(filePath ?? DefaultFilePath);
    }


    private string? GetSettingValue(string key, string? defaultValue = null)
    {
      var token = _innerObject[key];
      return token == null ? defaultValue : token.Value<string>();
    }

    private void SetSettingValue(string key, string? value)
    {
      var valueToken = string.IsNullOrEmpty(value) || value == null ? JValue.CreateNull() : JToken.FromObject(value);
      if (_innerObject.ContainsKey(key))
        _innerObject[key] = valueToken;
      else
        _innerObject.Add(key, valueToken);
    }

    [Obsolete("Use 'SaveToFile' instead.")]
    public Task Save()
    {
      return SaveToFile();
    }

    /// <summary>
    /// Write the configuration to disk.
    /// </summary>
    public async Task<bool> LoadFromFile()
    {
      _file.Refresh();
      if (!_file.Exists) return false;
      using (var fs = File.OpenText(_file.FullName))
      {
        var jsonContents = await fs.ReadToEndAsync();
        _innerObject = JObject.Parse(jsonContents);
      }

      return true;
    }

    /// <summary>
    /// Write the configuration to disk.
    /// </summary>
    public async Task SaveToFile()
    {
      _file.Directory?.Create();
      using (var fs = File.CreateText(_file.FullName))
      using (var jw = new JsonTextWriter(fs))
      {
        jw.Indentation = 4;
        jw.Formatting = Formatting.Indented;
        await _innerObject.WriteToAsync(jw);
        await jw.FlushAsync();
      }
    }
  }
}
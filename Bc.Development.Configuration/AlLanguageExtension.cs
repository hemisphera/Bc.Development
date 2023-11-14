using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// Provides information about an AL Language Extension.
  /// </summary>
  public class AlLanguageExtension
  {
    private static AlLanguageExtension[] All { get; set; }

    /// <summary>
    /// Returns a list of all AL Language Extensions installed on the system.
    /// </summary>
    /// <param name="force">If true, the list is reloaded from the file system, even if it was already loaded.</param>
    /// <returns>The list of AL Language Extensions.</returns>
    public static AlLanguageExtension[] Enumerate(bool force = false)
    {
      if (All == null || force)
        All = DetectAlLanguageExtensionVersions();
      return All;
    }

    private static AlLanguageExtension[] DetectAlLanguageExtensionVersions()
    {
      var root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".vscode",
        "extensions");
      if (!Directory.Exists(root)) return Array.Empty<AlLanguageExtension>();
      var executables = Directory.EnumerateFiles(root, "alc.exe", SearchOption.AllDirectories);
      return executables.Select(executable =>
      {
        try
        {
          return new AlLanguageExtension(Path.GetDirectoryName(executable));
        }
        catch
        {
          return null;
        }
      }).Where(a => a != null).ToArray();
    }

    /// <summary>
    /// Returns the latest version of the AL Language Extension for the specified platform.
    /// </summary>
    /// <param name="platform">The platform to return the latest version for. If null, the latest version for any platform is returned.</param>
    /// <returns>The latest version of the AL Language Extension for the specified platform.</returns>
    public static AlLanguageExtension GetLatest(string platform = null)
    {
      return Enumerate()
        .Where(a => string.IsNullOrEmpty(platform) || a.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase))
        .OrderBy(v => v.FileVersion)
        .LastOrDefault();
    }

    /// <summary>
    /// The folder where the AL Language Extension is installed.
    /// </summary>
    public DirectoryInfo Folder { get; }

    /// <summary>
    /// The full path to the alc.exe file.
    /// </summary>
    public string AlcPAth => Path.Combine(Folder.FullName, "alc.exe");

    /// <summary>
    /// The version of the AL Language Extension.
    /// </summary>
    public Version FileVersion { get; set; }

    /// <summary>
    /// The platform the AL Language Extension is for. 
    /// </summary>
    public string Platform => Folder.Name;


    /// <summary>
    /// Creates a new instance of the AlLanguageExtension class.
    /// </summary>
    /// <param name="folder">The folder where the AL Language Extension is installed.</param>
    public AlLanguageExtension(string folder)
    {
      Folder = new DirectoryInfo(folder);
      if (!File.Exists(AlcPAth)) throw new NotSupportedException();
      FileVersion = Version.Parse(FileVersionInfo.GetVersionInfo(AlcPAth).FileVersion);
    }
  }
}
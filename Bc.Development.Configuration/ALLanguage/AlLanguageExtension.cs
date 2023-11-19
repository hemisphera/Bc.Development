using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// Provides information about a loacally installed AL Language Extension.
  /// </summary>
  public class AlLanguageExtension
  {
    private static string DefaultRootFolder => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".vscode",
      "extensions");

    /// <summary>
    /// Loads the AL Language Extension from the specified folder.
    /// </summary>
    /// <param name="folder">The folder where the AL Language Extension is installed.</param>
    /// <returns>The AL Language Extension.</returns>
    public static AlLanguageExtension FromFolder(string folder)
    {
      return new AlLanguageExtension(folder);
    }

    /// <summary>
    /// Returns a list of all AL Language Extensions installed on the system.
    /// </summary>
    /// <param name="rootFolder">The root folder to search for AL Language Extensions. If null, the default root folder is used.</param>
    /// <param name="platform">The platform to return the latest version for. If null, all platforms are returned.</param>
    /// <returns>The list of AL Language Extensions.</returns>
    public static AlLanguageExtension[] Enumerate(AlLanguagePlatform? platform = null, string rootFolder = null)
    {
      if (String.IsNullOrEmpty(rootFolder)) rootFolder = DefaultRootFolder;
      if (!Directory.Exists(rootFolder)) return Array.Empty<AlLanguageExtension>();
      var executables = Directory.EnumerateFiles(rootFolder, "alc.exe", SearchOption.AllDirectories);
      return executables.Select(executable =>
      {
        try
        {
          var ext = FromFolder(Path.GetDirectoryName(executable));
          return (platform == null || ext.Platform == platform)
            ? ext
            : null;
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
    /// <param name="rootFolder">The root folder to search for AL Language Extensions. If null, the default root folder is used.</param>
    /// <param name="platform">The platform to return the latest version for. If null, the latest version for any platform is returned.</param>
    /// <returns>The latest version of the AL Language Extension for the specified platform.</returns>
    public static AlLanguageExtension GetLatest(string rootFolder = null, AlLanguagePlatform? platform = null)
    {
      if (platform == null) platform = AlLanguagePlatform.DotNetWindows;
      return Enumerate(platform, rootFolder)
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
    public Version FileVersion { get; }

    /// <summary>
    /// The platform the AL Language Extension is for. 
    /// </summary>
    public AlLanguagePlatform Platform { get; }


    /// <summary>
    /// Creates a new instance of the AlLanguageExtension class.
    /// </summary>
    /// <param name="folder">The folder where the AL Language Extension is installed.</param>
    private AlLanguageExtension(string folder)
    {
      Folder = new DirectoryInfo(folder);
      if (!File.Exists(AlcPAth)) throw new NotSupportedException();
      FileVersion = Version.Parse(FileVersionInfo.GetVersionInfo(AlcPAth).FileVersion);
      Platform = DetectPlatform(Folder);
    }

    private static AlLanguagePlatform DetectPlatform(DirectoryInfo folder)
    {
      if (folder.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
        return AlLanguagePlatform.NetFramework;
      if (folder.Name.Equals("win32", StringComparison.OrdinalIgnoreCase))
        return AlLanguagePlatform.DotNetWindows;
      return AlLanguagePlatform.Other;
    }
  }
}
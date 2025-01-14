using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

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
    public static AlLanguageExtension[] Enumerate(AlLanguagePlatform? platform = null, string? rootFolder = null)
    {
      if (string.IsNullOrEmpty(rootFolder)) rootFolder = DefaultRootFolder;
      if (!Directory.Exists(rootFolder)) return Array.Empty<AlLanguageExtension>();
      var executableNames = new[] { "alc.exe", "alc" };
      return executableNames.SelectMany(executableName =>
      {
        var executables = Directory.EnumerateFiles(rootFolder, executableName, SearchOption.AllDirectories);
        return executables.Select(executable =>
        {
          try
          {
            var directory = Path.GetDirectoryName(executable);
            if (directory == null) return null;
            var ext = FromFolder(directory);
            return platform == null || ext.Platform == platform
              ? ext
              : null;
          }
          catch
          {
            return null;
          }
        });
      }).OfType<AlLanguageExtension>().ToArray();
    }

    /// <summary>
    /// Returns the latest version of the AL Language Extension for the specified platform.
    /// </summary>
    /// <param name="rootFolder">The root folder to search for AL Language Extensions. If null, the default root folder is used.</param>
    /// <param name="platform">The platform to return the latest version for. If null, the latest version for any platform is returned.</param>
    /// <returns>The latest version of the AL Language Extension for the specified platform.</returns>
    public static AlLanguageExtension? GetLatest(string? rootFolder = null, AlLanguagePlatform? platform = null)
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
    public string AlcPath => AlcExecutable.FullName;

    /// <summary>
    /// The file info pointing to the ALC executable. 
    /// </summary>
    public FileInfo AlcExecutable { get; }

    /// <summary>
    /// The version of the AL Language Extension.
    /// </summary>
    public Version FileVersion { get; }

    /// <summary>
    /// The version of the AL Language Extension VSIX package.
    /// </summary>
    public Version? ProductVersion { get; }

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
      AlcExecutable = new FileInfo(Path.Combine(folder, "alc.exe"));
      if (!AlcExecutable.Exists)
      {
        AlcExecutable = new FileInfo(Path.Combine(folder, "alc"));
      }

      if (!AlcExecutable.Exists)
        throw new NotSupportedException($"The folder '{folder}' does not contain a supported AL language extension.");

      Folder = AlcExecutable.Directory ?? throw new NotSupportedException();
      FileVersion = Version.Parse(FileVersionInfo.GetVersionInfo(AlcPath).FileVersion);
      ProductVersion = DetectProductVersion(Folder);
      Platform = DetectPlatform(Folder);
    }

    private static AlLanguagePlatform DetectPlatform(DirectoryInfo folder)
    {
      if (folder.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
        return AlLanguagePlatform.NetFramework;
      if (folder.Name.Equals("darwin", StringComparison.OrdinalIgnoreCase))
        return AlLanguagePlatform.DotNetDarwin;
      if (folder.Name.Equals("linux", StringComparison.OrdinalIgnoreCase))
        return AlLanguagePlatform.DotNetLinux;
      if (folder.Name.Equals("win32", StringComparison.OrdinalIgnoreCase))
        return AlLanguagePlatform.DotNetWindows;
      return AlLanguagePlatform.Other;
    }

    private static Version? DetectProductVersion(DirectoryInfo root)
    {
      var folder = root;
      FileInfo? packageFile = null;
      var levels = 2;
      while (levels > 0)
      {
        levels -= 1;
        folder = folder.Parent;
        if (folder == null) return null;
        packageFile = new FileInfo(Path.Combine(folder.FullName, "package.json"));
        if (packageFile.Exists) break;
      }

      if (packageFile?.Exists != true) return null;

      var jo = JObject.Parse(File.ReadAllText(packageFile.FullName, Encoding.UTF8));
      return Version.TryParse(jo.Value<string>("version") ?? string.Empty, out var ver) ? ver : null;
    }
  }
}
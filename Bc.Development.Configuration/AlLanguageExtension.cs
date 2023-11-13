using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace Bc.Development.Configuration
{
  public class AlLanguageExtension
  {
    private static AlLanguageExtension[] All { get; set; }

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

    public static AlLanguageExtension GetLatest(string platform = null)
    {
      return Enumerate()
        .Where(a => string.IsNullOrEmpty(platform) || a.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase))
        .OrderBy(v => v.FileVersion)
        .LastOrDefault();
    }


    public DirectoryInfo Folder { get; }

    public string AlcPAth => Path.Combine(Folder.FullName, "alc.exe");

    public Version FileVersion { get; set; }

    public string Platform => Folder.Name;


    public AlLanguageExtension(string folder)
    {
      Folder = new DirectoryInfo(folder);
      if (!File.Exists(AlcPAth)) throw new NotSupportedException();
      FileVersion = Version.Parse(FileVersionInfo.GetVersionInfo(AlcPAth).FileVersion);
    }


    public NetworkCredential GetStoredCredential(string key)
    {
      var reader = CredentialReader.CreateUserPasswordCache(Folder.FullName);
      return reader.TryGetSavedCredentials(key);
    }

    public NetworkCredential GetStoredCredential(string server, string serverInstance)
    {
      var reader = CredentialReader.CreateUserPasswordCache(Folder.FullName);
      return reader.TryGetSavedCredentials(server, serverInstance);
    }

    public IEnumerable<string> ListStoredCredentialKeys()
    {
      var reader = CredentialReader.CreateUserPasswordCache(Folder.FullName);
      return reader.ListStoredCredentials();
    }
  }
}
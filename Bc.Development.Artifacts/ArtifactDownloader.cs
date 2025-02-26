using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bc.Development.Configuration;
using Hsp.Extensions.Io;
using Newtonsoft.Json.Linq;

namespace Bc.Development.Artifacts
{
  /// <summary>
  /// Downloads BC artifacts.
  /// </summary>
  public static class ArtifactDownloader
  {
    private static readonly Dictionary<string, string> UrlMap = new Dictionary<string, string>
    {
      {
        "https://go.microsoft.com/fwlink/?LinkID=844461",
        "https://bcartifacts.azureedge.net/prerequisites/DotNetCore.1.0.4_1.1.1-WindowsHosting.exe"
      },
      {
        "https://download.microsoft.com/download/C/9/E/C9E8180D-4E51-40A6-A9BF-776990D8BCA9/rewrite_amd64.msi",
        "https://bcartifacts.azureedge.net/prerequisites/rewrite_2.0_rtw_x64.msi"
      },
      {
        "https://download.microsoft.com/download/5/5/3/553C731E-9333-40FB-ADE3-E02DC9643B31/OpenXMLSDKV25.msi",
        "https://bcartifacts.azureedge.net/prerequisites/OpenXMLSDKv25.msi"
      },

      {
        "https://download.microsoft.com/download/A/1/2/A129F694-233C-4C7C-860F-F73139CF2E01/ENU/x86/ReportViewer.msi",
        "https://bcartifacts.azureedge.net/prerequisites/ReportViewer.msi"
      },
      {
        "https://download.microsoft.com/download/1/3/0/13089488-91FC-4E22-AD68-5BE58BD5C014/ENU/x86/SQLSysClrTypes.msi",
        "https://bcartifacts.azureedge.net/prerequisites/SQLSysClrTypes.msi"
      },
      {
        "https://download.microsoft.com/download/3/A/6/3A632674-A016-4E31-A675-94BE390EA739/ENU/x64/sqlncli.msi",
        "https://bcartifacts.azureedge.net/prerequisites/sqlncli.msi"
      },
      {
        "https://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x86.exe",
        "https://bcartifacts.azureedge.net/prerequisites/vcredist_x86.exe"
      }
    };

    /// <summary>
    /// Downloads the artifact specified through the artifact URI to the local cache folder.
    /// </summary>
    /// <param name="artifactUri">The artifact URI.</param>
    /// <param name="includePlatform">Specifies if platform artifacts should be downloaded as well.</param>
    /// <param name="force">Specifies if the artifact should be downloaded even if it already exists in the cache.</param>
    /// <returns>The download result.</returns>
    public static async Task<ArtifactDownloadResult> Download(Uri artifactUri, bool includePlatform = true, bool force = false)
    {
      var artifact = BcArtifact.FromUri(artifactUri);
      return await Download(artifact, includePlatform, force);
    }

    /// <summary>
    /// Downloads the artifact to the local cache folder.
    /// </summary>
    /// <param name="artifact">The artifact to download.</param>
    /// <param name="includePlatform">Specifies if platform artifacts should be downloaded as well.</param>
    /// <param name="force">Specifies if the artifact should be downloaded even if it already exists in the cache.</param>
    /// <returns>The download result.</returns>
    public static async Task<ArtifactDownloadResult> Download(BcArtifact artifact, bool includePlatform = true, bool force = false)
    {
      return await Download(artifact.StorageAccount ?? Defaults.DefaultStorageAccount, artifact.Type, artifact.Version, artifact.Country, includePlatform, force);
    }

    /// <summary>
    /// Downloads the artifact with the specified properties to the local cache.
    /// </summary>
    /// <param name="account">The artifact storage account.</param>
    /// <param name="type">The artifact type.</param>
    /// <param name="version">The version prefix.</param>
    /// <param name="country">The country.</param>
    /// <param name="includePlatform">Specifies if platform artifacts should be downloaded as well.</param>
    /// <param name="force">Specifies if the artifact should be downloaded even if it already exists in the cache.</param>
    /// <param name="useCdn">Specifies if the CDN should be used.</param>
    /// <returns>The download result.</returns>
    public static async Task<ArtifactDownloadResult> Download(ArtifactStorageAccount account, ArtifactType type, Version version, string country, bool includePlatform = true, bool force = false, bool useCdn = true)
    {
      if (includePlatform && country.Equals(Defaults.PlatformIdentifier, StringComparison.OrdinalIgnoreCase))
        includePlatform = false;

      var reader = new ArtifactReader(type, account)
      {
        UseCdn = useCdn
      };
      var result = new ArtifactDownloadResult();
      await Task.WhenAll(
        Task.Run(async () => result.Artifact = await DownloadUri(reader.MakeArtifactUri(version, country), force)),
        Task.Run(async () =>
        {
          if (!includePlatform) return null;
          return result.PlatformArtifact = await DownloadUri(reader.MakeArtifactUri(version, Defaults.PlatformIdentifier), force, true);
        })
      );
      if (country == Defaults.PlatformIdentifier)
        result.PlatformArtifact = result.Artifact;
      return result;
    }

    private static async Task<BcArtifact> DownloadUri(Uri uri, bool force, bool includePrereq = false)
    {
      var config = await BcContainerHelperConfiguration.Load();
      var lockFilePath = Path.Combine(
        config.BcArtifactsCacheFolder,
        $"dl-{uri.ToString().Split('?')[0].Substring(8).Replace('/', '_')}");
      var lockFile = new LockFile(lockFilePath);
      using (lockFile)
        try
        {
          await lockFile.Wait();

          var af = BcArtifact.FromUri(uri);
          var folder = await af.GetLocalFolder();
          if (folder.Exists && force)
            folder.Delete(true);

          if (!folder.Exists)
          {
            var tempFolder = await DownloadUriToTempFolder(uri, folder, includePrereq);
            Directory.Move(tempFolder, folder.FullName);
          }

          await af.SetLastUsedDate();
          return af;
        }
        finally
        {
          await lockFile.Release();
        }
    }

    private static async Task<string> DownloadUriToTempFolder(Uri uri, DirectoryInfo targetFolder, bool includePrereq)
    {
      var tempFolder = targetFolder + "_dl";
      var tempFile = new FileInfo(Path.GetTempFileName());
      try
      {
        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        Directory.CreateDirectory(tempFolder);

        using (var cl = new HttpClient())
        {
          await DownloadFile(cl, uri, tempFile);

          using (var zf = ZipFile.OpenRead(tempFile.FullName))
          {
            foreach (var zipEntry in zf.Entries)
            {
              if (string.IsNullOrEmpty(Path.GetExtension(zipEntry.FullName))) continue;
              var targetFile = new FileInfo(Path.Combine(tempFolder, zipEntry.FullName));
              targetFile.Directory?.Create();
              if (targetFile.Exists) targetFile.Delete();
              zipEntry.ExtractToFile(targetFile.FullName);
            }
          }

          if (includePrereq)
          {
            await DownloadPrerequisites(cl, tempFolder);
          }
        }

        return tempFolder;
      }
      finally
      {
        if (tempFile.Exists) tempFile.Delete();
      }
    }

    private static async Task DownloadFile(HttpClient cl, Uri uri, FileInfo tempFile)
    {
      try
      {
        tempFile.Directory?.Create();
        using (var fs = tempFile.Create())
        {
          var remoteStream = await cl.GetStreamAsync(uri);
          await remoteStream.CopyToAsync(fs);
        }
      }
      catch
      {
        if (tempFile.Exists) tempFile.Delete();
        throw;
      }
    }

    private static async Task DownloadPrerequisites(HttpClient cl, string tempFolder)
    {
      const string index = "Prerequisite Components.json";
      var indexFile = new FileInfo(Path.Combine(tempFolder, index));
      if (!indexFile.Exists) return;
      using (var fs = indexFile.OpenText())
      {
        var contents = JObject.Parse(await fs.ReadToEndAsync());
        var tasks = contents.Properties().Select(async prop =>
        {
          var targetFile = new FileInfo(Path.Combine(tempFolder, prop.Name));
          var sourceUri = GetSourceUri(prop.Value.Value<string>());
          await DownloadFile(cl, sourceUri, targetFile);
        });
        await Task.WhenAll(tasks);
      }
    }

    private static Uri GetSourceUri(string value)
    {
      var me = UrlMap.FirstOrDefault(a => a.Key.Equals(value, StringComparison.OrdinalIgnoreCase));
      var actualUri = me.Value != null ? new Uri(me.Value) : new Uri(value);
      return CdnHelper.ResolveUri(actualUri);
    }
  }
}
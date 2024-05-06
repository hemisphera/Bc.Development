using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Bc.Development.Configuration;
using Bc.Development.Util;
using Hsp.Extensions.Io;
using Ionic.Zip;

namespace Bc.Development.Artifacts
{
  /// <summary>
  /// Downloads BC artifacts.
  /// </summary>
  public static class ArtifactDownloader
  {
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
          return result.PlatformArtifact = await DownloadUri(reader.MakeArtifactUri(version, Defaults.PlatformIdentifier), force);
        })
      );
      if (country == Defaults.PlatformIdentifier)
        result.PlatformArtifact = result.Artifact;
      return result;
    }

    private static async Task<BcArtifact> DownloadUri(Uri uri, bool force)
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
            var tempFolder = await DownloadUriToTempFolder(uri, folder);
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

    private static async Task<string> DownloadUriToTempFolder(Uri uri, DirectoryInfo targetFolder)
    {
      var tempFolder = targetFolder + "_dl";
      var tempFile = new FileInfo(Path.GetTempFileName());
      try
      {
        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        Directory.CreateDirectory(tempFolder);

        using (var fs = tempFile.Create())
        using (var cl = new HttpClient())
        {
          var remoteStream = await cl.GetStreamAsync(uri);
          await remoteStream.CopyToAsync(fs);
        }

        using (var zf = ZipFile.Read(tempFile.FullName))
        {
          foreach (var zipEntry in zf.Entries)
          {
            if (string.IsNullOrEmpty(Path.GetExtension(zipEntry.FileName))) continue;
            zipEntry.Extract(tempFolder, ExtractExistingFileAction.OverwriteSilently);
          }
        }

        return tempFolder;
      }
      finally
      {
        if (tempFile.Exists) tempFile.Delete();
      }
    }
  }
}
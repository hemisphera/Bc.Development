﻿using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Ionic.Zip;

namespace Bc.Development.Artifacts
{

  public static class ArtifactDownloader
  {

    public static async Task<ArtifactDownloadResult> Download(Uri artifactUri, bool includePlatform = true, bool force = false)
    {
      var artifact = BcArtifact.FromUri(artifactUri);
      return await Download(artifact, includePlatform, force);
    }

    public static async Task<ArtifactDownloadResult> Download(BcArtifact artifact, bool includePlatform = true, bool force = false)
    {
      return await Download(artifact.StorageAccount ?? Defaults.DefaultStorageAccount, artifact.Type, artifact.Version, artifact.Country, includePlatform, force);
    }

    public static async Task<ArtifactDownloadResult> Download(ArtifactStorageAccount account, ArtifactType type, Version version, string country, bool includePlatform = true, bool force = false)
    {
      if (includePlatform && country.Equals(Defaults.PlatformIdentifier, StringComparison.OrdinalIgnoreCase))
        includePlatform = false;

      var reader = new ArtifactReader(type, account);
      var result = new ArtifactDownloadResult();
      await Task.WhenAll(
        Task.Run(async () => result.Artifact = await DownloadUri(reader.MakeArtifactUri(version, country), force)),
        Task.Run(async () =>
        {
          if (!includePlatform) return null;
          return result.PlatformArtifact = await DownloadUri(reader.MakeArtifactUri(version, Defaults.PlatformIdentifier), force);
        })
      );
      return result;
    }

    private static async Task<BcArtifact> DownloadUri(Uri uri, bool force)
    {
      var mutex = new Mutex(false, $"dl-{uri.ToString().Split('?')[0].Substring(8).Replace('/', '_')}");
      try
      {
        mutex.WaitOne();

        var af = BcArtifact.FromUri(uri);
        var folder = new DirectoryInfo(af.LocalFolder);
        if (folder.Exists && force)
          folder.Delete(true);

        if (!folder.Exists)
        {
          var tempFolder = await DownloadUriToTempFolder(uri);
          folder.Parent?.Create();
          Directory.Move(tempFolder, folder.FullName);
        }

        await af.SetLastUsedDate();
        return af;
      }
      finally
      {
        mutex.ReleaseMutex();
      }
    }

    private static async Task<string> DownloadUriToTempFolder(Uri uri)
    {
      var tempFile = new FileInfo(Path.GetTempFileName());
      var folder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
      try
      {
        if (!folder.Exists) folder.Create();
        var bc = new BlobClient(uri);
        using (var fs = tempFile.Create())
          await bc.DownloadToAsync(fs);
        using (var zf = ZipFile.Read(tempFile.FullName))
          zf.ExtractAll(folder.FullName, ExtractExistingFileAction.OverwriteSilently);
        return folder.FullName;
      }
      finally
      {
        if (tempFile.Exists) tempFile.Delete();
      }
    }

  }

}
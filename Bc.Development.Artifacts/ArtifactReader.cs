using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Bc.Development.Artifacts
{

  public class ArtifactReader
  {

    internal const string PlatformIdentifier = "platform";

    public ArtifactStorageAccount Account { get; set; }

    public ArtifactType ArtifactType { get; }



    public ArtifactReader(
      ArtifactType type,
      ArtifactStorageAccount account = ArtifactStorageAccount.BcArtifacts)
    {
      Account = account;
      ArtifactType = type;
      if (Account == ArtifactStorageAccount.BcInsider && ArtifactType != ArtifactType.Sandbox)
        throw new NotSupportedException("Only Sandbox is supported for BcInsider");
    }


    public Uri MakeArtifactUri(Version version, string country)
    {
      var ub = new UriBuilder(GetAccountUri());
      ub.Path += $"/{version}/{country}";
      return ub.Uri;
    }

    public async Task<BcArtifact[]> GetAll(string versionPrefix, string country)
    {
      var accountUri = GetAccountUri();
      var blobclient = new BlobContainerClient(accountUri);
      var result = await blobclient.GetBlobsAsync()
        .Select(a =>
        {
          var parts = $"{a.Name}".Split('/').Reverse().Take(2).Reverse().ToArray();
          var uri = new Uri($"{MakeArtifactUri(new Version(parts[0]), parts[1])}");
          return BcArtifact.FromUri(uri);
        })
        .Where(a => string.IsNullOrEmpty(country) || a.Country.Equals(country, StringComparison.OrdinalIgnoreCase))
        .Where(a => string.IsNullOrEmpty(versionPrefix) || a.Version.ToString().StartsWith(versionPrefix, StringComparison.OrdinalIgnoreCase))
        .ToArrayAsync();
      return result;
    }

    public async Task<BcArtifact> GetLatest(string versionPrefix, string country)
    {
      var all = await GetAll(versionPrefix, country);
      return all.OrderByDescending(a => a.Version).FirstOrDefault();
    }

    public async Task<BcArtifact> GetNextMajor(string country)
    {
      if (Account != ArtifactStorageAccount.BcInsider)
        throw new NotSupportedException("Only supported for bcartifacts");
      var baseVersion = await GetCurrentVersion();

      var previewReader = new ArtifactReader(ArtifactType, ArtifactStorageAccount.BcPublicPreview);

      BcArtifact preview = null;
      BcArtifact insider = null;
      await Task.WhenAll(
        Task.Run(async () => preview = (await previewReader.GetAll($"{baseVersion.Major + 1}.", country)).LastOrDefault()),
        Task.Run(async () => insider = (await GetAll($"{baseVersion.Major + 1}.", country)).LastOrDefault())
      );
      if (insider == null) return preview;
      if (preview != null && preview.Version >= insider.Version) return preview;
      return insider;
    }

    public async Task<BcArtifact> GetNextMinor(string country)
    {
      if (Account != ArtifactStorageAccount.BcInsider)
        throw new NotSupportedException("Only supported for bcartifacts");
      var baseVersion = await GetCurrentVersion();

      var previewReader = new ArtifactReader(ArtifactType, ArtifactStorageAccount.BcPublicPreview);

      BcArtifact preview = null;
      BcArtifact insider = null;
      await Task.WhenAll(
        Task.Run(async () => preview = (await previewReader.GetAll($"{baseVersion.Major}.{baseVersion.Major + 1}", country)).LastOrDefault()),
        Task.Run(async () => insider = (await GetAll($"{baseVersion.Major}.{baseVersion.Minor + 1}", country)).LastOrDefault())
      );
      if (insider == null) return preview;
      if (preview != null && preview.Version >= insider.Version) return preview;
      return insider;
    }

    public async Task<Version> GetCurrentVersion()
    {
      var reader = new ArtifactReader(ArtifactType.Sandbox);
      var current = await reader.GetLatest(null, "base");
      return current.Version;
    }

    public async Task<ArtifactDownloadResult> Download(BcArtifact artifact, bool includePlatform = true)
    {
      return await Download(artifact.Version, artifact.Country, includePlatform);
    }

    public async Task<ArtifactDownloadResult> Download(Version version, string country, bool includePlatform = true, bool force = false)
    {
      if (includePlatform && country.Equals(PlatformIdentifier, StringComparison.OrdinalIgnoreCase))
        includePlatform = false;

      var result = new ArtifactDownloadResult();
      await Task.WhenAll(
        Task.Run(async () => result.Folder = await DownloadUri(MakeArtifactUri(version, country), force)),
        Task.Run(async () =>
        {
          if (!includePlatform) return null;
          return result.PlatformFolder = await DownloadUri(MakeArtifactUri(version, PlatformIdentifier), force);
        })
      );
      return result;
    }

    private static void TagLastUsed(string resultFolder)
    {
      try
      {
        File.WriteAllText(Path.Combine(resultFolder, "lastused"), $"{DateTime.UtcNow.Ticks}");
      }
      catch
      {
        // ignore
      }
    }

    private static async Task<string> DownloadUri(Uri uri, bool force)
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
        TagLastUsed(folder.FullName);
        return folder.FullName;
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
        using (var zf = ZipFile.OpenRead(tempFile.FullName))
          zf.ExtractToDirectory(folder.FullName);
        return folder.FullName;
      }
      finally
      {
        if (tempFile.Exists) tempFile.Delete();
      }
    }

    private Uri GetAccountUri()
    {
      return new Uri($"https://{Account}.blob.core.windows.net/{ArtifactType}".ToLowerInvariant());
    }

  }

}
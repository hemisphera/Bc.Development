using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Bc.Development.Artifacts
{
  public class ArtifactReader
  {
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


    public static Uri MakeArtifactUri(ArtifactStorageAccount account, ArtifactType artifactType, Version version, string country)
    {
      var ub = new UriBuilder(GetAccountUri(account, artifactType));
      ub.Path += $"/{version}/{country}";
      return ub.Uri;
    }

    public Uri MakeArtifactUri(Version version, string country)
    {
      return MakeArtifactUri(Account, ArtifactType, version, country);
    }

    private async Task<BcArtifact[]> GetAllRemote()
    {
      var accountUri = GetAccountUri();
      var blobclient = new BlobContainerClient(accountUri);
      return await blobclient.GetBlobsAsync()
        .Select(a =>
        {
          var parts = $"{a.Name}".Split('/').Reverse().Take(2).Reverse().ToArray();
          var uri = new Uri($"{MakeArtifactUri(new Version(parts[0]), parts[1])}");
          return BcArtifact.FromUri(uri);
        }).ToArrayAsync();
    }

    private static async Task<BcArtifact[]> GetAllLocal()
    {
      var r = await LocalArtifactCache.Enumerate();
      return await r.ToArrayAsync();
    }

    public async Task<BcArtifact[]> GetAll(string versionPrefix, string country, bool local = false)
    {
      var artifacts = local ? GetAllLocal() : GetAllRemote();
      var result = (await artifacts)
        .Where(a => string.IsNullOrEmpty(country) || a.Country.Equals(country, StringComparison.OrdinalIgnoreCase))
        .Where(a => string.IsNullOrEmpty(versionPrefix) || a.Version.ToString().StartsWith(versionPrefix, StringComparison.OrdinalIgnoreCase));
      return result.ToArray();
    }

    public async Task<BcArtifact> GetLatestLocalFirst(string versionPrefix, string country)
    {
      var local = await GetLatest(versionPrefix, country, true);
      if (local != null) return local;
      return await GetLatest(versionPrefix, country);
    }

    public async Task<BcArtifact> GetLatest(string versionPrefix, string country, bool local = false)
    {
      var all = await GetAll(versionPrefix, country, local);
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

    public static Uri GetAccountUri(ArtifactStorageAccount account, ArtifactType artifactType)
    {
      return new Uri($"https://{account}.blob.core.windows.net/{artifactType}".ToLowerInvariant());
    }

    public Uri GetAccountUri()
    {
      return GetAccountUri(Account, ArtifactType);
    }
  }
}
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Bc.Development.Artifacts
{
  /// <summary>
  /// Reads available artifacts.
  /// </summary>
  public class ArtifactReader
  {
    /// <summary>
    /// The storage account to read from.
    /// </summary>
    public ArtifactStorageAccount Account { get; set; }

    /// <summary>
    /// Specify whether to use the CDN.
    /// </summary>
    public bool UseCdn { get; set; } = true;

    /// <summary>
    /// The artifact type to read.
    /// </summary>
    public ArtifactType ArtifactType { get; }


    /// <summary>
    /// Creates an instance.
    /// </summary>
    /// <param name="type">The artifact type to read.</param>
    /// <param name="account">The storage account to read from.</param>
    public ArtifactReader(
      ArtifactType type,
      ArtifactStorageAccount account = ArtifactStorageAccount.BcArtifacts)
    {
      Account = account;
      ArtifactType = type;
      if (Account == ArtifactStorageAccount.BcInsider && ArtifactType != ArtifactType.Sandbox)
        throw new NotSupportedException("Only Sandbox is supported for BcInsider");
    }


    /// <summary>
    /// Creates an artifact uri.
    /// </summary>
    /// <param name="account">The storage account.</param>
    /// <param name="artifactType">The artifact type.</param>
    /// <param name="version">The full artifact version.</param>
    /// <param name="country">The country.</param>
    /// <param name="useCdn">Specfiy whether to use the CDN.</param>
    /// <returns>The artifact URI.</returns>
    public static Uri MakeArtifactUri(ArtifactStorageAccount account, ArtifactType artifactType, Version version, string country, bool useCdn = true)
    {
      var ub = new UriBuilder(GetAccountUri(account, artifactType, useCdn));
      ub.Path += $"/{version}/{country}";
      return ub.Uri;
    }

    /// <summary>
    /// Creates an artifact uri.
    /// </summary>
    /// <param name="version">The full artifact version.</param>
    /// <param name="country">The country.</param>
    /// <returns>The artifact URI.</returns>
    public Uri MakeArtifactUri(Version version, string country)
    {
      return MakeArtifactUri(Account, ArtifactType, version, country, UseCdn);
    }

    private async Task<JArray> GetFile(string name)
    {
      var ub = new UriBuilder(GetAccountUri(Account, ArtifactType));
      ub.Path += $"/{name}";
      using (var wc = new HttpClient())
      {
        var response = await wc.GetAsync(ub.Uri);
        var content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        return JArray.Parse(content);
      }
    }

    /// <summary>
    /// Get a list of all available countries.
    /// </summary>
    /// <returns>The list of countries.</returns>
    public async Task<string[]> GetAllCountries()
    {
      var file = await GetFile("indexes/countries.json");
      return file
        .Select(a => a.Value<string>())
        .Where(s => !string.Equals(s, "platform", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    }

    /// <summary>
    /// Get a list of all available platforms.
    /// </summary>
    /// <returns>The list of platforms.</returns>
    public async Task<ArtifactIndexEntry[]> GetPlatforms()
    {
      var file = await GetFile("indexes/platform.json");
      return file.ToObject<ArtifactIndexEntry[]>();
    }

    /// <summary>
    /// Get the index for the given country.
    /// </summary>
    /// <param name="country">The country to get the index for.</param>
    /// <returns>The index.</returns>
    public async Task<ArtifactIndex> GetIndex(string country)
    {
      var file = await GetFile($"indexes/{country}.json");
      return new ArtifactIndex(country, file.ToObject<ArtifactIndexEntry[]>());
    }

    /// <summary>
    /// List all remote artifacts.
    /// </summary>
    /// <returns>The artifacts.</returns>
    public async Task<BcArtifact[]> GetAllRemote()
    {
      var countries = await GetAllCountries();
      var items = await Task.WhenAll(countries.Select(async country =>
      {
        var index = await GetIndex(country);
        return index.Entries
          .Select(a =>
          {
            var uri = MakeArtifactUri(a.Version, index.Country);
            return BcArtifact.FromUri(uri);
          });
      }));
      return items.SelectMany(i => i).ToArray();
    }

    private static async Task<BcArtifact[]> GetAllLocal()
    {
      var r = await LocalArtifactCache.Enumerate();
      return await r.ToArrayAsync();
    }

    /// <summary>
    /// Lists all available artifacts (local or remote) for the given parameters.
    /// </summary>
    /// <param name="versionPrefix">The version prefix to filter for.</param>
    /// <param name="country">The country to filter for.</param>
    /// <param name="local">Whether to read from local cache or from the remote account.</param>
    /// <returns>The available artifacts.</returns>
    public async Task<BcArtifact[]> GetAll(string versionPrefix, string country, bool local = false)
    {
      var artifacts = local ? GetAllLocal() : GetAllRemote();
      var result = (await artifacts)
        .Where(a => string.IsNullOrEmpty(country) || a.Country.Equals(country, StringComparison.OrdinalIgnoreCase))
        .Where(a => string.IsNullOrEmpty(versionPrefix) || a.Version.ToString().StartsWith(versionPrefix, StringComparison.OrdinalIgnoreCase));
      return result.ToArray();
    }

    /// <summary>
    /// Gets the latest locally available artifact. If none is found, the remote storage account is queried.
    /// </summary>
    /// <param name="versionPrefix">The version prefix to filter for.</param>
    /// <param name="country">The country to filter for.</param>
    /// <returns>The found artifact.</returns>
    public async Task<BcArtifact> GetLatestLocalFirst(string versionPrefix, string country)
    {
      var local = await GetLatest(versionPrefix, country, true);
      if (local != null) return local;
      return await GetLatest(versionPrefix, country);
    }

    /// <summary>
    /// Gets the latest artifact (local or remote).
    /// </summary>
    /// <param name="versionPrefix">The version prefix to filter for.</param>
    /// <param name="country">The country to filter for.</param>
    /// <param name="local">Whether to read from local cache or from the remote account.</param>
    /// <returns>The found artifact.</returns>
    public async Task<BcArtifact> GetLatest(string versionPrefix, string country, bool local = false)
    {
      var all = await GetAll(versionPrefix, country, local);
      return all.OrderByDescending(a => a.Version).FirstOrDefault();
    }

    /// <summary>
    /// Gets the next major artifact.
    /// </summary>
    /// <param name="country">The country to filter for.</param>
    /// <returns>The found artifact.</returns>
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

    /// <summary>
    /// Gets the next minor artifact.
    /// </summary>
    /// <param name="country">The country to filter for.</param>
    /// <returns>The found artifact.</returns>
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

    /// <summary>
    /// Gets the current version.
    /// </summary>
    /// <returns>The current version.</returns>
    public static async Task<Version> GetCurrentVersion()
    {
      var reader = new ArtifactReader(ArtifactType.Sandbox);
      var current = await reader.GetLatest(null, "base");
      return current.Version;
    }

    private static string MakeHost(ArtifactStorageAccount account, bool useCdn)
    {
      return !useCdn
        ? $"{account}.blob.core.windows.net"
        : $"{account}-{CdnHelper.Resolve(account)}";
    }

    /// <summary>
    /// Gets the base URI for the account and artifact type.
    /// </summary>
    /// <param name="account">The storage account.</param>
    /// <param name="artifactType">The artifact type.</param>
    /// <param name="useCdn">Specfiy whether to use the CDN.</param>
    /// <returns>The base URI.</returns>
    public static Uri GetAccountUri(ArtifactStorageAccount account, ArtifactType artifactType, bool useCdn = true)
    {
      return new Uri($"https://{MakeHost(account, useCdn)}/{artifactType}".ToLowerInvariant());
    }

    /// <summary>
    /// Gets the base URI for the current account and artifact type.
    /// </summary>
    /// <returns>The base URI.</returns>
    public Uri GetAccountUri()
    {
      return GetAccountUri(Account, ArtifactType);
    }
  }
}
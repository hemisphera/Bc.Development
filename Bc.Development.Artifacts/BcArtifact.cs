using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bc.Development.Configuration;

namespace Bc.Development.Artifacts
{
  /// <summary>
  /// A BC artifact.
  /// </summary>
  public class BcArtifact
  {
    /// <summary>
    /// Loads the artifact from the specified local folder.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="account">The (optional) account it was downloaded from.</param>
    /// <returns>The artifact.</returns>
    public static async Task<BcArtifact> FromLocalFolder(string folder, ArtifactStorageAccount? account = null)
    {
      var config = await BcContainerHelperConfiguration.Load();
      var relativePart = folder.Substring(config.BcArtifactsCacheFolder.Length + 1);
      var parts = relativePart.Split('\\', '/');
      try
      {
        return new BcArtifact
        {
          StorageAccount = account,
          Type = (ArtifactType)Enum.Parse(typeof(ArtifactType), parts[0], true),
          Version = new Version(parts[1]),
          Country = parts[2],
          Uri = null
        };
      }
      catch
      {
        return null;
      }
    }

    /// <summary>
    /// Creates an artifact from the specified URI.
    /// </summary>
    /// <param name="artifactUri">The artifact URI.</param>
    /// <returns>The artifact.</returns>
    public static BcArtifact FromUri(Uri artifactUri)
    {
      try
      {
        var host = artifactUri.Host;
        var accountType = host.Split('.')[0];
        if (host.EndsWith(".azurefd.net", StringComparison.OrdinalIgnoreCase))
          accountType = accountType.Split('-')[0];
        var pathParts = artifactUri.AbsolutePath.Split('/').SkipWhile(string.IsNullOrEmpty).ToArray();

        return new BcArtifact
        {
          StorageAccount = (ArtifactStorageAccount?)(string.IsNullOrEmpty(accountType) ? null : Enum.Parse(typeof(ArtifactStorageAccount), accountType, true)),
          Type = (ArtifactType)Enum.Parse(typeof(ArtifactType), pathParts[0], true),
          Version = new Version(pathParts[1]),
          Country = pathParts[2],
          Uri = artifactUri
        };
      }
      catch (Exception e)
      {
        throw new ArgumentException($"The supplied URI '{artifactUri}' is not a valid artifact URI.", e);
      }
    }

    /// <summary>
    /// Specifies whether this artifact is a platform artifact.
    /// </summary>
    public bool IsPlatform => Country.Equals(Defaults.PlatformIdentifier, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Specifies the version of the artifact.
    /// </summary>
    public Version Version { get; private set; }

    /// <summary>
    /// Specifies the country of the artifact.
    /// </summary>
    public string Country { get; private set; }

    /// <summary>
    /// Specifies the URI of the artifact.
    /// </summary>
    public Uri Uri { get; private set; }

    /// <summary>
    /// Specifies the storage account of the artifact.
    /// This might be null for local artifacts.
    /// </summary>
    public ArtifactStorageAccount? StorageAccount { get; private set; }

    /// <summary>
    /// The type of the artifact.
    /// </summary>
    public ArtifactType Type { get; private set; }


    private BcArtifact()
    {
    }


    /// <summary>
    /// Gets the lst date and time this artifact was used.
    /// </summary>
    /// <returns>The date and time.</returns>
    public async Task<DateTime?> GetLastUsedDate()
    {
      var localFolder = await GetLocalFolder();
      if (!localFolder.Exists) return null;
      var fi = new FileInfo(Path.Combine(localFolder.FullName, "lastused"));
      if (!fi.Exists) return null;
      using (var sr = fi.OpenText())
        return new DateTime(long.Parse(await sr.ReadLineAsync()));
    }

    /// <summary>
    /// Sets the last date and time this artifact was used.
    /// </summary>
    /// <param name="tag">The date and time to set. If null, the current date and time is used.</param>
    /// <returns>true or false, depending on whether the operation was successful.</returns>
    public async Task<bool> SetLastUsedDate(DateTime? tag = null)
    {
      var localFolder = await GetLocalFolder();
      if (!localFolder.Exists) return false;
      try
      {
        var dateTime = (tag ?? DateTime.Now).ToUniversalTime();
        using (var s = File.CreateText(Path.Combine(localFolder.FullName, "lastused")))
          await s.WriteLineAsync($"{dateTime.Ticks}");
        return true;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Creates the corresponding platform artifact from a country artifact.
    /// </summary>
    /// <returns>The platform artifact.</returns>
    public BcArtifact CreatePlatformArtifact()
    {
      if (IsPlatform) return this;
      var uri = ArtifactReader.MakeArtifactUri(
        StorageAccount ?? Defaults.DefaultStorageAccount,
        Type,
        Version,
        Defaults.PlatformIdentifier);
      return FromUri(uri);
    }

    /// <summary>
    /// Gets the expected local cache folder for this artifact.
    /// </summary>
    /// <returns>The expected folder.</returns>
    public async Task<DirectoryInfo> GetLocalFolder()
    {
      var config = await BcContainerHelperConfiguration.Load();
      var folder = Path.Combine(
        config.BcArtifactsCacheFolder,
        Type.ToString().ToLowerInvariant(),
        Version.ToString(),
        Country);
      return new DirectoryInfo(folder);
    }


    /// <inheritdoc />
    public override string ToString()
    {
      return $"{Type} {Version} ({Country})";
    }
  }
}
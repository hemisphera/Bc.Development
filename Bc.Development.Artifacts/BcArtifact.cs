using System;
using System.IO;
using System.Threading.Tasks;
using Bc.Development.Configuration;

namespace Bc.Development.Artifacts
{

  public class BcArtifact
  {

    public static BcArtifact FromLocalFolder(string folder)
    {
      var relativePart = folder.Substring(BcContainerHelperConfiguration.Current.BcArtifactsCacheFolder.Length + 1);
      var parts = relativePart.Split('\\', '/');
      return new BcArtifact
      {
        StorageAccount = null,
        Type = (ArtifactType)Enum.Parse(typeof(ArtifactType), parts[0], true),
        Version = new Version(parts[1]),
        Country = parts[2],
        Uri = null
      };
    }

    public static BcArtifact FromUri(Uri artifactUri)
    {
      var parts = $"{artifactUri}".Split('/');
      var accountType = parts[2].Split('.')[0];

      return new BcArtifact
      {
        StorageAccount = String.IsNullOrEmpty(accountType) ? null : Enum.Parse(typeof(ArtifactStorageAccount), accountType, true),
        Type = (ArtifactType)Enum.Parse(typeof(ArtifactType), parts[3], true),
        Version = new Version(parts[4]),
        Country = parts[5],
        Uri = artifactUri
      };
    }


    public bool IsPlatform => Country.Equals(ArtifactReader.PlatformIdentifier, StringComparison.OrdinalIgnoreCase);

    public Version Version { get; private set; }

    public string Country { get; private set; }

    public Uri Uri { get; private set; }

    public object StorageAccount { get; private set; }

    public ArtifactType Type { get; private set; }

    public string LocalFolder => Path.Combine(
      BcContainerHelperConfiguration.Current.BcArtifactsCacheFolder,
      Type.ToString().ToLowerInvariant(),
      Version.ToString(),
      Country);


    private BcArtifact()
    {
    }


    public override string ToString()
    {
      return $"{Version} ({Country})";
    }

    public async Task<DateTime?> GetLastUsedDate()
    {
      var di = new DirectoryInfo(LocalFolder);
      if (!di.Exists) return null;
      var fi = new FileInfo(Path.Combine(di.FullName, "lastused"));
      if (!fi.Exists) return null;
      using (var sr = fi.OpenText())
        return new DateTime(long.Parse(await sr.ReadLineAsync()));
    }

  }

}
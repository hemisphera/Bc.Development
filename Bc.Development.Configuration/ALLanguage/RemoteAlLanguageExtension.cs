using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ArgumentException = System.ArgumentException;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// A version of the AL language extension that is available on the marketplace.
  /// </summary>
  public class RemoteAlLanguageExtension
  {
    /// <summary>
    /// The version of the extension.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Indicates whether this is a prerelease version.
    /// </summary>
    public bool PreRelease { get; }

    /// <summary>
    /// Indicates when this version was last updated.
    /// </summary>
    public DateTime LastUpdated { get; }

    /// <summary>
    /// The URI to download this version.
    /// </summary>
    public Uri? Uri { get; }


    private RemoteAlLanguageExtension(JObject jo)
    {
      var uri = (jo["files"] as JArray)?.FirstOrDefault()?.Value<string>("source");

      Version = Version.Parse(jo.Value<string>("version") ?? throw new ArgumentException("Version is missing"));
      LastUpdated = jo.Value<DateTime>("lastUpdated");
      Uri = string.IsNullOrEmpty(uri) ? null : new Uri(uri);
      PreRelease = (jo["properties"] as JArray)?
        .FirstOrDefault(f => f.Value<string>("key")?.Equals("Microsoft.VisualStudio.Code.PreRelease", StringComparison.OrdinalIgnoreCase) == true)
        ?.Value<bool>("value") ?? false;
    }


    /// <summary>
    /// Enumerates all versions of the AL language extension that are available on the marketplace.
    /// </summary>
    /// <param name="includePreRelease">Indicates whether prerelease versions should be included.</param>
    /// <returns>All versions of the AL language extension that are available on the marketplace.</returns>
    public static async Task<RemoteAlLanguageExtension[]> Enumerate(bool includePreRelease = false)
    {
      const string marketplaceUri =
        "https://marketplace.visualstudio.com/_apis/public/gallery/extensionquery?api-version=3.0-preview.1";
      var idx = await GetIndex(marketplaceUri);
      var versions = idx.SelectToken("results[0].extensions[0].versions") as JArray;
      return versions?
        .Select(FromJToken)
        .OfType<RemoteAlLanguageExtension>()
        .Where(a => includePreRelease || !a.PreRelease)
        .ToArray() ?? Array.Empty<RemoteAlLanguageExtension>();
    }

    private static RemoteAlLanguageExtension? FromJToken(JToken tk)
    {
      try
      {
        if (!(tk is JObject jo)) return null;
        return new RemoteAlLanguageExtension(jo);
      }
      catch
      {
        return null;
      }
    }

    private static async Task<JObject> GetIndex(string marketplaceUri)
    {
      var cl = new HttpClient();

      var filters = new Dictionary<int, string>
      {
        { 8, "Microsoft.VisualStudio.Code" },
        { 12, "4096" },
        { 7, "ms-dynamics-smb.al" }
      };

      using (var sw = new StringWriter())
      using (var jw = new JsonTextWriter(sw))
      {
        await jw.WriteStartObjectAsync();
        await jw.WritePropertyNameAsync("filters");
        await jw.WriteStartArrayAsync();

        await jw.WriteStartObjectAsync();

        // criteria
        await jw.WritePropertyNameAsync("criteria");
        await jw.WriteStartArrayAsync();
        foreach (var filter in filters)
        {
          await jw.WriteStartObjectAsync();
          await jw.WritePropertyNameAsync("filterType");
          await jw.WriteValueAsync(filter.Key);
          await jw.WritePropertyNameAsync("value");
          await jw.WriteValueAsync(filter.Value);
          await jw.WriteEndObjectAsync();
        }

        await jw.WriteEndArrayAsync();

        // properties
        await jw.WritePropertyNameAsync("pageNumber");
        await jw.WriteValueAsync(1);
        await jw.WritePropertyNameAsync("pageSize");
        await jw.WriteValueAsync(50);

        await jw.WriteEndObjectAsync();
        await jw.WriteEndArrayAsync();

        await jw.WritePropertyNameAsync("assetTypes");
        await jw.WriteStartArrayAsync();
        await jw.WriteValueAsync("Microsoft.VisualStudio.Services.VSIXPackage");
        await jw.WriteEndArrayAsync();

        await jw.WritePropertyNameAsync("flags");
        await jw.WriteValueAsync(0x192);

        await jw.WriteEndObjectAsync();
        await jw.FlushAsync();

        var response =
          await cl.PostAsync(
            new Uri(marketplaceUri),
            new StringContent(sw.ToString(), Encoding.UTF8, "application/json"));
        return JObject.Parse(await response.Content.ReadAsStringAsync());
      }
    }


    /// <summary>
    /// Downloads the AL language extension to the specified folder.
    /// </summary>
    /// <param name="targetFolder">The folder to download the extension to.</param>
    /// <returns>All available instances in the downloaded AL language extension.</returns>
    public async Task<AlLanguageExtension[]> Download(string targetFolder)
    {
      var cl = new HttpClient();
      using (var fs = await cl.GetStreamAsync(Uri))
      using (var ms = new MemoryStream())
      {
        await fs.CopyToAsync(ms);
        ms.Position = 0;
        using (var zf = ZipFile.Read(ms))
        {
          foreach (var entry in zf.Entries)
          {
            var parts = entry.FileName.Split('/');
            if (parts.FirstOrDefault() != "extension") continue;
            var file = new FileInfo(Path.Combine(targetFolder, string.Join("/", entry.FileName.Split('/').Skip(1))));
            file.Directory?.Create();
            using (var targetFile = file.Create())
            using (var sourceFile = entry.OpenReader())
            {
              await sourceFile.CopyToAsync(targetFile);
              targetFile.Close();
            }
          }

          return AlLanguageExtension.Enumerate(null, targetFolder);
        }
      }
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return $"{Version}";
    }
  }
}
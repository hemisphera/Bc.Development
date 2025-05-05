using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Bc.Development.Artifacts
{
  [JsonObject]
  public class ArtifactIndexEntry
  {
    [JsonProperty("Version")]
    [JsonConverter(typeof(VersionConverter))]
    public Version Version { get; set; }

    [JsonProperty("CreationTime")]
    public DateTime CreationTime { get; set; }
  }
}
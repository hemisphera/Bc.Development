using System;

namespace Bc.Development.Artifacts
{
  public class ArtifactIndex
  {
    public string Country { get; }
    public ArtifactIndexEntry[] Entries { get; }

    public ArtifactIndex(string country, ArtifactIndexEntry[] entries)
    {
      Country = country;
      Entries = entries;
    }
  }
}
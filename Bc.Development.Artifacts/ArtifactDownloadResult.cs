using System;
using System.Threading.Tasks;

namespace Bc.Development.Artifacts
{

  public class ArtifactDownloadResult
  {

    public BcArtifact Artifact { get; set; }

    public BcArtifact PlatformArtifact { get; set; }


    public async Task SetLastUsedDate(DateTime? tag = null)
    {
      await Task.WhenAll(
        Task.Run(async () =>
        {
          if (Artifact != null) await Artifact.SetLastUsedDate(tag);
        }),
        Task.Run(async () =>
        {
          if (PlatformArtifact != null) await PlatformArtifact.SetLastUsedDate(tag);
        })
      );
    }

  }

}
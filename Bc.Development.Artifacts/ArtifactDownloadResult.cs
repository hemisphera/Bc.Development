using System;
using System.Threading.Tasks;

namespace Bc.Development.Artifacts
{
  /// <summary>
  /// The result of an artifact download.
  /// </summary>
  public class ArtifactDownloadResult
  {
    /// <summary>
    /// The downloaded artifact.
    /// </summary>
    public BcArtifact Artifact { get; set; }

    /// <summary>
    /// The downloaded platform artifact.
    /// </summary>
    public BcArtifact PlatformArtifact { get; set; }


    /// <summary>
    /// Updates the last used date for the artifact and platform artifact.
    /// </summary>
    /// <param name="tag">The date and time to set. If null, the current date and time is used.</param>
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
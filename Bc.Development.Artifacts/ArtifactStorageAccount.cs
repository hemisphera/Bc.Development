namespace Bc.Development.Artifacts
{
  /// <summary>
  /// The artifact storage account.
  /// </summary>
  public enum ArtifactStorageAccount
  {
    /// <summary>
    /// The default artifact storage account.
    /// </summary>
    BcArtifacts,

    /// <summary>
    /// The storage account for insider builds.
    /// </summary>
    BcInsider,

    /// <summary>
    /// The storage account for the public preview.
    /// </summary>
    BcPublicPreview
  }
}
namespace Bc.Development.Artifacts
{
  /// <summary>
  /// Global defaults.
  /// </summary>
  public static class Defaults
  {
    /// <summary>
    /// The default storage account.
    /// </summary>
    public const ArtifactStorageAccount DefaultStorageAccount = ArtifactStorageAccount.BcArtifacts;

    /// <summary>
    /// The default country.
    /// </summary>
    public const string DefaultCountry = "w1";

    /// <summary>
    /// The country identifier for platform artifacts.
    /// </summary>
    public const string PlatformIdentifier = "platform";
  }
}
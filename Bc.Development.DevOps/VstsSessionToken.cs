using System;

namespace Bc.Development.DevOps
{
  /// <summary>
  /// The VSTS session token.
  /// </summary>
  public class VstsSessionToken
  {
    /// <summary>
    /// The display name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// The scope of the token.
    /// </summary>
    public string Scope { get; set; }

    /// <summary>
    /// The expiration date.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// The token.
    /// </summary>
    public string Token { get; set; }
  }
}
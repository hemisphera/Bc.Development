using System;

namespace Bc.Development.Configuration
{
  /// <summary>
  /// URI helper methods.
  /// </summary>
  public static class UriHelper
  {
    /// <summary>
    /// Normalizes a BC server URI to remove the port number, paths and andy query strings.
    /// </summary>
    /// <param name="serverUri">The server URI.</param>
    /// <returns>The normalized URI.</returns>
    public static Uri NormalizeBcServerUri(this Uri serverUri)
    {
      return new UriBuilder
      {
        Scheme = serverUri.Scheme,
        Host = serverUri.Host
      }.Uri;
    }
  }
}
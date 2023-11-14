using System;

namespace Bc.Development.Configuration
{
  public static class UriHelper
  {
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
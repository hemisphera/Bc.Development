using System;
using System.Collections.Generic;
using System.Linq;

namespace Bc.Development.Artifacts
{
  internal static class CdnHelper
  {
    private static readonly Dictionary<ArtifactStorageAccount, string> CdnMap = new Dictionary<ArtifactStorageAccount, string>
    {
      { ArtifactStorageAccount.BcArtifacts, "exdbf9fwegejdqak.b02.azurefd.net" },
      { ArtifactStorageAccount.BcPublicPreview, "f2ajahg0e2cudpgh.b02.azurefd.net" },
      { ArtifactStorageAccount.BcInsider, "fvh2ekdjecfjd6gk.b02.azurefd.net" }
    };

    public static string Resolve(ArtifactStorageAccount account)
    {
      return CdnMap[account];
    }

    public static Uri ResolveUri(Uri uri)
    {
      var ub = new UriBuilder(uri);
      var host = ub.Host;
      if (!Enum.TryParse<ArtifactStorageAccount>(ub.Host.Split('.').First(), true, out var acc))
        return uri;
      ub.Host = $"{acc.ToString().ToLower()}-{Resolve(acc)}";
      return ub.Uri;
    }
  }
}
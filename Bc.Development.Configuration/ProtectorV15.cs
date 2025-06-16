using System.IO;
using Microsoft.AspNetCore.DataProtection;

namespace Bc.Development.Configuration
{
  internal class ProtectorV15 : ICacheEncryption
  {
    private readonly IDataProtector _dataProtector;

    public ProtectorV15(string filePath)
    {
      var folder = Path.GetDirectoryName(filePath);
      if (folder == null) throw new DirectoryNotFoundException("The folder of the file could not be found.");
      var provider = DataProtectionProvider.Create(new DirectoryInfo(folder));
      _dataProtector = provider.CreateProtector(new[]
      {
        "Microsoft.Dynamics.Nav.Deployment",
        Path.GetFileName(filePath)
      });
    }

    public byte[] Encrypt(byte[] data)
    {
      return _dataProtector.Protect(data);
    }

    public byte[] Decrypt(byte[] data)
    {
      return _dataProtector.Unprotect(data);
    }
  }
}
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Bc.Development.Configuration
{
  internal class ProtectorV16 : ICacheEncryption
  {
    public byte[] Encrypt(byte[] data)
    {
      return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
    }

    public byte[] Decrypt(byte[] data)
    {
      return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
    }
  }
}
using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Bc.Development.Configuration
{
  internal class UserProtectedFileStorage
  {
    private static readonly object FileLock = new object();

    private IDataProtector DataProtector { get; }

    private string FilePath { get; }

    private IDataProtectionProvider Provider { get; }


    public UserProtectedFileStorage(string fileName)
    {
      FilePath = fileName;
      var folder = Path.GetDirectoryName(fileName);
      if (folder == null) throw new DirectoryNotFoundException("The folder of the file could not be found.");
      fileName = Path.GetFileName(fileName);

      Provider = DataProtectionProvider.Create(new DirectoryInfo(folder));
      DataProtector = Provider.CreateProtector(new[]
      {
        "Microsoft.Dynamics.Nav.Deployment",
        fileName
      });
    }


    public static UserProtectedFileStorage CreateUserPasswordCache(string path)
    {
      return new UserProtectedFileStorage(path);
    }

    public bool Exists() => File.Exists(FilePath);

    public byte[]? Read()
    {
      lock (FileLock)
        return Exists() ? Unprotect(File.ReadAllBytes(FilePath)) : null;
    }

    public T? Read<T>() where T : class
    {
      var bytes = Read();
      return bytes == null ? null : JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
    }

    public void Write(byte[] bytes)
    {
      File.WriteAllBytes(FilePath, Protect(bytes));
    }

    public bool Clear()
    {
      if (Exists())
        File.Delete(FilePath);
      return true;
    }

    private byte[] Protect(byte[] data) => DataProtector.Protect(data);

    private byte[] Unprotect(byte[] data) => DataProtector.Unprotect(data);
  }
}
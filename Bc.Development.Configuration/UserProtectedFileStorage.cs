﻿using System.IO;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Bc.Development.Configuration
{
  internal class UserProtectedFileStorage
  {
    private static readonly object FileLock = new object();

    private Encoding Encoding { get; } = Encoding.UTF8;

    private IDataProtector DataProtector { get; }

    private string FilePath { get; }

    private IDataProtectionProvider Provider { get; }


    public UserProtectedFileStorage(string fileName)
    {
      FilePath = fileName;
      var folder = Path.GetDirectoryName(fileName);
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

    public byte[] Read()
    {
      lock (FileLock)
        return Exists() ? Unprotect(File.ReadAllBytes(FilePath)) : null;
    }

    public T Read<T>()
    {
      var bytes = Read();
      return bytes == null ? default : JsonConvert.DeserializeObject<T>(Encoding.GetString(bytes));
    }

    public void Write(byte[] bytes)
    {
      File.WriteAllBytes(FilePath, Protect(bytes));
    }

    public void Write(object obj) => Write(Encoding.GetBytes(JsonConvert.SerializeObject(obj)));

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
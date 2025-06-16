using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Bc.Development.Configuration
{
  internal class UserProtectedFileStorage
  {
    private static readonly object FileLock = new object();

    private readonly ICacheEncryption _encryption;

    private string FilePath { get; }


    public UserProtectedFileStorage(string filePath, ICacheEncryption encryption)
    {
      _encryption = encryption;
      FilePath = filePath;
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

    private byte[] Protect(byte[] data)
    {
      lock (FileLock)
        return _encryption.Encrypt(data);
    }

    private byte[] Unprotect(byte[] data)
    {
      lock (FileLock)
        return _encryption.Decrypt(data);
    }
  }
}
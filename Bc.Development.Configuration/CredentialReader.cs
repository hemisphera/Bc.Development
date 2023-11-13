using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Bc.Development.Configuration
{
  internal class CredentialReader
  {
    public string Filename { get; }


    private CredentialReader(string filename)
    {
      Filename = filename;
      //if (!Path.IsPathRooted(Filename))
      //  Filename = Path.Combine(AlLanguageExtension.Latest.Folder.FullName, filename);
    }

    public static CredentialReader Create(string filename)
    {
      return new CredentialReader(filename);
    }

    public static CredentialReader CreateUserPasswordCache(string folder = null)
    {
      const string filename = "UserPasswordCache.dat";
      return new CredentialReader(!string.IsNullOrEmpty(folder) ? Path.Combine(folder, filename) : filename);
    }

    public static CredentialReader CreateServerInfoCache(string folder = null)
    {
      const string filename = "ServerInfoCache.dat";
      return new CredentialReader(!string.IsNullOrEmpty(folder) ? Path.Combine(folder, filename) : filename);
    }

    public static CredentialReader CreateTokenKeyCache(string folder = null)
    {
      const string filename = "TokenKeyCache.dat";
      return new CredentialReader(!string.IsNullOrEmpty(folder) ? Path.Combine(folder, filename) : filename);
    }


    private static string CreateCredentialsKey(string server, string serverInstance) => server.ToLowerInvariant() + "_" + serverInstance.ToLowerInvariant();

    public NetworkCredential TryGetSavedCredentials(string server, string serverInstance)
    {
      server = server.TrimEnd('/');
      if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(serverInstance))
        return null;
      return TryGetSavedCredentials(CreateCredentialsKey(server, serverInstance));
    }

    public NetworkCredential TryGetSavedCredentials(string credentialsKey)
    {
      var dictionary = new UserProtectedFileStorage(Filename).Read<Dictionary<string, UsernamePassword>>();
      if (dictionary == null)
        return null;
      return dictionary.TryGetValue(credentialsKey, out var savedCredentials) ? savedCredentials.AsNetworkCredential() : null;
    }

    public IEnumerable<string> ListStoredCredentials()
    {
      var dictionary = new UserProtectedFileStorage(Filename).Read<Dictionary<string, UsernamePassword>>();
      return dictionary.Keys;
    }
  }
}
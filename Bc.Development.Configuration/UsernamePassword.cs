using System.Net;

namespace Bc.Development.Configuration
{
  internal sealed class UsernamePassword
  {
    public string Username { get; set; }

    public string Password { get; set; }


    public NetworkCredential AsNetworkCredential()
    {
      return new NetworkCredential(Username, Password);
    }
  }
}
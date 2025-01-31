using System;
using System.Net;

namespace Bc.Development.Configuration
{
  internal sealed class UsernamePassword
  {
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;


    public NetworkCredential AsNetworkCredential()
    {
      return new NetworkCredential(Username, Password);
    }
  }
}
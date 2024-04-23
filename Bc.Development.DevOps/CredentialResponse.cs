using System.Net;

namespace Bc.Development.DevOps
{
  /// <summary>
  /// Represents the response to a credential request.
  /// </summary>
  public sealed class CredentialResponse
  {
    /// <summary>
    /// Creates a response that indicates failure.
    /// </summary>
    public static CredentialResponse CreateFailure() => new CredentialResponse(null, false);

    /// <summary>
    /// Creates a response that indicates cancellation.
    /// </summary>
    public static CredentialResponse CreateCancellation => new CredentialResponse(null, null);

    /// <summary>
    /// Creates a response that indicates success.
    /// </summary>
    public static CredentialResponse Create(NetworkCredential credential) => new CredentialResponse(credential);

    /// <summary>
    /// The credentials that were provided.
    /// </summary>
    public NetworkCredential Credential { get; }

    /// <summary>
    /// The response that was given.
    /// True indicates success, False indicates failure, null indicates cancellation.
    /// </summary>
    public bool? Success { get; }


    private CredentialResponse(NetworkCredential credential, bool? success = true)
    {
      Credential = credential;
      Success = success;
    }
  }
}
namespace Bc.Development.Configuration
{
  internal interface ICacheEncryption
  {
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
  }
}
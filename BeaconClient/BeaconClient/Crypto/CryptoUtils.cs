using System;
using System.Security.Cryptography;
using System.Text;

namespace BeaconClient.Crypto
{
    public static class CryptoUtils
    {
        public static AES256Key DeriveMasterKey(string password, string email)
        {
            byte[] emailBytes = Encoding.UTF8.GetBytes(email);
            Rfc2898DeriveBytes deriver = new Rfc2898DeriveBytes(password, emailBytes);

            byte[] key = deriver.GetBytes(32);

            return new AES256Key(key);
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;

namespace BeaconClient.Crypto
{
    public class AES256Key
    {
        private AesCryptoServiceProvider provider;

        private void InitialiseProvider()
        {
            provider = new AesCryptoServiceProvider();
            provider.KeySize = 256;
        }

        public AES256Key()
        {
            InitialiseProvider();
            provider.GenerateKey();
        }

        public AES256Key(byte[] key)
        {
            InitialiseProvider();
            provider.Key = key;
        }

        ~AES256Key()
        {
            provider.Dispose();
        }

        public byte[] IV => provider.IV;

        public byte[] EncryptStringToBytes(string plainText)
        {
            // TODO do checks on args

            provider.GenerateIV();

            byte[] encrypted;

            ICryptoTransform encryptor = provider.CreateEncryptor();

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                encrypted = msEncrypt.ToArray();
                }
            }

            return encrypted;
        }

        public string DecryptBytesToString(byte[] cipherText, byte[] iv)
        {
            // TODO do checks on args

            provider.IV = iv;

            string plainText;

            ICryptoTransform decryptor = provider.CreateDecryptor();

            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        plainText = srDecrypt.ReadToEnd();
                    }
                }
            }

            return plainText;
        }
    }
}

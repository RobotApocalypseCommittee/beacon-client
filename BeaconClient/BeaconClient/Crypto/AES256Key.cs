using System;
using System.IO;
using System.Security.Cryptography;

namespace BeaconClient.Crypto
{
    public class AES256Key
    {
        private AesCryptoServiceProvider _provider;

        private void InitialiseProvider()
        {
            _provider = new AesCryptoServiceProvider {KeySize = 256};
        }

        public AES256Key()
        {
            InitialiseProvider();
            _provider.GenerateKey();
        }

        public AES256Key(byte[] key)
        {
            InitialiseProvider();
            _provider.Key = key;
        }

        ~AES256Key()
        {
            _provider.Dispose();
        }

        public byte[] IV => _provider.IV;

        public byte[] Encrypt(byte[] plainText, int offset = 0)
        {
            // TODO do checks on args
            
            byte[] encrypted;
            
            _provider.GenerateIV();
            ICryptoTransform encryptor = _provider.CreateEncryptor();

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(plainText, offset, plainText.Length-offset);
                }

                encrypted = msEncrypt.ToArray();
            }

            return encrypted;
        }

        public byte[] Encrypt(string plainText)
        {
            // TODO do checks on args
            
            byte[] encrypted;

            _provider.GenerateIV();
            ICryptoTransform encryptor = _provider.CreateEncryptor();

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                }
                
                encrypted = msEncrypt.ToArray();
            }

            return encrypted;
        }

        public byte[] DecryptBytes(byte[] cipherText, byte[] iv)
        {
            // TODO do checks on args

            byte[] plainText;

            _provider.IV = iv;
            ICryptoTransform decryptor = _provider.CreateDecryptor();

            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (BinaryReader brDecrypt = new BinaryReader(csDecrypt))
                    {
                        plainText = brDecrypt.ReadBytes(cipherText.Length);
                    }
                }
            }

            return plainText;
        }

        public string DecryptString(byte[] cipherText, byte[] iv)
        {
            // TODO do checks on args
            
            string plainText;

            _provider.IV = iv;
            ICryptoTransform decryptor = _provider.CreateDecryptor();

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

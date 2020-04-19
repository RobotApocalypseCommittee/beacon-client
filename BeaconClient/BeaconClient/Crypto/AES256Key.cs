using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public byte[] Key => _provider.Key;

        public byte[] Encrypt(byte[] plainText, byte[] iv, int offset = 0)
        {
            // TODO do checks on args
            
            byte[] encrypted;

            if (iv is null)
            {
                _provider.GenerateIV();
            }
            else
            {
                _provider.IV = iv;
            }

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

        // Returns (cipher text, iv) tuple
        public (byte[], byte[]) Encrypt(byte[] plainText, int offset = 0)
        {
            _provider.GenerateIV();
            return (Encrypt(plainText, _provider.IV, offset), _provider.IV);
        }

        public byte[] Decrypt(byte[] cipherText, byte[] iv)
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

        public byte[] CalculateHmac256Hash(byte[] inputData)
        {
            HMACSHA256 hmac = new HMACSHA256(_provider.Key);
            return hmac.ComputeHash(inputData);
        }
    }
}

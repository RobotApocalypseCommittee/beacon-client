using System;
using System.Linq;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC.Rfc7748;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.Security;

namespace BeaconClient.Crypto
{
    public class Curve25519KeyPair
    {
        // The benefit here is that ONLY the Ed25519 keys need to be stored, as the X ones can be derived from their respective keys
        
        private readonly byte[] _edPrivateKey;
        private readonly byte[] _XprivateKey;

        // This is the !!! Ed25519 !!! public key
        private readonly byte[] _edPublicKey;
        
        // This is the !!! X25519 !!! public key
        private readonly byte[] _XpublicKey;
        
        public byte[] EdPrivateKey => _edPrivateKey;
        public byte[] XPrivateKey => _XprivateKey;
        public byte[] EdPublicKey => _edPublicKey;
        public byte[] XPublicKey => _XpublicKey;
        
        public Curve25519KeyPair()
        {
            byte[] privateKey;
            byte[] edPublicKey;
            byte[] xPrivateKey;
            byte[] xPublicKey;
            
            // Generates keys until the conversion algorithm works for that key (there is probably a <1/100000 chance of this, but hey)
            while (true)
            {
                privateKey = new byte[Ed25519PrivateKeyParameters.KeySize];
                Ed25519.GeneratePrivateKey(new SecureRandom(), privateKey);

                edPublicKey = new byte[Ed25519PublicKeyParameters.KeySize];
                Ed25519.GeneratePublicKey(privateKey, 0, edPublicKey, 0);

                xPrivateKey = ConvertEdPrivateKeyToMontgomery(privateKey);

                xPublicKey = new byte[X25519PublicKeyParameters.KeySize];
                X25519.GeneratePublicKey(xPrivateKey, 0, xPublicKey, 0);
                
                if (xPublicKey.SequenceEqual(ConvertEdPublicKeyToMontgomery(edPublicKey)))
                {
                    break;
                }
            }

            _edPrivateKey = privateKey;
            _XprivateKey = xPrivateKey;
            
            _edPublicKey = edPublicKey;
            _XpublicKey = xPublicKey;
        }

        public Curve25519KeyPair(byte[] key, bool isPrivate)
        {
            // MUST be an Ed25519 key!
            if (isPrivate)
            {
                _edPrivateKey = key;
                
                byte[] xPrivateKey = ConvertEdPrivateKeyToMontgomery(key);
                
                byte[] edPublicKey = new byte[Ed25519PublicKeyParameters.KeySize];
                Ed25519.GeneratePublicKey(key, 0, edPublicKey, 0);
            
                byte[] xPublicKey = new byte[X25519PublicKeyParameters.KeySize];
                X25519.GeneratePublicKey(xPrivateKey, 0, xPublicKey, 0);

                if (!xPublicKey.SequenceEqual(ConvertEdPublicKeyToMontgomery(edPublicKey)))
                {
                    throw new CryptographicException("The supplied Ed25519 private key is invalid for use: please use another.");
                }

                _XprivateKey = xPrivateKey;
                
                _edPublicKey = edPublicKey;
                _XpublicKey = xPublicKey;
            }
            else
            {
                _edPublicKey = key;
                _edPrivateKey = null;
                
                _XpublicKey = ConvertEdPublicKeyToMontgomery(key);
                _XprivateKey = null;
            }
        }

        private byte[] ConvertEdPrivateKeyToMontgomery(byte[] edPrivateKey)
        {
            SHA512 hasher = SHA512.Create();

            byte[] output = hasher.ComputeHash(edPrivateKey);

            output[0] &= 248;
            output[31] &= 127;
            output[31] |= 64;
            return output.Take(X25519PrivateKeyParameters.KeySize).ToArray();
        }

        private byte[] ConvertEdPublicKeyToMontgomery(byte[] edPublicKey)
        {
            int[] x = X25519Field.Create();
            int[] oneMinusY = X25519Field.Create();
            int[] aY = new int[X25519Field.Size];
            X25519Field.Decode(edPublicKey, 0, aY);
            
            X25519Field.One(oneMinusY);
            X25519Field.Sub(oneMinusY, aY, oneMinusY);
            X25519Field.One(x);
            X25519Field.Add(x, aY, x);
            X25519Field.Inv(oneMinusY, oneMinusY);
            X25519Field.Mul(x, oneMinusY, x);
            
            byte[] xpublicKey = new byte[X25519PublicKeyParameters.KeySize];
            X25519Field.Encode(x, xpublicKey, 0);
            
            // UPDATE: the reality of the situation is that it's OK to check if it works, because the vast, vast majority work right off the bat, and it takes very little time to generate
            // I have no idea, but it has always worked....: I am very unhappy with it (tested 1m times, didn't fail). But since I don't trust it, I check it too (when it is possible)
            if (xpublicKey[X25519PublicKeyParameters.KeySize - 1] >= 128)
            {
                // Take off 128
                xpublicKey[X25519PublicKeyParameters.KeySize - 1] -= 128;
                // We need to add 19 as well...
                xpublicKey[0] = (byte) ((xpublicKey[0] + 19) % 256);
                if (xpublicKey[0] < 19)
                {
                    int index = 0;
                    while (true)
                    {
                        index++;
                        if (xpublicKey[index] != 255)
                        {
                            xpublicKey[index] += 1;
                            break;
                        }

                        xpublicKey[index] = 0;
                    }
                }
            }
            
            return xpublicKey;
        }

        public byte[] CalculateSharedSecret(byte[] otherPublicKey)
        {
            var output = new byte[32];
            X25519.CalculateAgreement(_XprivateKey, 0, otherPublicKey, 0, output, 0);
            
            return output;
        }

        public byte[] Sign(byte[] message)
        {
            // This is Ed25519.PointBytes + Ed25519.ScalarBytes
            byte[] sig = new byte[64];
            Ed25519.Sign(_edPrivateKey, 0, message, 0, message.Length, sig, 0);

            return sig;
        }

        public bool Verify(byte[] message, byte[] signature)
        {
            return Ed25519.Verify(signature, 0, _edPublicKey, 0, message, 0, message.Length);
        }
    }
}

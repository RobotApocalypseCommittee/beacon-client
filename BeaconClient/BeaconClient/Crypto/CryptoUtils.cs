using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace BeaconClient.Crypto
{
    public static class CryptoUtils
    {
        public static readonly BigInteger DhP = new BigInteger("FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200CBBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFCE0FD108E4B82D120A93AD2CAFFFFFFFFFFFFFFFF", 16);
        public static readonly BigInteger DhG = new BigInteger("2", 10);

        private static readonly byte[] ThirtyTwoFFs =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        private static readonly byte[] ThirtyTwoZeros =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        private const string ProtocolInfo = "BeaconClientv1.0.0";
        
        public static AES256Key DeriveMasterKey(string password, string email)
        {
            byte[] emailBytes = Encoding.UTF8.GetBytes(email);
            Rfc2898DeriveBytes deriver = new Rfc2898DeriveBytes(password, emailBytes, 16384);

            byte[] key = deriver.GetBytes(32);

            return new AES256Key(key);
        }

        public static byte[] X3DhKdf(byte[] input, byte[] info)
        {
            byte[] ikm = ThirtyTwoFFs.Concat(input).ToArray();

            HkdfParameters parameters = new HkdfParameters(ikm, ThirtyTwoZeros, info);
            HkdfBytesGenerator generator = new HkdfBytesGenerator(new Sha256Digest());
            generator.Init(parameters);
            
            byte[] output = new byte[256/8];
            generator.GenerateBytes(output, 0, output.Length);

            return output;
        }

        public static AES256Key DeriveX3DhSecretSender(Curve25519KeyPair selfIdentityKeyPair, Curve25519KeyPair ephemeralKeyPair,
            Curve25519KeyPair otherIdentityKey, Curve25519KeyPair otherSignedPreKey,
            Curve25519KeyPair otherOneTimePreKey = null)
        {
            byte[] dh1 = selfIdentityKeyPair.CalculateSharedSecret(otherSignedPreKey);
            byte[] dh2 = ephemeralKeyPair.CalculateSharedSecret(otherIdentityKey);
            byte[] dh3 = ephemeralKeyPair.CalculateSharedSecret(otherSignedPreKey);

            IEnumerable<byte> dhConcat = dh1.Concat(dh2).Concat(dh3);

            if (!(otherOneTimePreKey is null))
            {
                byte[] dh4 = ephemeralKeyPair.CalculateSharedSecret(otherOneTimePreKey);
                dhConcat = dhConcat.Concat(dh4);
            }

            byte[] sharedSecret = X3DhKdf(dhConcat.ToArray(), Encoding.UTF8.GetBytes(ProtocolInfo));
            return new AES256Key(sharedSecret);
        }

        public static AES256Key DeriveX3DhSecretReceiver(Curve25519KeyPair selfIdentityKeyPair,
            Curve25519KeyPair selfSignedPreKeyPair, Curve25519KeyPair otherIdentityKey, Curve25519KeyPair ephemeralKey,
            Curve25519KeyPair selfOneTimePreKeyPair = null)
        {
            byte[] dh1 = selfSignedPreKeyPair.CalculateSharedSecret(otherIdentityKey);
            byte[] dh2 = selfIdentityKeyPair.CalculateSharedSecret(ephemeralKey);
            byte[] dh3 = selfSignedPreKeyPair.CalculateSharedSecret(ephemeralKey);

            IEnumerable<byte> dhConcat = dh1.Concat(dh2).Concat(dh3);

            if (!(selfOneTimePreKeyPair is null))
            {
                byte[] dh4 = selfOneTimePreKeyPair.CalculateSharedSecret(ephemeralKey);
                dhConcat = dhConcat.Concat(dh4);
            }

            byte[] sharedSecret = X3DhKdf(dhConcat.ToArray(), Encoding.UTF8.GetBytes(ProtocolInfo));
            return new AES256Key(sharedSecret);
        }

        public static byte[] CalculateAssociatedData(Curve25519KeyPair identityKeyA,
            Curve25519KeyPair identityKeyB)
        {
            return identityKeyA.XPublicKey.Concat(identityKeyB.XPublicKey).ToArray();
        }
    }
}

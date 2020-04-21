using BeaconClient.Crypto;

namespace BeaconClient.Server
{
    public class ChatPackage
    {
        public Curve25519KeyPair OtherIdentityKey { get; set; }
        public Curve25519KeyPair OtherSignedPreKey { get; set; }
        public Curve25519KeyPair OtherOneTimePreKey { get; set; }
    }
}
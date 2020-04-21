using BeaconClient.Crypto;

namespace BeaconClient.Messages
{
    public class MessageHeader
    {
        public Curve25519KeyPair DhRatchetKey;
        public uint PreviousCount;
        public uint MessageNumber;
    }
}
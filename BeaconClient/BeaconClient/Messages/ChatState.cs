using System.Collections.Generic;
using BeaconClient.Crypto;

namespace BeaconClient.Messages
{
    public class ChatState
    {
        public Curve25519KeyPair DhSendingKeyPair;
        public Curve25519KeyPair DhReceivingKey;
        public byte[] RootKey;
        public byte[] SendingChainKey;
        public byte[] ReceivingChainKey;
        public uint CountSent;
        public uint CountReceived;
        public uint PreviousCount;
        
        // string here is the base64 encoded version of the DH public key + that message count
        public Dictionary<string, (byte[], byte[])> MissedMessages;
    }
}
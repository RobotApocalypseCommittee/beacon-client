using System;
using System.Collections.Generic;
using BeaconClient.Crypto;
using BeaconClient.Messages;

namespace BeaconClient.Database
{
    public class GlobalKeyStore
    {
        // Global keystore singleton
        
        private static GlobalKeyStore _instance;
        private static readonly object Locker = new object();

        private GlobalKeyStore()
        {
        }

        public Curve25519KeyPair IdentityKeyPair;
        public List<Curve25519KeyPair> SignedPreKeyPairs;
        public List<Curve25519KeyPair> OneTimePreKeyPairs;

        public Dictionary<string, ChatState> ChatStates;

        public static GlobalKeyStore Instance
        {
            get
            {
                lock (Locker)
                {
                    if (_instance is null)
                    {
                        throw new Exception("GlobalKeyStore accessed before initialisation");
                    }

                    return _instance;
                }
            }
        }

        public static void Initialise(Curve25519KeyPair identityKeyPair, List<Curve25519KeyPair> signedPreKeyPairs,
            List<Curve25519KeyPair> oneTimePreKeyPairs, Dictionary<string, ChatState> chatStates)
        {
            lock (Locker)
            {
                if (!(_instance is null))
                {
                    throw new Exception("GlobalKeyStore has already been initialised");
                }

                _instance = new GlobalKeyStore
                {
                    IdentityKeyPair = identityKeyPair,
                    SignedPreKeyPairs = signedPreKeyPairs,
                    OneTimePreKeyPairs = oneTimePreKeyPairs,
                    ChatStates = chatStates
                };
            }
        }
        
        // Todo maybe add reset method
    }
}
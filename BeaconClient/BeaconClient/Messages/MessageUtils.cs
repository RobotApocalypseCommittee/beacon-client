using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeaconClient.Crypto;
using BeaconClient.Database;
using BeaconClient.Server;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Digests;
using Xamarin.Forms;

namespace BeaconClient.Messages
{
    public static class MessageUtils
    {
        public static void HandleMessage(MetaMessage metaMessage)
        {
            switch (metaMessage.Type)
            {
                case MessageType.InitialMessage:
                    HandleInitialMessage(metaMessage);
                    break;
                case MessageType.NormalTextMessage:
                    HandleNormalTextMessage(metaMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void HandleInitialMessage(MetaMessage metaMessage)
        {
            // TODO actually do database stuff

            string senderUuid = metaMessage.OtherUuid;

            InitialMessage message = JsonConvert.DeserializeObject<InitialMessage>(metaMessage.Payload);
            
            Curve25519KeyPair otherIdentityKey = new Curve25519KeyPair(Convert.FromBase64String(message.SenderIdentityKeyBase64), false, true);
            Curve25519KeyPair ephemeralKey = new Curve25519KeyPair(Convert.FromBase64String(message.EphemeralKeyBase64), false, false);
            
            Curve25519KeyPair receivedSignedPreKey = new Curve25519KeyPair(Convert.FromBase64String(message.RecipientSignedPreKeyBase64), false, false);
            Curve25519KeyPair signedPreKeyPair = GlobalKeyStore.Instance.SignedPreKeyPairs.Find(pair => pair.XPublicKey.SequenceEqual(receivedSignedPreKey.XPublicKey));

            if (signedPreKeyPair is null)
            {
                throw new Exception("Matching signed prekey pair not found");
            }

            Curve25519KeyPair oneTimePreKeyPair;
            if (message.RecipientOneTimePreKeyBase64 is null)
            {
                oneTimePreKeyPair = null;
            }
            else
            {
                Curve25519KeyPair receivedOneTimePreKey = new Curve25519KeyPair(Convert.FromBase64String(message.RecipientOneTimePreKeyBase64), false, false);
                oneTimePreKeyPair = GlobalKeyStore.Instance.OneTimePreKeyPairs.Find(pair => pair.XPublicKey.SequenceEqual(receivedOneTimePreKey.XPublicKey));

                if (oneTimePreKeyPair is null)
                {
                    throw new Exception("Matching one-time prekey pair not found");
                }

                // Remove the one-time key, never to be used again
                GlobalKeyStore.Instance.SignedPreKeyPairs.Remove(oneTimePreKeyPair);
            }

            byte[] secretKey = CryptoUtils.DeriveX3DhSecretReceiver(GlobalKeyStore.Instance.IdentityKeyPair,
                signedPreKeyPair, otherIdentityKey, ephemeralKey, oneTimePreKeyPair);

            GlobalKeyStore.Instance.ChatStates[senderUuid] = new ChatState
            {
                DhSendingKeyPair = signedPreKeyPair,
                DhReceivingKey = null,
                RootKey = secretKey,
                SendingChainKey = null,
                ReceivingChainKey = null,
                CountSent = 0,
                CountReceived = 0,
                PreviousCount = 0,
                MissedMessages = new Dictionary<string, (byte[], byte[])>()
            };

            MetaMessage innerMetaMessage = new MetaMessage
            {
                OtherUuid = metaMessage.OtherUuid,
                Type = MessageType.NormalTextMessage,
                Payload = message.InnerMessagePayload,
                Timestamp = metaMessage.Timestamp
            };
            
            NormalTextMessage innerMessage = JsonConvert.DeserializeObject<NormalTextMessage>(innerMetaMessage.Payload);
            string requiredAssociatedDataBase64 = Convert.ToBase64String(
                CryptoUtils.CalculateInitialAssociatedData(otherIdentityKey, GlobalKeyStore.Instance.IdentityKeyPair));
            if (innerMessage.AssociatedDataBase64 != requiredAssociatedDataBase64)
            {
                throw new Exception($"Associated data for the initial message isn't what it should be: {innerMessage.AssociatedDataBase64} != {requiredAssociatedDataBase64}");
            }
            
            HandleNormalTextMessage(innerMetaMessage);
        }

        private static void HandleNormalTextMessage(MetaMessage metaMessage)
        {
            // TODO actually do database stuff

            string senderUuid = metaMessage.OtherUuid;
            
            // TODO make much better!
            ChatState state = GlobalKeyStore.Instance.ChatStates[senderUuid];

            NormalTextMessage message = JsonConvert.DeserializeObject<NormalTextMessage>(metaMessage.Payload);
            MessageHeader header = new MessageHeader
            {
                DhRatchetKey = new Curve25519KeyPair(Convert.FromBase64String(message.DhKeyBase64), false, false),
                PreviousCount = message.PreviousCount,
                MessageNumber = message.MessageNumber
            };

            byte[] associatedData = null;
            if (!(message.AssociatedDataBase64 is null))
            {
                associatedData = Convert.FromBase64String(message.AssociatedDataBase64);
            }

            byte[] decryptedData = RatchetDecrypt(state, header, Convert.FromBase64String(message.EncryptedTextBase64),
                associatedData);

            string decryptedText = Encoding.UTF8.GetString(decryptedData);
            
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("Message received!", $"From: {senderUuid}\nContent: {decryptedText}", "Ok");
            });
        }

        private static byte[] RatchetDecrypt(ChatState state, MessageHeader header, byte[] cipherText,
            byte[] associatedData = null)
        {
            // TODO do ratcheting with copy before it's verified to have worked
            
            string missedMessagesKey = Convert.ToBase64String(header.DhRatchetKey.XPublicKey) + header.MessageNumber;
            if (state.MissedMessages.TryGetValue(missedMessagesKey, out (byte[], byte[]) skippedMessageTuple))
            {
                state.MissedMessages.Remove(missedMessagesKey);

                (byte[] skippedMessageKey, byte[] skippedAssociatedData) = skippedMessageTuple;
                return CryptoUtils.DecryptWithMessageKey(skippedMessageKey, cipherText, CombineAdAndHeader(associatedData ?? skippedAssociatedData, header));
            }

            if (state.DhReceivingKey is null || !header.DhRatchetKey.XPublicKey.SequenceEqual(state.DhReceivingKey.XPublicKey))
            {
                SkipMessageKeys(state, header.PreviousCount);
                
                // Ratchet the chat state
                state.PreviousCount = state.CountSent;
                state.CountSent = 0;
                state.CountReceived = 0;
                state.DhReceivingKey = header.DhRatchetKey;
                (state.RootKey, state.ReceivingChainKey) = CryptoUtils.RatchetRootKey(state.RootKey,
                    state.DhSendingKeyPair.CalculateSharedSecret(state.DhReceivingKey));
                state.DhSendingKeyPair = new Curve25519KeyPair();
                (state.RootKey, state.SendingChainKey) = CryptoUtils.RatchetRootKey(state.RootKey,
                    state.DhSendingKeyPair.CalculateSharedSecret(state.DhReceivingKey));
            }
            
            SkipMessageKeys(state, header.MessageNumber);

            byte[] messageKey, derivedAssociatedData;
            (state.ReceivingChainKey, messageKey, derivedAssociatedData) =
                CryptoUtils.RatchetChainKey(state.ReceivingChainKey);

            state.CountReceived++;

            return CryptoUtils.DecryptWithMessageKey(messageKey, cipherText, CombineAdAndHeader(associatedData ?? derivedAssociatedData, header));
        }

        private static void SkipMessageKeys(ChatState state, uint upTo)
        {
            if (state.CountReceived + 50 < upTo)
            {
                throw new Exception("MAX_SKIP exceeded");
            }

            if (state.ReceivingChainKey is null) return;
            while (state.CountReceived < upTo)
            {
                byte[] messageKey, associatedData;
                (state.ReceivingChainKey, messageKey, associatedData) =
                    CryptoUtils.RatchetChainKey(state.ReceivingChainKey);
                    
                string missedMessagesKey = Convert.ToBase64String(state.DhReceivingKey.XPublicKey) + state.CountReceived;
                state.MissedMessages[missedMessagesKey] = (messageKey, associatedData);

                state.CountReceived++;
            }
        }

        private static byte[] CombineAdAndHeader(byte[] associatedData, MessageHeader header)
        {
            return associatedData.Concat(header.DhRatchetKey.XPublicKey)
                .Concat(BitConverter.GetBytes(header.PreviousCount))
                .Concat(BitConverter.GetBytes(header.MessageNumber)).ToArray();
        }

        public static MetaMessage ComposeInitialMessage(string recipientUuid, ChatPackage chatPackage, string text)
        {
            Curve25519KeyPair ephemeralKeyPair = new Curve25519KeyPair();
            byte[] secretKey = CryptoUtils.DeriveX3DhSecretSender(GlobalKeyStore.Instance.IdentityKeyPair,
                ephemeralKeyPair, chatPackage.OtherIdentityKey, chatPackage.OtherSignedPreKey,
                chatPackage.OtherOneTimePreKey);


            Curve25519KeyPair dhSendingKeyPair = new Curve25519KeyPair();
            (byte[] rootKey, byte[] sendingChainKey) = CryptoUtils.RatchetRootKey(secretKey,
                dhSendingKeyPair.CalculateSharedSecret(chatPackage.OtherSignedPreKey));
            
            ChatState state = new ChatState
            {
                DhSendingKeyPair = dhSendingKeyPair,
                DhReceivingKey = chatPackage.OtherSignedPreKey,
                RootKey = rootKey,
                SendingChainKey = sendingChainKey,
                ReceivingChainKey = null,
                CountSent = 0,
                CountReceived = 0,
                PreviousCount = 0,
                MissedMessages = new Dictionary<string, (byte[], byte[])>()
            };
            GlobalKeyStore.Instance.ChatStates[recipientUuid] = state;

            byte[] initialAssociatedData = CryptoUtils.CalculateInitialAssociatedData(
                GlobalKeyStore.Instance.IdentityKeyPair,
                chatPackage.OtherIdentityKey);
            (byte[] encryptedText, MessageHeader header) = RatchetEncrypt(state, Encoding.UTF8.GetBytes(text), initialAssociatedData);

            NormalTextMessage innerPayload = new NormalTextMessage
            {
                AssociatedDataBase64 = Convert.ToBase64String(initialAssociatedData),
                DhKeyBase64 = Convert.ToBase64String(dhSendingKeyPair.XPublicKey),
                EncryptedTextBase64 = Convert.ToBase64String(encryptedText),
                MessageNumber = header.MessageNumber,
                PreviousCount = header.PreviousCount
            };

            string innerPayloadJson = JsonConvert.SerializeObject(innerPayload);

            InitialMessage payload = new InitialMessage
            {
                EphemeralKeyBase64 = Convert.ToBase64String(ephemeralKeyPair.XPublicKey),
                InnerMessagePayload = innerPayloadJson,
                SenderIdentityKeyBase64 = Convert.ToBase64String(GlobalKeyStore.Instance.IdentityKeyPair.EdPublicKey),
                RecipientSignedPreKeyBase64 = Convert.ToBase64String(chatPackage.OtherSignedPreKey.XPublicKey),
                RecipientOneTimePreKeyBase64 = Convert.ToBase64String(chatPackage.OtherOneTimePreKey.XPublicKey)
            };

            string payloadJson = JsonConvert.SerializeObject(payload);

            return new MetaMessage
            {
                OtherUuid = recipientUuid,
                Type = MessageType.InitialMessage,
                Payload = payloadJson
            };
        }

        public static MetaMessage ComposeNormalTextMessage(string recipientUuid, string text)
        {
            if (!GlobalKeyStore.Instance.ChatStates.TryGetValue(recipientUuid, out ChatState state))
            {
                throw new Exception("You need to send or receive an initial message before sending a normal message!");
            }
            
            (byte[] encryptedText, MessageHeader header) = RatchetEncrypt(state, Encoding.UTF8.GetBytes(text));
            
            NormalTextMessage payload = new NormalTextMessage
            {
                AssociatedDataBase64 = null,
                DhKeyBase64 = Convert.ToBase64String(header.DhRatchetKey.XPublicKey),
                EncryptedTextBase64 = Convert.ToBase64String(encryptedText),
                MessageNumber = header.MessageNumber,
                PreviousCount = header.PreviousCount
            };

            string payloadJson = JsonConvert.SerializeObject(payload);
            
            return new MetaMessage
            {
                OtherUuid = recipientUuid,
                Type = MessageType.NormalTextMessage,
                Payload = payloadJson
            };
        }

        private static (byte[], MessageHeader) RatchetEncrypt(ChatState state, byte[] plainText, byte[] associatedData = null)
        {
            byte[] messageKey, derivedAssociatedData;
            (state.SendingChainKey, messageKey, derivedAssociatedData) =
                CryptoUtils.RatchetChainKey(state.SendingChainKey);

            MessageHeader header = new MessageHeader
            {
                PreviousCount = state.PreviousCount,
                MessageNumber = state.CountSent,
                DhRatchetKey = state.DhSendingKeyPair
            };

            state.CountSent++;

            return (
                CryptoUtils.EncryptWithMessageKey(messageKey, plainText,
                    CombineAdAndHeader(associatedData ?? derivedAssociatedData, header)), header);
        }
    }
}
using System;

// This class is to represent a message ON THE UI side
namespace BeaconClient.Messages
{
    public class NormalMessage
    {
        public DateTime Timestamp { get; set; }
        public int MessageType { get; set; }
        public string Body { get; set; }
        public string DHPublicKey { get; set; }
        public int Recipient { get; set; }
        public int Originator { get; set; }
        public string MessageID { get; set; }
        public int ReadStatus { get; set; }
        public int ChainNumber { get; set; }
        public int MessageNumber { get; set; }
        public int ChannelID { get; set; }
    }
}
/*
 I am not 100% sure what all of these are
(For now, user-user chats)

- Timestamp
    - (Hmmm)
- Type? (text or file: maybe there are other types too? Could of course be contained in the blob if we wanted) (is this implemented?)
- If file, url to (encrypted) blob?
- body - blob
- DH public key - optional blob
- Recipient - user ID
- Originator? - user ID
- Message ID - (composed)
- Read Status - delivered, read (this should only be relevant in group chats, since once it’s been fully delivered, the message should get deleted from the server)
- Chain Number, Message Number
- ‘Channel’ ID?
*/
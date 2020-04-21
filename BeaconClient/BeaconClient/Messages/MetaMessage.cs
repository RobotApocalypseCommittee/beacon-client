using System;

namespace BeaconClient.Messages
{
    public class MetaMessage
    {
        public string OtherUuid;
        public MessageType Type;
        public string Payload;
        public DateTime Timestamp;
    }
}
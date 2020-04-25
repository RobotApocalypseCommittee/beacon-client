using System.Collections.Generic;
using BeaconClient.Messages;

namespace BeaconClient
{
    public class DisplayChat
    {
        public int ChannelID { get; set; }
        
        // I don't see any reference to chat names in the technical spec, but I'm leaving this here since I assume it is needed
        public string ChannelName { get; set; }

        public List<NormalMessage> MessageList { get; set; }
    }
}
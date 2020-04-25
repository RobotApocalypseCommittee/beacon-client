using System;
using System.Collections.Generic;
using BeaconClient.Messages;

namespace BeaconClient
{
    public static class Singleton
    {
        public static List<DisplayChat> AllChats = new List<DisplayChat>
        {
            new DisplayChat()
            {
                ChannelID = 1,
                ChannelName = "Bhuvan Belur",
                MessageList =  new List<Messages.NormalMessage>
                {
                    new Messages.NormalMessage
                    {
                        Body = "Hey! What's up! I found this new game called DF2 - you should check it out!",
                        ChainNumber = 0,
                        ChannelID = 1,
                        DHPublicKey = "",
                        MessageID = "goegr",
                        MessageNumber = 3,
                        MessageType = 0,
                        Originator = 0,
                        ReadStatus = 0,
                        Timestamp = new DateTime(2020, 4, 24, 20, 52, 30)
                    },
                    new Messages.NormalMessage
                    {
                        Body = "Are you here?",
                        ChainNumber = 0,
                        ChannelID = 1,
                        DHPublicKey = "",
                        MessageID = "joifiewroj",
                        MessageNumber = 2,
                        MessageType = 0,
                        Originator = 0,
                        ReadStatus = 0,
                        Timestamp = new DateTime(2020, 4, 23, 4, 34, 1)
                    },
                    new Messages.NormalMessage
                    {
                        Body = "Hey - I just got beacon and I gotta say - THIS IS SUCH A GOOD APP!",
                        ChainNumber = 0,
                        ChannelID = 1,
                        DHPublicKey = "",
                        MessageID = "wwwef",
                        MessageNumber = 1,
                        MessageType = 0,
                        Originator = 0,
                        ReadStatus = 0,
                        Timestamp = new DateTime(2020, 4, 20, 9, 20, 55)
                    }
                }
            },
            new DisplayChat
            {
                ChannelID = 2,
                ChannelName = "Atto Allas",
                MessageList = new List<NormalMessage>
                {
                    new Messages.NormalMessage
                    {
                        Body = "Who's Joe?",
                        ChainNumber = 0,
                        ChannelID = 22,
                        DHPublicKey = "",
                        MessageID = "grgw",
                        MessageNumber = 3,
                        MessageType = 0,
                        Originator = 3,
                        ReadStatus = 0,
                        Timestamp = new DateTime(2020, 4, 23, 15, 6, 30)
                    },
                    new Messages.NormalMessage
                    {
                        Body = "Are u ready",
                        ChainNumber = 0,
                        ChannelID = 2,
                        DHPublicKey = "",
                        MessageID = "rgreah",
                        MessageNumber = 2,
                        MessageType = 0,
                        Originator = 3,
                        ReadStatus = 0,
                        Timestamp = new DateTime(2020, 4, 23, 15, 6, 15)
                    },
                    new Messages.NormalMessage
                    {
                        Body = "Ok, I have a great joke for you",
                        ChainNumber = 0,
                        ChannelID = 2,
                        DHPublicKey = "",
                        MessageID = "utkkj7ut",
                        MessageNumber = 1,
                        MessageType = 0,
                        Originator = 3,
                        ReadStatus = 0,
                        Timestamp = new DateTime(2020, 4, 23, 15, 6, 2)
                    }
                }
            }
        };
    }
}
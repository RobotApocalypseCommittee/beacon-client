using System;
using System.Runtime.CompilerServices;

namespace BeaconClient
{
    public class ChatPreview
    {
        public String Name { get; set; }
        public String Recent { get; set; }
        public DateTime LastActivity { get; set; }

        public override string ToString()
        {
            return Recent;
        }

    }
}
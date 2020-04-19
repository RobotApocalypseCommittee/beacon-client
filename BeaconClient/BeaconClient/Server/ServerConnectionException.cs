using System;

namespace BeaconClient.Server
{
    public class ServerConnectionException : Exception
    {
        // Represents everything that could go wrong when communicating with the server: be it connection errors or server mis-formatting, etc
        
        public ServerConnectionException()
        {
        }

        public ServerConnectionException(string message) : base(message)
        {
        }

        public ServerConnectionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
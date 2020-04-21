using System;
using System.Threading.Tasks;
using BeaconClient.Crypto;
using Newtonsoft.Json;

namespace BeaconClient.Messages
{
    public class InitialMessage
    {
        [JsonProperty("identity_key")]
        public string SenderIdentityKeyBase64 { get; set; }
        
        [JsonProperty("ephemeral_key")]
        public string EphemeralKeyBase64 { get; set; }
        
        [JsonProperty("signed_prekey")]
        public string RecipientSignedPreKeyBase64 { get; set; }
        
        [JsonProperty("onetime_key")]
        public string RecipientOneTimePreKeyBase64 { get; set; }
        
        [JsonProperty("inner_payload")]
        public string InnerMessagePayload { get; set; }
    }
}
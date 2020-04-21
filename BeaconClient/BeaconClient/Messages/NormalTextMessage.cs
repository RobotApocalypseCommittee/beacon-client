using Newtonsoft.Json;

namespace BeaconClient.Messages
{
    public class NormalTextMessage
    {
        [JsonProperty("encrypted_text")]
        public string EncryptedTextBase64;
        
        [JsonProperty("associated_data")]
        public string AssociatedDataBase64;
        
        [JsonProperty("dh")]
        public string DhKeyBase64;
        
        [JsonProperty("pn")]
        public uint PreviousCount;
        
        [JsonProperty("n")]
        public uint MessageNumber;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BeaconClient.Crypto;
using BeaconClient.Database;
using BeaconClient.Messages;
using Newtonsoft.Json;

namespace BeaconClient.Server
{
    public class ServerConnection
    {
        // Represents the connection to the central server and its APIs
        private readonly HttpClient _client;

        private readonly string _baseUrl;
        private readonly Curve25519KeyPair _deviceKey;
        private string _deviceUuid;

        // Both _nonce and _nonceSig are base64
        private string _nonce;
        private string _nonceSig;
        private DateTime _expiryDateTime;

        public ServerConnection(string baseUrl, Curve25519KeyPair deviceKey, string deviceUuid = null)
        {
            _client = new HttpClient();
            _baseUrl = baseUrl;
            _deviceKey = deviceKey;
            _deviceUuid = deviceUuid;
        }
        
        private async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            return await _client.GetAsync(_baseUrl + endpoint).ConfigureAwait(false);
        }
        
        private async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content = null)
        {
            return await _client.PostAsync(_baseUrl + endpoint, content).ConfigureAwait(false);
        }

        private void UpdateNonce(HttpResponseHeaders headers)
        {
            List<string> newNonceHeaders = headers.GetValues("X-NEWNONCE").ToList();

            if (newNonceHeaders.Count != 1)
            {
                throw new ServerConnectionException($"{newNonceHeaders.Count} X-NEWNONCE headers were found, exactly 1 required");
            }

            string newNonce = newNonceHeaders.First();
            
            // TODO Nonce expiry!
            DateTime expiryDateTime = DateTime.Now.Add(new TimeSpan(10000000));
            
            byte[] newNonceBytes = Convert.FromBase64String(newNonce);
            string nonceSig = Convert.ToBase64String(_deviceKey.Sign(newNonceBytes));

            _nonce = newNonce;
            _nonceSig = nonceSig;
            _expiryDateTime = expiryDateTime;
        }

        private async Task HandleResponse(HttpResponseMessage response, bool authenticated = true)
        {
            // Todo handle stuff like "not authorised" by automatically redoing requests using an enum for return possibilities
            if (!response.IsSuccessStatusCode)
            {
                throw new ServerConnectionException($"The server responded with {response.StatusCode}: {response.ReasonPhrase}. The content of the server response is:\n{await response.Content.ReadAsStringAsync()}");
            }

            if (authenticated)
            {
                UpdateNonce(response.Headers);
            }
        }
        
        private async Task<Dictionary<string, T>> HandleResponse<T>(HttpResponseMessage response, bool authenticated = true)
        {
            await HandleResponse(response, authenticated);
            
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Dictionary<string, T>>(responseString);
        }

        private async Task StartNewSessionAsync()
        {
            HttpResponseMessage response = await PostAsync("/session/new", null).ConfigureAwait(false);
            await HandleResponse<string>(response).ConfigureAwait(false);

            HttpResponseHeaders headers = response.Headers;
            UpdateNonce(headers);
        }
        
        private async Task StartNewSessionIfNeededAsync()
        {
            if (_nonceSig is null || _expiryDateTime >= DateTime.Now)
            {
                await StartNewSessionAsync().ConfigureAwait(false);
            }
        }

        private async Task<HttpResponseMessage> GetAuthenticatedAsync(string endpoint)
        {
            await StartNewSessionIfNeededAsync().ConfigureAwait(false);
            
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_baseUrl + endpoint),
                Headers =
                {
                    {"X-NONCE", _nonce},
                    {"X-SIGNEDNONCE", _nonceSig},
                    {"X-DEVICEID", _deviceUuid}
                }
            };

            return await _client.SendAsync(request).ConfigureAwait(false);
        }
        
        private async Task<HttpResponseMessage> PostAuthenticatedAsync(string endpoint, HttpContent content = null)
        {
            await StartNewSessionIfNeededAsync().ConfigureAwait(false);
            
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_baseUrl + endpoint),
                Headers =
                {
                    {"X-NONCE", _nonce},
                    {"X-SIGNEDNONCE", _nonceSig},
                    {"X-DEVICEID", _deviceUuid}
                },
                Content = content
            };

            return await _client.SendAsync(request).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> PostAuthenticatedAsync(string endpoint,
            Dictionary<string, string> contentDict)
        {
            string contentString = JsonConvert.SerializeObject(contentDict);
            HttpContent content = new StringContent(contentString, Encoding.UTF8, "application/json");

            return await PostAuthenticatedAsync(endpoint, content).ConfigureAwait(false);
        }

        // Returns the Device UUID as a string, as well as setting it for this ServerConnection
        public async Task<string> RegisterDeviceAsync()
        {
            if (!(_deviceUuid is null))
            {
                throw new ServerConnectionException("Device UUID must be null to register a new device");
            }

            Dictionary<string, string> contentDict = new Dictionary<string, string>
            {
                {"public_key", Convert.ToBase64String(_deviceKey.EdPublicKey)}
            };
            string contentString = JsonConvert.SerializeObject(contentDict);
            HttpContent content = new StringContent(contentString, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await PostAsync("/devices/new", content).ConfigureAwait(false);
            Dictionary<string, string> responseDict = await HandleResponse<string>(response, false).ConfigureAwait(false);
            
            if (!responseDict.ContainsKey("device_id"))
            {
                throw new ServerConnectionException($"Device UUID not found in response content: {responseDict}");
            }

            _deviceUuid = responseDict["device_id"];

            return _deviceUuid;
        }

        // Returns the User UUID
        public async Task<string> RegisterUserAsync(string email, Curve25519KeyPair userKeyPair, Curve25519KeyPair signedPreKeyPair, string nickname = null,
            string bio = null)
        {
            Dictionary<string, string> contentDict = new Dictionary<string, string>
            {
                {"email", email},
                {"identity_key", Convert.ToBase64String(userKeyPair.EdPublicKey)},
                {"signed_prekey", Convert.ToBase64String(signedPreKeyPair.XPublicKey)},
                {"prekey_signature", Convert.ToBase64String(userKeyPair.Sign(signedPreKeyPair.XPublicKey))},
                {"nickname", nickname},
                {"bio", bio}
            };
            
            HttpResponseMessage response = await PostAuthenticatedAsync("/users/new", contentDict).ConfigureAwait(false);
            Dictionary<string, string> responseDict = await HandleResponse<string>(response).ConfigureAwait(false);

            if (!responseDict.ContainsKey("user_id"))
            {
                throw new ServerConnectionException($"User UUID not found in response content: {responseDict}");
            }

            return responseDict["user_id"];
        }

        public async Task UploadOneTimePreKeysAsync(IEnumerable<Curve25519KeyPair> oneTimePreKeys)
        {
            IEnumerable<string> publicKeysBase64 =
                from preKey in oneTimePreKeys select Convert.ToBase64String(preKey.XPublicKey);
            var contentDict = new Dictionary<string, IEnumerable<string>>
            {
                {"keys", publicKeysBase64}
            };
            string contentString = JsonConvert.SerializeObject(contentDict);
            HttpContent content = new StringContent(contentString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await PostAuthenticatedAsync("/keys/onetime", content).ConfigureAwait(false);
            await HandleResponse(response);
        }

        public async Task<User> GetUserInfoAsync(string userUuid)
        {
            HttpResponseMessage response = await GetAuthenticatedAsync($"/users/{userUuid}");
            
            throw new NotImplementedException();
        }

        public async Task<ChatPackage> GetChatPackage(string userUuid)
        {
            HttpResponseMessage response = await PostAuthenticatedAsync($"/users/{userUuid}/package");
            Dictionary<string, string> responseDict = await HandleResponse<string>(response).ConfigureAwait(false);
            
            // TODO check all keys are present in dictionary
            
            Curve25519KeyPair otherIdentityKey = new Curve25519KeyPair(Convert.FromBase64String(responseDict["identity_key"]), false, true);
            Curve25519KeyPair otherSignedPreKey = new Curve25519KeyPair(Convert.FromBase64String(responseDict["signed_prekey"]), false, false);
            byte[] otherSignedPreKeySignature = Convert.FromBase64String(responseDict["prekey_signature"]);
            Curve25519KeyPair otherOneTimePreKey = new Curve25519KeyPair(Convert.FromBase64String(responseDict["onetime_key"]), false, false);

            if (!otherIdentityKey.Verify(otherSignedPreKey.XPublicKey, otherSignedPreKeySignature))
            {
                throw new ServerConnectionException($"The user's pre-key signature does not match their signed key: uuid = {userUuid}");
            }
            
            return new ChatPackage
            {
                OtherIdentityKey = otherIdentityKey,
                OtherSignedPreKey = otherSignedPreKey,
                OtherOneTimePreKey = otherOneTimePreKey
            };
        }

        public async Task SendMessageAsync(MetaMessage metaMessage)
        {
            var contentDict = new Dictionary<string, string>
            {
                {"recipient", metaMessage.OtherUuid},
                {"type", metaMessage.Type.ToString()},
                {"payload", metaMessage.Payload}
            };
            HttpResponseMessage response = await PostAuthenticatedAsync("/messages/send", contentDict).ConfigureAwait(false);
            await HandleResponse(response);
        }

        public async Task<IEnumerable<MetaMessage>> CheckMailboxAsync()
        {
            HttpResponseMessage response = await PostAuthenticatedAsync("/messages/mailbox").ConfigureAwait(false);
            var responseDict = await HandleResponse<IEnumerable<Dictionary<string, string>>>(response)
                .ConfigureAwait(false);

            IEnumerable<Dictionary<string, string>> messages = responseDict["messages"];
            var messageObjects = new List<MetaMessage>();

            foreach (var message in messages)
            {
                // Todo check that all the keys exist
                
                if (false && !long.TryParse(message["timestamp"], out long timestamp))
                {
                    throw new ServerConnectionException($"The mailbox message did not contain a valid timestamp: {message["timestamp"]}");
                }

                if (!Enum.TryParse(message["type"], out MessageType messageType))
                {
                    throw new ServerConnectionException($"The mailbox message did not contain a known message type: {message["type"]}");
                }
                
                messageObjects.Add(new MetaMessage
                {
                    OtherUuid = message["sender"],
                    Type = messageType,
                    Payload = message["payload"],
                    Timestamp = DateTime.Now
                });
            }

            return messageObjects;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BeaconClient.Crypto;
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
        
        private async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content)
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
        
        private async Task<Dictionary<string, string>> HandleResponse(HttpResponseMessage response)
        {
            // Todo handle stuff like "not authorised" by automatically redoing requests using an enum for return possibilities
            if (!response.IsSuccessStatusCode)
            {
                throw new ServerConnectionException($"The server responded with {response.StatusCode}: {response.ReasonPhrase}. The content of the server reponse is:\n{response.Content}");
            }

            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
        }

        private async Task StartNewSessionAsync()
        {
            HttpResponseMessage response = await PostAsync("/session/new", null).ConfigureAwait(false);
            await HandleResponse(response).ConfigureAwait(false);

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
        
        private async Task<HttpResponseMessage> PostAuthenticatedAsync(string endpoint, HttpContent content)
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
            HttpContent content = new StringContent(contentString);

            return await PostAuthenticatedAsync(endpoint, content).ConfigureAwait(false);
        }

        // Returns the UUID as a string, as well as setting it for this ServerConnection
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
            HttpContent content = new StringContent(contentString);

            HttpResponseMessage response = await PostAsync("/devices/register", content).ConfigureAwait(false);
            Dictionary<string, string> responseDict = await HandleResponse(response).ConfigureAwait(false);
            
            if (!responseDict.ContainsKey("device_id"))
            {
                throw new ServerConnectionException($"Device UUID not found in response content: {responseDict}");
            }

            _deviceUuid = responseDict["device_id"];

            return _deviceUuid;
        }

        // Returns the User UUID
        public async Task<string> RegisterUserAsync(string email, Curve25519KeyPair userKeyPair, string nickname = null,
            string bio = null)
        {
            Dictionary<string, string> contentDict = new Dictionary<string, string>
            {
                {"email", email},
                {"public_key", Convert.ToBase64String(userKeyPair.EdPublicKey)},
                {"nickname", nickname},
                {"bio", bio}
            };
            
            HttpResponseMessage response = await PostAuthenticatedAsync("/users/register", contentDict).ConfigureAwait(false);
            Dictionary<string, string> responseDict = await HandleResponse(response).ConfigureAwait(false);

            if (!responseDict.ContainsKey("user_id"))
            {
                throw new ServerConnectionException($"User UUID not found in response content: {responseDict}");
            }

            return responseDict["user_id"];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Xamarin.Forms;

using BeaconClient.Crypto;
using BeaconClient.Database;
using BeaconClient.Messages;
using BeaconClient.NativeDependencies;
using BeaconClient.Server;

namespace BeaconClient
{
    public partial class MainPage : ContentPage
    {
        private readonly ISecureStorageService _secureStorageService = DependencyService.Get<ISecureStorageService>();
        private readonly IPreferencesService _preferencesService = DependencyService.Get<IPreferencesService>();

        private ServerConnection _connection;
        
        public MainPage()
        {
            InitializeComponent();
            
            Curve25519KeyPair identityKey = new Curve25519KeyPair();
            Curve25519KeyPair signedPreKey = new Curve25519KeyPair();
            Curve25519KeyPair oneTimePreKey = new Curve25519KeyPair();
            GlobalKeyStore.Initialise(identityKey, new List<Curve25519KeyPair>{signedPreKey}, new List<Curve25519KeyPair>{oneTimePreKey}, new Dictionary<string, ChatState>());
        }

        private async void OnSetUpPressed(object sender, EventArgs e)
        {
            string serverUrl = ServerEntry.Text;
            
            string devicePrivateKeyBase64 = await _secureStorageService.GetAsync("devicePrivateKey");
            Curve25519KeyPair deviceKeyPair;
            if (devicePrivateKeyBase64 is null)
            {
                deviceKeyPair = new Curve25519KeyPair();
                await _secureStorageService.SetAsync("devicePrivateKey", Convert.ToBase64String(deviceKeyPair.EdPrivateKey));
            }
            else
            {
                deviceKeyPair = new Curve25519KeyPair(Convert.FromBase64String(devicePrivateKeyBase64), true, true);
            }
            
            _connection = new ServerConnection(serverUrl, deviceKeyPair);
            
            string deviceUuid = await _connection.RegisterDeviceAsync();
            string userUuid = await _connection.RegisterUserAsync(EmailEntry.Text, GlobalKeyStore.Instance.IdentityKeyPair, GlobalKeyStore.Instance.SignedPreKeyPairs[0], ":)", "Non-contradictory");

            await _connection.UploadOneTimePreKeysAsync(GlobalKeyStore.Instance.OneTimePreKeyPairs);

            await DisplayAlert("Information: ", $"Device ID: {deviceUuid}\nUser ID: {userUuid}", "Ok");
            EmailEntry.Text = userUuid;
        }

        private async void OnSendInitialMessagePressed(object sender, EventArgs e)
        {
            string recipientUuid = RecipientUuid.Text;
            string messageText = Message.Text;

            ChatPackage chatPackage = await _connection.GetChatPackage(recipientUuid);

            MetaMessage initialMessage = MessageUtils.ComposeInitialMessage(recipientUuid, chatPackage, messageText);

            await _connection.SendMessageAsync(initialMessage);
        }
        
        private async void OnSendNormalMessagePressed(object sender, EventArgs e)
        {
            string recipientUuid = RecipientUuid.Text;
            string messageText = Message.Text;

            MetaMessage textMessage = MessageUtils.ComposeNormalTextMessage(recipientUuid, messageText);

            await _connection.SendMessageAsync(textMessage);
        }

        private async void OnCheckMailboxPressed(object sender, EventArgs e)
        {
            IEnumerable<MetaMessage> messages = await _connection.CheckMailboxAsync();

            foreach (var message in messages)
            {
                MessageUtils.HandleMessage(message);
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            var preferences = _preferencesService;

            preferences.Remove("userUuid");
            preferences.Remove("deviceUuid");
        }
    }
}

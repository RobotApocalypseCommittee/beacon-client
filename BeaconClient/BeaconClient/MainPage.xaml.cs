using System;
using System.Security.Cryptography;
using Xamarin.Forms;

using BeaconClient.Crypto;
using BeaconClient.NativeDependencies;
using BeaconClient.Server;

namespace BeaconClient
{
    public partial class MainPage : ContentPage
    {
        private readonly ISecureStorageService _secureStorageService = DependencyService.Get<ISecureStorageService>();
        
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnButtonClicked(object sender, EventArgs e)
        {
            AES256Key key = CryptoUtils.DeriveMasterKey("secure boi", "jane.evans@westminster.org.uk");
            byte[] cipherText = key.Encrypt("Hello World!");
            await DisplayAlert("Encrypted:", string.Join(", ", cipherText), "Ok");

            byte[] iv = key.IV;

            key = CryptoUtils.DeriveMasterKey("secure boi", "jane.evans@westminster.org.uk");
            await DisplayAlert("Decrypted:", key.DecryptString(cipherText, iv), "Ok");

            try
            { 
                AES256Key wrongKey = CryptoUtils.DeriveMasterKey("wrong boi", "jane.evans@westminster.org.uk");
                await DisplayAlert("Wrong Password Decryption:", wrongKey.DecryptString(cipherText, iv), "Ok");
            }
            catch (CryptographicException ex)
            {
                await DisplayAlert("Wrongful Decryption Failed (This is good):", ex.ToString(), "Ok");
            }
            
            var watch = System.Diagnostics.Stopwatch.StartNew();
            _secureStorageService.Remove("hi");
            await _secureStorageService.SetAsync("hi", "epic gamer");
            watch.Stop();
            await DisplayAlert("SecureStorage", $"{await _secureStorageService.GetAsync("hi")}\n{watch.ElapsedMilliseconds} ms", "Ok");
        }

        private async void OnButton2Clicked(object sender, EventArgs e)
        {
            var preferences = Application.Current.Properties;

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

            ServerConnection connection;
            string deviceUuid;
            if (preferences.ContainsKey("deviceUuid"))
            {
                deviceUuid = (string) preferences["deviceUuid"];
                connection = new ServerConnection(serverUrl, deviceKeyPair, deviceUuid);
            }
            else
            {
                connection = new ServerConnection(serverUrl, deviceKeyPair);
                deviceUuid = await connection.RegisterDeviceAsync();
                preferences["deviceUuid"] = deviceUuid;
                await Application.Current.SavePropertiesAsync();
            }

            await DisplayAlert("Info", $"Device UUID: {deviceUuid}", "Ok");

            string userUuid;
            Curve25519KeyPair userKeyPair;
            Curve25519KeyPair signedPreKeyPair;
            if (preferences.ContainsKey("userUuid"))
            {
                userUuid = (string) preferences["userUuid"];

                string userPrivateKeyBase64 = await _secureStorageService.GetAsync("userPrivateKey");
                string userSignedPreKeyBase64 = await _secureStorageService.GetAsync("userSignedPreKey");
                if (userPrivateKeyBase64 is null || userSignedPreKeyBase64 is null)
                {
                    throw new Exception("User Uuid found, but no private key or signed prekey!");
                }
                
                userKeyPair = new Curve25519KeyPair(Convert.FromBase64String(userPrivateKeyBase64), true, true);
                signedPreKeyPair = new Curve25519KeyPair(Convert.FromBase64String(userSignedPreKeyBase64), true, false);
            }
            else
            {
                userKeyPair = new Curve25519KeyPair();
                signedPreKeyPair = new Curve25519KeyPair();

                userUuid = await connection.RegisterUserAsync(EmailEntry.Text, userKeyPair, signedPreKeyPair, "Jevans", "Coolest alive");
                preferences["userUuid"] = userUuid;
                await _secureStorageService.SetAsync("userPrivateKey", Convert.ToBase64String(userKeyPair.EdPrivateKey));
                await _secureStorageService.SetAsync("userSignedPreKey", Convert.ToBase64String(signedPreKeyPair.XPrivateKey));
            }
            
            await DisplayAlert("Info", $"User UUID: {userUuid}", "Ok");
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            var preferences = Application.Current.Properties;

            preferences.Remove("userUuid");
            preferences.Remove("deviceUuid");
        }
    }
}

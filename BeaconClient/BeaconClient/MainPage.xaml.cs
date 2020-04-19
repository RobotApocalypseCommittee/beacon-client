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
                key = CryptoUtils.DeriveMasterKey("wrong boi", "jane.evans@westminster.org.uk");
                await DisplayAlert("Wrong Password Decryption:", key.DecryptString(cipherText, iv), "Ok");
            }
            catch (CryptographicException ex)
            {
                await DisplayAlert("Wrongful Decryption Failed (This is good):", ex.ToString(), "Ok");
            }
        }

        private async void OnButton2Clicked(object sender, EventArgs e)
        {
            string serverUrl = "https://localhost:8088";

            ISecureStorageService secureStorageService = DependencyService.Get<ISecureStorageService>();
            var preferences = Application.Current.Properties;
            
            string devicePrivateKeyBase64 = await secureStorageService.GetAsync("devicePrivateKey");
            Curve25519KeyPair deviceKeyPair;
            if (devicePrivateKeyBase64 is null)
            {
                deviceKeyPair = new Curve25519KeyPair();
                await secureStorageService.SetAsync("devicePrivateKey", Convert.ToBase64String(deviceKeyPair.EdPrivateKey));
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
            }

            await DisplayAlert("Info", $"Device UUID: {deviceUuid}", "Ok");

            string userUuid;
            Curve25519KeyPair userKeyPair;
            if (preferences.ContainsKey("userUuid"))
            {
                userUuid = (string) preferences["userUuid"];

                string userPrivateKeyBase64 = await secureStorageService.GetAsync("userPrivateKey");
                if (userPrivateKeyBase64 is null)
                {
                    throw new Exception("User Uuid found, but no private key!");
                }
                
                userKeyPair = new Curve25519KeyPair(Convert.FromBase64String(userPrivateKeyBase64), true, true);
            }
            else
            {
                userKeyPair = new Curve25519KeyPair();

                userUuid = await connection.RegisterUserAsync("jane.evans@gmail.com", userKeyPair, "Jevans", "Coolest alive");
                preferences["userUuid"] = userUuid;
                await secureStorageService.SetAsync("userPrivateKey", Convert.ToBase64String(userKeyPair.EdPrivateKey));
            }
            
            await DisplayAlert("Info", $"User UUID: {userUuid}", "Ok");
        }
    }
}

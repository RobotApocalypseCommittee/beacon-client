using System;
using System.Security.Cryptography;
using System.Text;
using Xamarin.Forms;

using BeaconClient.Crypto;
using BeaconClient.NativeDependencies;
using BeaconClient.Server;

namespace BeaconClient
{
    public partial class MainPage : ContentPage
    {
        private readonly ISecureStorageService _secureStorageService = DependencyService.Get<ISecureStorageService>();
        private readonly IPreferencesService _preferencesService = DependencyService.Get<IPreferencesService>();
        
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnButtonClicked(object sender, EventArgs e)
        {
            AES256Key key = CryptoUtils.DeriveMasterKey("secure boi", "jane.evans@westminster.org.uk");
            // Just an example
            byte[] mKey = new AES256Key().Key;

            byte[] plainText = Encoding.UTF8.GetBytes("SECRET");
            byte[] associatedData = Encoding.UTF8.GetBytes("AD, Not secret");
            byte[] cipherTextWithHmac = CryptoUtils.EncryptWithMessageKey(mKey, plainText, associatedData);
            string plainTextDecrypted = Encoding.UTF8.GetString(CryptoUtils.DecryptWithMessageKey(mKey, cipherTextWithHmac, associatedData));
            
            byte[] associatedDataBad = Encoding.UTF8.GetBytes("AD, wrong");
            byte[] mKeyBad = new AES256Key().Key;

            byte[] plainTextDecryptedAdBad = 
                CryptoUtils.DecryptWithMessageKey(mKey, cipherTextWithHmac, associatedDataBad);

            await DisplayAlert("Message Keys",
                $"Decrypted: {plainTextDecrypted}\nAD Bad null? {plainTextDecryptedAdBad is null}", "Ok");

            try
            {
                byte[] plainTextDecryptedKeyBad =
                    CryptoUtils.DecryptWithMessageKey(mKeyBad, cipherTextWithHmac, associatedData);
                await DisplayAlert("Bad Key gives null? (True is good)", (plainTextDecryptedKeyBad is null).ToString(), "Ok");
            }
            catch (CryptographicException ex)
            {
                await DisplayAlert("Bad Key Failed :)", ex.ToString(), "Ok");
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            _secureStorageService.Remove("hi");
            await _secureStorageService.SetAsync("hi", "epic gamer");
            watch.Stop();
            await DisplayAlert("SecureStorage", $"{await _secureStorageService.GetAsync("hi")}\n{watch.ElapsedMilliseconds} ms", "Ok");
        }

        private async void OnButton2Clicked(object sender, EventArgs e)
        {
            var preferences = _preferencesService;

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
                deviceUuid = preferences.Get("deviceUuid", null);
                connection = new ServerConnection(serverUrl, deviceKeyPair, deviceUuid);
            }
            else
            {
                connection = new ServerConnection(serverUrl, deviceKeyPair);
                deviceUuid = await connection.RegisterDeviceAsync();
                preferences.Set("deviceUuid", deviceUuid);
                await Application.Current.SavePropertiesAsync();
            }

            await DisplayAlert("Info", $"Device UUID: {deviceUuid}", "Ok");

            string userUuid;
            Curve25519KeyPair userKeyPair;
            Curve25519KeyPair signedPreKeyPair;
            if (preferences.ContainsKey("userUuid"))
            {
                userUuid = preferences.Get("userUuid", null);

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
                preferences.Set("userUuid", userUuid);
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

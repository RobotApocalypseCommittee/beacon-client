using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

using BeaconClient.Crypto;

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
            byte[] cipherText = key.EncryptStringToBytes("Hello World!");
            await DisplayAlert("Encrypted:", string.Join(", ", cipherText), "Ok");

            byte[] iv = key.IV;

            key = CryptoUtils.DeriveMasterKey("secure boi", "jane.evans@westminster.org.uk");
            await DisplayAlert("Decrypted:", key.DecryptBytesToString(cipherText, iv), "Ok");

            try
            {
                key = CryptoUtils.DeriveMasterKey("wrong boi", "jane.evans@westminster.org.uk");
                await DisplayAlert("Wrong Password Decryption:", key.DecryptBytesToString(cipherText, iv), "Ok");
            }
            catch (CryptographicException ex)
            {
                await DisplayAlert("Wrongful Decryption Failed (This is good):", ex.ToString(), "Ok");
            }
        }
    }
}

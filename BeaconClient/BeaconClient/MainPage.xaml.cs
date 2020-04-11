using System;
using System.Linq;
using System.Security.Cryptography;
using Xamarin.Forms;

using BeaconClient.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

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
            AES256Key key = CryptoUtils.DeriveMasterKey("secure boi", "jane.evans@westminster.org.uk");
            byte[] toEncrypt = {10, 12, 34, 0, 65};
            byte[] cipherText = key.Encrypt(toEncrypt);
            await DisplayAlert("Encrypted:", string.Join(", ", cipherText), "Ok");

            byte[] iv = key.IV;

            key = CryptoUtils.DeriveMasterKey("secure boi", "jane.evans@westminster.org.uk");
            await DisplayAlert("Decrypted:", string.Join(", ", key.DecryptBytes(cipherText, iv)), "Ok");

            try
            {
                key = CryptoUtils.DeriveMasterKey("wrong boi", "jane.evans@westminster.org.uk");
                await DisplayAlert("Wrong Password Decryption:", string.Join(", ", key.DecryptBytes(cipherText, iv)), "Ok");
            }
            catch (CryptographicException ex)
            {
                await DisplayAlert("Wrongful Decryption Failed (This is good):", ex.ToString(), "Ok");
            }

            /*for (int i = 0; i < 10001; i++)
            {
                var key2 = new Curve25519KeyPair();
                
                var key2pu = key2.EdPublicKey;
            
                var key3 = new Curve25519KeyPair(key2pu, false);
                
                if (!key2.XPublicKey.SequenceEqual(key3.XPublicKey)) {
                    Console.Out.WriteLine($"{key2.XPublicKey.SequenceEqual(key3.XPublicKey)}: {key2.XPublicKey[0]}, {key3.XPublicKey[0]}; {key2.XPublicKey[31]}, {key3.XPublicKey[31]}");

                    var bruh = string.Join(", ", key2.XPublicKey);
                    var bruh2 = string.Join(", ", key3.XPublicKey);
                    Console.Out.WriteLine($"{bruh} == {bruh2}");
                }
                
            }*/
            
            var key4 = new Curve25519KeyPair();
            var key5 = new Curve25519KeyPair();
            var key4pub = new Curve25519KeyPair(key4.EdPublicKey, false);

            byte[] toSign = {0x11, 0x12, 0x55, 0x12};
            var sig = key4.Sign(toSign);

            await DisplayAlert("DH Shared Secret Match?", key4.CalculateSharedSecret(key5.XPublicKey).SequenceEqual(key5.CalculateSharedSecret(key4.XPublicKey)).ToString(), "Ok");
            await DisplayAlert("Signature Match?", key4pub.Verify(toSign, sig).ToString(), "Ok");

            toSign[0]++;
            await DisplayAlert("Signature Not Match?", key4pub.Verify(toSign, sig).ToString(), "Ok");
            toSign[0]--;
            sig[0]++;
            await DisplayAlert("Signature Not Match 2?", key4pub.Verify(toSign, sig).ToString(), "Ok");

            DHParameters dhParams = new DHParameters(CryptoUtils.DhP, CryptoUtils.DhG);
            DHKeyGenerationParameters dhKeygenParams = new DHKeyGenerationParameters(new SecureRandom(), dhParams);
            DHKeyPairGenerator dhKeyPairGenerator = new DHKeyPairGenerator();
            dhKeyPairGenerator.Init(dhKeygenParams);

            dhKeyPairGenerator.GenerateKeyPair();
        }
    }
}

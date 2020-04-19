using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BeaconClient.NativeDependencies;
using Xamarin.Forms;

[assembly: Dependency(typeof(BeaconClient.Windows.NativeDependencies.SecureStorageService))]
namespace BeaconClient.Windows.NativeDependencies
{
    public class SecureStorageService : ISecureStorageService
    {
        private const string KeyPrefix = "BeaconClient-SecureStorage-";
        // It's a singleton in reality
        private static readonly PreferencesService PreferencesService = new PreferencesService();
        
        public async Task SetAsync(string key, string value)
        {
            key = KeyPrefix + key;
            
            await Task.Run(() =>
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);

                byte[] encBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                PreferencesService.Set(key, Convert.ToBase64String(encBytes));
            });
        }
        
        public async Task<string> GetAsync(string key)
        {
            key = KeyPrefix + key;
            
            return await Task.Run(() =>
            {
                string encString = PreferencesService.Get(key, null);
                if (encString is null) return null;
                
                byte[] encBytes = Convert.FromBase64String((string) encString);
                byte[] bytes = ProtectedData.Unprotect(encBytes, null, DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(bytes);
            });
        }

        public bool Remove(string key)
        {
            key = KeyPrefix + key;
            
            // If it doesn't exist, we should return true
            if (PreferencesService.ContainsKey(key))
                PreferencesService.Remove(key);
            
            // It will NEVER exist after this, so we always return true
            return true;
        }
    }
}
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
        public async Task SetAsync(string key, string value)
        {
            await Task.Run(async () =>
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);

                byte[] encBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                Application.Current.Properties[key] = Convert.ToBase64String(encBytes);
                await Application.Current.SavePropertiesAsync().ConfigureAwait(false);
            });
        }
        
        public async Task<string> GetAsync(string key)
        {
            return await Task.Run(() =>
            {
                if (!Application.Current.Properties.TryGetValue(key, out var encString))
                    return null;
                
                byte[] encBytes = Convert.FromBase64String((string) encString);
                byte[] bytes = ProtectedData.Unprotect(encBytes, null, DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(bytes);

            });
        }

        public bool Remove(string key)
        {
            // If it doesn't exist, we should return true
            if (!Application.Current.Properties.ContainsKey(key))
                return true;
            
            bool success = Application.Current.Properties.Remove(key);
            Application.Current.SavePropertiesAsync();
            return success;
        }
    }
}
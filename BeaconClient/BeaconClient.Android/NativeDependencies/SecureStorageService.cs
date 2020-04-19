using System.Threading.Tasks;
using BeaconClient.NativeDependencies;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(BeaconClient.Android.NativeDependencies.SecureStorageService))]
namespace BeaconClient.Android.NativeDependencies
{
    public class SecureStorageService : ISecureStorageService
    {
        public async Task SetAsync(string key, string value)
        {
            await SecureStorage.SetAsync(key, value).ConfigureAwait(false);
        }
        
        public async Task<string> GetAsync(string key)
        {
            return await SecureStorage.GetAsync(key).ConfigureAwait(false);
        }

        public bool Remove(string key)
        {
            return SecureStorage.Remove(key);
        }
    }
}

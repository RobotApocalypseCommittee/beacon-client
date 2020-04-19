using System.Threading.Tasks;
using BeaconClient.NativeDependencies;
using Foundation;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(BeaconClient.iOS.NativeDependencies.SecureStorageService))]
namespace BeaconClient.iOS.NativeDependencies
{
    [Preserve(AllMembers = true)]
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

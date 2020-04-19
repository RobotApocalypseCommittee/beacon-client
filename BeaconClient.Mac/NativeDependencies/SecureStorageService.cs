using System.Threading.Tasks;
using BeaconClient.NativeDependencies;
using Xamarin.Forms;

[assembly: Dependency(typeof(BeaconClient.Mac.NativeDependencies.SecureStorageService))]
namespace BeaconClient.Mac.NativeDependencies
{
    public class SecureStorageService : ISecureStorageService
    {
        public async Task SetAsync(string key, string value)
        {
            throw new System.NotImplementedException();
        }

        public async Task<string> GetAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(string key)
        {
            throw new System.NotImplementedException();
        }
    }
}
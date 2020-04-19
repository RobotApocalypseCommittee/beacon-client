using System.Threading.Tasks;

namespace BeaconClient.NativeDependencies
{
    public interface ISecureStorageService
    {
        Task SetAsync(string key, string value);
        // Returns null on unset key
        Task<string> GetAsync(string key);
        bool Remove(string key);
    }
}
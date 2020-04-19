namespace BeaconClient.NativeDependencies
{
    public interface IPreferencesService
    {
        bool ContainsKey(string key);
        string Get(string key, string defaultValue);
        void Remove(string key);
        void Set(string key, string value);
        void Clear();
    }
}
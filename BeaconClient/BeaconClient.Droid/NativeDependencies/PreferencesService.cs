using BeaconClient.NativeDependencies;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(BeaconClient.Droid.NativeDependencies.PreferencesService))]
namespace BeaconClient.Droid.NativeDependencies
{
    public class PreferencesService : IPreferencesService
    {
        public bool ContainsKey(string key)
        {
            return Preferences.ContainsKey(key);
        }

        public string Get(string key, string defaultValue)
        {
            return Preferences.Get(key, defaultValue);
        }

        public void Remove(string key)
        {
            Preferences.Remove(key);
        }

        public void Set(string key, string value)
        {
            Preferences.Set(key, value);
        }

        public void Clear()
        {
            Preferences.Clear();
        }
    }
}

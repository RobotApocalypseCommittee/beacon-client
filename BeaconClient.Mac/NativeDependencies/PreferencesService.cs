using BeaconClient.NativeDependencies;
using Foundation;
using Xamarin.Forms;

[assembly: Dependency(typeof(BeaconClient.Mac.NativeDependencies.PreferencesService))]
namespace BeaconClient.Mac.NativeDependencies
{
    public class PreferencesService : IPreferencesService
    {
        private static readonly object Locker = new object();
        
        public bool ContainsKey(string key)
        {
            lock (Locker)
            {
                return GetUserDefaults(null)[key] != null;
            }
        }

        public string Get(string key, string defaultValue)
        {
            lock (Locker)
            {
                using (var userDefaults = GetUserDefaults(null))
                {
                    return userDefaults[key] == null ? defaultValue : userDefaults.StringForKey(key);
                }
            }
        }

        public void Remove(string key)
        {
            lock (Locker)
            {
                using (var userDefaults = GetUserDefaults(null))
                {
                    if (userDefaults[key] != null)
                        userDefaults.RemoveObject(key);
                }
            }
        }

        public void Set(string key, string value)
        {
            lock (Locker)
            {
                using (var userDefaults = GetUserDefaults(null))
                {
                    if (value == null)
                    {
                        if (userDefaults[key] != null)
                            userDefaults.RemoveObject(key);
                        return;
                    }

                    userDefaults.SetString(value, key);
                }
            }
        }

        public void Clear()
        {
            lock (Locker)
            {
                using (var userDefaults = GetUserDefaults(null))
                {
                    var items = userDefaults.ToDictionary();

                    foreach (var item in items.Keys)
                    {
                        if (item is NSString nsString)
                            userDefaults.RemoveObject(nsString);
                    }
                }
            }
        }

        private static NSUserDefaults GetUserDefaults(string sharedName)
        {
            return !string.IsNullOrWhiteSpace(sharedName) ? new NSUserDefaults(sharedName, NSUserDefaultsType.SuiteName)
                : NSUserDefaults.StandardUserDefaults;
        }
    }
}
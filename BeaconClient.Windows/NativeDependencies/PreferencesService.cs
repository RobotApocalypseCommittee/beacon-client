using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using BeaconClient.NativeDependencies;
using Xamarin.Forms;
using PreferencesDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

[assembly: Dependency(typeof(BeaconClient.Windows.NativeDependencies.PreferencesService))]
namespace BeaconClient.Windows.NativeDependencies
{
    public class PreferencesService : IPreferencesService
    {
        private static readonly object Locker = new object();
        
        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeaconClient", "settings.dat");
        
        private static readonly PreferencesDictionary Preferences = new PreferencesDictionary();
        private static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(typeof(PreferencesDictionary));
        
        static PreferencesService()
        {
            if (File.Exists(SettingsPath))
            {
                using (var stream = File.OpenRead(SettingsPath))
                {
                    try
                    {
                        var readPreferences = (PreferencesDictionary)Serializer.ReadObject(stream);

                        if (readPreferences != null)
                        {
                            Preferences = readPreferences;
                        }
                    }
                    catch (SerializationException)
                    {
                        // if deserialization fails proceed with empty settings
                    }
                }
            }
            else
            {
                string directoryName = Path.GetDirectoryName(SettingsPath);
                
                if (!(directoryName is null) && !Directory.Exists(directoryName))
                {
                    // create folder for app settings
                    Directory.CreateDirectory(directoryName);
                }
            }

            if (!Preferences.ContainsKey(string.Empty))
            {
                Preferences.Add(string.Empty, new Dictionary<string, object>());
            }
        }
        
        private static void Save()
        {
            using (var stream = File.OpenWrite(SettingsPath))
            {
                Serializer.WriteObject(stream, Preferences);
            }
        }
        
        public bool ContainsKey(string key)
        {
            lock (Locker)
            {
                return Preferences.TryGetValue(string.Empty, out var inner) && inner.ContainsKey(key);
            }
        }

        public string Get(string key, string defaultValue)
        {
            lock (Locker)
            {
                if (Preferences.TryGetValue(string.Empty, out var inner) && inner.TryGetValue(key, out var value))
                {
                    return (string) value;
                }

                return defaultValue;
            }
        }

        public void Remove(string key)
        {
            lock (Locker)
            {
                if (!Preferences.TryGetValue(string.Empty, out var inner)) return;
                inner.Remove(key);
                Save();
            }
        }

        public void Set(string key, string value)
        {
            lock (Locker)
            {
                if (!Preferences.TryGetValue(string.Empty, out var inner))
                {
                    inner = new Dictionary<string, object>();
                    Preferences.Add(string.Empty, inner);
                }

                inner[key] = value;

                Save();
            }
        }

        public void Clear()
        {
            lock (Locker)
            {
                if (!Preferences.TryGetValue(string.Empty, out var inner)) return;
                inner.Clear();
                Save();
            }
        }
    }
}
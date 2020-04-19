using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BeaconClient.NativeDependencies;
using Foundation;
using Xamarin.Forms;

using Security;

[assembly: Dependency(typeof(BeaconClient.Mac.NativeDependencies.SecureStorageService))]
namespace BeaconClient.Mac.NativeDependencies
{
    public class SecureStorageService : ISecureStorageService
    {
        private const string Service = "com.bekos.BeaconClient.Mac.SecureStorage";
        private const string KeyPrefix = "BeaconClient-SecureStorage-";
        private const SecAccessible Accessible = SecAccessible.AfterFirstUnlockThisDeviceOnly;
        
        public Task SetAsync(string key, string value)
        {
            key = KeyPrefix + key;
            
            SecRecord record = new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = Service,
                Label = key,
                Accessible = Accessible,
                ValueData = NSData.FromString(value, NSStringEncoding.UTF8)
            };

            using (record)
            {
                SecStatusCode result = SecKeyChain.Add(record);
                switch (result)
                {
                    case SecStatusCode.DuplicateItem:
                    {
                        Debug.WriteLine("Duplicate item found. Attempting to remove and add again.");

                        if (Remove(key))
                        {
                            result = SecKeyChain.Add(record);
                            if (result != SecStatusCode.Success)
                                throw new Exception($"Error adding record: {result}");
                        }
                        else
                        {
                            throw new Exception($"Error removing record: {result}");
                        }
                    }
                        break;
                    case SecStatusCode.Success:
                        break;
                    default:
                        throw new Exception($"Error adding record: {result}");
                }
            }

            return Task.CompletedTask;
        }

        public Task<string> GetAsync(string key)
        {
            key = KeyPrefix + key;
            
            SecRecord record = new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = Service
            };
            
            using (record)
            using (SecRecord match = SecKeyChain.QueryAsRecord(record, out var result))
            {
                if (result == SecStatusCode.Success)
                    return Task.FromResult(NSString.FromData(match.ValueData, NSStringEncoding.UTF8).ToString());
                if (result == SecStatusCode.ItemNotFound)
                    return Task.FromResult<string>(null);
                
                throw new Exception($"Error querying record: {result}");
            }
        }

        public bool Remove(string key)
        {
            key = KeyPrefix + key;
            
            SecRecord record = new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = Service
            };

            using(record)
            using (SecRecord match = SecKeyChain.QueryAsRecord(record, out SecStatusCode result))
            {
                if (result == SecStatusCode.Success)
                {
                    result = SecKeyChain.Remove(record);
                    if (result != SecStatusCode.Success && result != SecStatusCode.ItemNotFound)
                        throw new Exception($"Error removing record: {result}");

                    return true;
                }
            }

            return false;
        }
    }
}
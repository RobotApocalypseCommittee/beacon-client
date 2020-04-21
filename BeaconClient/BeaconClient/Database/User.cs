using SQLite;

namespace BeaconClient.Database
{
    public class User
    {
        [PrimaryKey]
        public string Uuid { get; set; }
        
    }
}
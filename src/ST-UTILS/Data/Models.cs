// File: Data/PersonalBestDataModel.cs
namespace SurfTimer.Data
{
    public class PersonalBestDataModel : RunStats
    {
        public int ID { get; set; }
        public int Rank { get; set; }
    }

    public class MapInfoDataModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = "N/A";
        public string Author { get; set; } = "Unknown";
        public int Tier { get; set; }
        public int Stages { get; set; }
        public int Bonuses { get; set; }
        public bool Ranked { get; set; }
        public int DateAdded { get; set; }
        public int LastPlayed { get; set; }
    }


    public class MapRecordRunDataModel : RunStats
    {
        public int ID { get; set; }
        public int RunTime { get; set; }
        public int Type { get; set; }      // 0 = Map, 1 = Bonus, 2 = Stage
        public int Stage { get; set; }
        public int Style { get; set; }
        public string Name { get; set; } = "";
        public int TotalCount { get; set; }
        // public string ReplayFramesBase64 { get; set; } = "";
    }


    public class PlayerProfileDataModel
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public ulong SteamID { get; set; } = 0;
        public string Country { get; set; } = "";
        public int JoinDate { get; set; } = 0;
        public int LastSeen { get; set; } = 0;
        public int Connections { get; set; } = 0;
    }

    public class PlayerMapTimeDataModel : RunStats
    {
        public int ID { get; set; }
        public int RunTime { get; set; }
        public int Type { get; set; }  // 0 = Map, 1 = Bonus, 2 = Stage
        public int Stage { get; set; }
        public int Style { get; set; }
        public int Rank { get; set; }
    }

    public class MapTimeDataModel : RunStats
    {
        public int PlayerId { get; set; }
        public int MapId { get; set; }
        public int Style { get; set; }
        public int Type { get; set; } // 0 = Map, 1 = Bonus, 2 = Stage
        public int Stage { get; set; }
    }
}

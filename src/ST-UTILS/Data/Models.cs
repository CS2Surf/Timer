// File: Data/PersonalBestDataModel.cs
using System.Data;
using MySqlConnector;

namespace SurfTimer.Data
{
    public class PersonalBestDataModel : RunStats
    {
        public int ID { get; set; }
        public int Rank { get; set; }

        /// <summary>
        /// Assigns data from API response to the needed data model
        /// </summary>
        public PersonalBestDataModel(API_PersonalBest data)
        {
            ID = data.id;
            Ticks = data.run_time;
            Rank = data.rank;
            StartVelX = data.start_vel_x;
            StartVelY = data.start_vel_y;
            StartVelZ = data.start_vel_z;
            EndVelX = data.end_vel_x;
            EndVelY = data.end_vel_y;
            EndVelZ = data.end_vel_z;
            RunDate = data.run_date;
            ReplayFramesBase64 = data.replay_frames;
        }

        /// <summary>
        /// Assigns data from MySqlDataReader (MySQL query) to the needed data model
        /// </summary>
        public PersonalBestDataModel(MySqlDataReader data)
        {
            ID = data.GetInt32("id");
            Ticks = data.GetInt32("run_time");
            Rank = data.GetInt32("rank");
            StartVelX = (float)data.GetDouble("start_vel_x");
            StartVelY = (float)data.GetDouble("start_vel_y");
            StartVelZ = (float)data.GetDouble("start_vel_z");
            EndVelX = (float)data.GetDouble("end_vel_x");
            EndVelY = (float)data.GetDouble("end_vel_y");
            EndVelZ = (float)data.GetDouble("end_vel_z");
            RunDate = data.GetInt32("run_date");
        }
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

        /// <summary>
        /// Parameterless constructor for manual assigning of data
        /// </summary>
        public MapInfoDataModel() { }

        /// <summary>
        /// Assigns data from API response to the needed data model
        /// </summary>
        public MapInfoDataModel(API_MapInfo data)
        {
            ID = data.id;
            Name = data.name;
            Author = data.author;
            Tier = data.tier;
            Stages = data.stages;
            Bonuses = data.bonuses;
            Ranked = data.ranked == 1;
            DateAdded = data.date_added ?? 0;
            LastPlayed = data.last_played ?? 0;
        }

        /// <summary>
        /// Assigns data from MySqlDataReader (MySQL query) to the needed data model
        /// </summary>
        public MapInfoDataModel(MySqlDataReader data)
        {
            ID = data.GetInt32("id");
            Name = data.GetString("name");
            Author = data.GetString("author") ?? "Unknown";
            Tier = data.GetInt32("tier");
            Ranked = data.GetBoolean("ranked");
            DateAdded = data.GetInt32("date_added");
            LastPlayed = data.GetInt32("last_played");
        }
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

        /// <summary>
        /// Assigns data from API response to the needed data model
        /// </summary>
        public MapRecordRunDataModel(API_MapTime data)
        {
            ID = data.id;
            RunTime = data.run_time;
            Type = data.type;
            Stage = data.stage;
            Style = data.style;
            Name = data.name;
            StartVelX = (float)data.start_vel_x;
            StartVelY = (float)data.start_vel_y;
            StartVelZ = (float)data.start_vel_z;
            EndVelX = (float)data.end_vel_x;
            EndVelY = (float)data.end_vel_y;
            EndVelZ = (float)data.end_vel_z;
            RunDate = data.run_date;
            TotalCount = data.total_count;
            ReplayFramesBase64 = data.replay_frames;
        }

        /// <summary>
        /// Assigns data from MySqlDataReader (MySQL query) to the needed data model
        /// </summary>
        public MapRecordRunDataModel(MySqlDataReader data)
        {
            string replayFramesBase64;

            try
            {
                replayFramesBase64 = data.GetString("replay_frames");
            }
            catch (InvalidCastException)
            {
                byte[] replayFramesData = data.GetFieldValue<byte[]>("replay_frames");
                replayFramesBase64 = System.Text.Encoding.UTF8.GetString(replayFramesData);
            }

            ID = data.GetInt32("id");
            RunTime = data.GetInt32("run_time");
            Type = data.GetInt32("type");
            Stage = data.GetInt32("stage");
            Style = data.GetInt32("style");
            Name = data.GetString("name");
            StartVelX = data.GetFloat("start_vel_x");
            StartVelY = data.GetFloat("start_vel_y");
            StartVelZ = data.GetFloat("start_vel_z");
            EndVelX = data.GetFloat("end_vel_x");
            EndVelY = data.GetFloat("end_vel_y");
            EndVelZ = data.GetFloat("end_vel_z");
            RunDate = data.GetInt32("run_date");
            TotalCount = data.GetInt32("total_count");
            ReplayFramesBase64 = replayFramesBase64;
        }
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

        /// <summary>
        /// Parameterless constructor for manual assigning of data
        /// </summary>
        public PlayerProfileDataModel() { }

        /// <summary>
        /// Assigns data from API response to the needed data model
        /// </summary>
        public PlayerProfileDataModel(API_PlayerSurfProfile data)
        {
            ID = data.id;
            // SteamID = steamId;
            Name = data.name;
            Country = data.country;
            JoinDate = data.join_date;
            LastSeen = data.last_seen;
            Connections = data.connections;
        }

        /// <summary>
        /// Assigns data from MySqlDataReader (MySQL query) to the needed data model
        /// </summary>
        public PlayerProfileDataModel(MySqlDataReader data)
        {
            ID = data.GetInt32("id");
            // SteamID = data.GetString("steam_id");
            Name = data.GetString("name");
            Country = data.GetString("country");
            JoinDate = data.GetInt32("join_date");
            LastSeen = data.GetInt32("last_seen");
            Connections = data.GetInt32("connections");
        }
    }

    public class PlayerMapTimeDataModel : RunStats
    {
        public int ID { get; set; }
        public int RunTime { get; set; }
        public int Type { get; set; }  // 0 = Map, 1 = Bonus, 2 = Stage
        public int Stage { get; set; }
        public int Style { get; set; }
        public int Rank { get; set; }

        /// <summary>
        /// Assigns data from API response to the needed data model
        /// </summary>
        public PlayerMapTimeDataModel(API_PersonalBest data)
        {
            RunTime = data.run_time;
            Type = data.type;
            Stage = data.stage;
            Style = data.style;
            Rank = data.rank;
            StartVelX = (float)data.start_vel_x;
            StartVelY = (float)data.start_vel_y;
            StartVelZ = (float)data.start_vel_z;
            EndVelX = (float)data.end_vel_x;
            EndVelY = (float)data.end_vel_y;
            EndVelZ = (float)data.end_vel_z;
            RunDate = data.run_date;
            ReplayFramesBase64 = data.replay_frames;
        }

        /// <summary>
        /// Assigns data from MySqlDataReader (MySQL query) to the needed data model
        /// </summary>
        public PlayerMapTimeDataModel(MySqlDataReader data)
        {
            ID = data.GetInt32("id");
            RunTime = data.GetInt32("run_time");
            Type = data.GetInt32("type");
            Stage = data.GetInt32("stage");
            Style = data.GetInt32("style");
            Rank = data.GetInt32("rank");
            StartVelX = (float)data.GetDouble("start_vel_x");
            StartVelY = (float)data.GetDouble("start_vel_y");
            StartVelZ = (float)data.GetDouble("start_vel_z");
            EndVelX = (float)data.GetDouble("end_vel_x");
            EndVelY = (float)data.GetDouble("end_vel_y");
            EndVelZ = (float)data.GetDouble("end_vel_z");
            RunDate = data.GetInt32("run_date");
        }
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

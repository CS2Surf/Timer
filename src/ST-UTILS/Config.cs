using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

public static class Config
{
    public static string PluginName => Assembly.GetExecutingAssembly().GetName().Name ?? "";
    public static string PluginPrefix = $"[{ChatColors.DarkBlue}CS2 Surf{ChatColors.Default}]"; // To-do: make configurable
    public static string PluginPath => $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/plugins/{PluginName}/";
    public static string PluginSurfConfig = $"{Server.GameDirectory}/csgo/cfg/{PluginName}/{PluginName}.json";
    public static string ApiUrl => API.GetApiUrl();
    public static string DbConnectionString => MySQL.GetConnectionString();

    /// <summary>
    /// Placeholder for amount of styles
    /// </summary>
    public static List<int> Styles = new List<int> { 0 }; // Add all supported style IDs

    public static bool ReplaysEnabled => true;
    public static int ReplaysPre => 64;
    // Helper class/methods for configuration loading
    private static class ConfigLoader
    {
        private static readonly Dictionary<string, JsonDocument> _configDocuments = new();

        public static JsonDocument GetConfigDocument(string configPath)
        {
            if (!_configDocuments.ContainsKey(configPath))
            {
                var fullPath = Server.GameDirectory + configPath;
                _configDocuments[configPath] = JsonDocument.Parse(File.ReadAllText(fullPath));
            }
            return _configDocuments[configPath];
        }
    }

    public static class API
    {
        private const string API_CONFIG_PATH = "/csgo/cfg/SurfTimer/api_config.json";

        private static JsonDocument ConfigDocument => ConfigLoader.GetConfigDocument(API_CONFIG_PATH);

        /// <summary>
        ///   Retrieves the `api_url` string from the configuration path
        /// </summary>
        /// <returns>A <see cref="string"/> value containing the URL.</returns>
        public static string GetApiUrl()
        {
            return ConfigDocument.RootElement.GetProperty("api_url").GetString()!;
        }

        /// <summary>
        ///   Retrieves the `api_enabled` value from the configuration path
        /// </summary>
        /// <returns>A <see cref="bool"/> value for whether the API should be used.</returns>
        public static bool GetApiOnly()
        {
            return ConfigDocument.RootElement.GetProperty("api_enabled").GetBoolean();
        }

        /// <summary>
        ///   Contains all the endpoints used by the API for the SurfTimer plugin.
        /// </summary>
        public static class Endpoints
        {
            // Map.cs related endpoints
            public const string ENDPOINT_MAP_GET_INFO = "/surftimer/mapinfo?mapname={0}";
            public const string ENDPOINT_MAP_INSERT_INFO = "/surftimer/insertmap";
            public const string ENDPOINT_MAP_UPDATE_INFO = "/surftimer/updateMap";
            // public const string ENDPOINT_MAP_GET_RUNS = "/surftimer/maptotals?map_id={0}&style={1}";
            public const string ENDPOINT_MAP_GET_RUNS = "/surftimer/maprunsdata?map_id={0}&style={1}&type={2}";
            public const string ENDPOINT_MAP_GET_RUN_CPS = "/surftimer/mapcheckpointsdata?maptime_id={0}";

            // CurrentRun.cs
            public const string ENDPOINT_CR_SAVE_STAGE_TIME = "/surftimer/savestagetime";
        }
    }

    public static class MySQL
    {
        private const string DB_CONFIG_PATH = "/csgo/cfg/SurfTimer/database.json";

        private static JsonDocument ConfigDocument => ConfigLoader.GetConfigDocument(DB_CONFIG_PATH);

        /// <summary>
        ///   Retrieves the connection details for connecting to the MySQL Database
        /// </summary>
        /// <returns>A connection <see cref="string"/></returns>
        public static string GetConnectionString()
        {
            string host = ConfigDocument.RootElement.GetProperty("host").GetString()!;
            string database = ConfigDocument.RootElement.GetProperty("database").GetString()!;
            string user = ConfigDocument.RootElement.GetProperty("user").GetString()!;
            string password = ConfigDocument.RootElement.GetProperty("password").GetString()!;
            int port = ConfigDocument.RootElement.GetProperty("port").GetInt32()!;
            int timeout = ConfigDocument.RootElement.GetProperty("timeout").GetInt32()!;

            string connString = $"server={host};user={user};password={password};database={database};port={port};connect timeout={timeout};";

            // Console.WriteLine($"============= [CS2 Surf] Extracted connection string: {connString}");

            return connString;
        }

        /// <summary>
        ///   Contains all the queries used by MySQL for the SurfTimer plugin.
        /// </summary>
        public static class Queries
        {
            // Map.cs related queries
            public const string DB_QUERY_MAP_GET_RUNS = @"
                SELECT MapTimes.*, Player.name
                FROM MapTimes
                JOIN Player ON MapTimes.player_id = Player.id
                WHERE MapTimes.map_id = {0} AND MapTimes.style = {1} AND MapTimes.type = {2}
                ORDER BY MapTimes.run_time ASC;
            "; // Deprecated
            public const string DB_QUERY_MAP_GET_INFO = "SELECT * FROM Maps WHERE name='{0}';";
            public const string DB_QUERY_MAP_INSERT_INFO = "INSERT INTO Maps (name, author, tier, stages, ranked, date_added, last_played) VALUES ('{0}', '{1}', {2}, {3}, {4}, {5}, {5})"; // "INSERT INTO Maps (name, author, tier, stages, ranked, date_added, last_played) VALUES ('{MySqlHelper.EscapeString(Name)}', 'Unknown', {this.Stages}, {this.Bonuses}, 0, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()})"
            public const string DB_QUERY_MAP_UPDATE_INFO_FULL = "UPDATE Maps SET last_played={0}, stages={1}, bonuses={2} WHERE id={3};";
            public const string DB_QUERY_MAP_GET_RECORD_RUNS_AND_COUNT = @"
            SELECT 
                ranked_times.*
            FROM (
                SELECT 
                    MapTimes.*,
                    Player.name,
                    ROW_NUMBER() OVER (
                        PARTITION BY MapTimes.type, MapTimes.stage 
                        ORDER BY MapTimes.run_time ASC
                    ) AS row_num,
                    COUNT(*) OVER (PARTITION BY MapTimes.type, MapTimes.stage) AS total_count
                FROM MapTimes
                JOIN Player ON MapTimes.player_id = Player.id
                WHERE MapTimes.map_id = {0}
            ) AS ranked_times
            WHERE ranked_times.row_num = 1;";


            // PlayerStats.cs related queries
            public const string DB_QUERY_PS_GET_ALL_RUNTIMES = @"
                SELECT mainquery.*, (SELECT COUNT(*) FROM `MapTimes` AS subquery 
                WHERE subquery.`map_id` = mainquery.`map_id` AND subquery.`style` = mainquery.`style` 
                AND subquery.`run_time` <= mainquery.`run_time` AND subquery.`type` = mainquery.`type` AND subquery.`stage` = mainquery.`stage`) AS `rank` FROM `MapTimes` AS mainquery 
                WHERE mainquery.`player_id` = {0} AND mainquery.`map_id` = {1}; 
            "; // Deprecated

            // PersonalBest.cs related queries
            public const string DB_QUERY_PB_GET_TYPE_RUNTIME = @"
                SELECT mainquery.*, (SELECT COUNT(*) FROM `MapTimes` AS subquery 
                WHERE subquery.`map_id` = mainquery.`map_id` AND subquery.`style` = mainquery.`style` 
                AND subquery.`run_time` <= mainquery.`run_time` AND subquery.`type` = mainquery.`type` AND subquery.`stage` = mainquery.`stage`) AS `rank` FROM `MapTimes` AS mainquery 
                WHERE mainquery.`player_id` = {0} AND mainquery.`map_id` = {1} AND mainquery.`type` = {2} AND mainquery.`style` = {3}; 
            ";
            public const string DB_QUERY_PB_GET_SPECIFIC_MAPTIME_DATA = @"
                SELECT mainquery.*, (SELECT COUNT(*) FROM `MapTimes` AS subquery 
                WHERE subquery.`map_id` = mainquery.`map_id` AND subquery.`style` = mainquery.`style` 
                AND subquery.`run_time` <= mainquery.`run_time` AND subquery.`type` = mainquery.`type` AND subquery.`stage` = mainquery.`stage`) AS `rank` FROM `MapTimes` AS mainquery 
                WHERE mainquery.`id` = {0}; 
            ";
            public const string DB_QUERY_PB_GET_CPS = "SELECT * FROM `Checkpoints` WHERE `maptime_id` = {0};";

            // CurrentRun.cs related queries
            public const string DB_QUERY_CR_INSERT_TIME = @"
                INSERT INTO `MapTimes` 
                (`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`, `replay_frames`) 
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, 
                {6}, {7}, {8}, {9}, {10}, {11}, {12}, '{13}') 
                ON DUPLICATE KEY UPDATE run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), 
                start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date), replay_frames=VALUES(replay_frames);
            ";
            public const string DB_QUERY_CR_INSERT_CP = @"
                INSERT INTO `Checkpoints` 
                (`maptime_id`, `cp`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, 
                `end_vel_x`, `end_vel_y`, `end_vel_z`, `attempts`, `end_touch`) 
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}) 
                ON DUPLICATE KEY UPDATE 
                run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), 
                end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), attempts=VALUES(attempts), end_touch=VALUES(end_touch);
            ";

            // ReplayPlayer.cs related queries
            public const string DB_QUERY_RP_LOAD_REPLAY = @"
                SELECT MapTimes.replay_frames, MapTimes.run_time, Player.name
                FROM MapTimes
                JOIN Player ON MapTimes.player_id = Player.id
                WHERE MapTimes.id={0};
            ";

            // Players.cs related queries
            public const string DB_QUERY_PP_GET_PROFILE = "SELECT * FROM `Player` WHERE `steam_id` = {0} LIMIT 1;";
            public const string DB_QUERY_PP_INSERT_PROFILE = @"
                INSERT INTO `Player` (`name`, `steam_id`, `country`, `join_date`, `last_seen`, `connections`) 
                VALUES ('{0}', {1}, '{2}', {3}, {4}, {5});
            ";
            public const string DB_QUERY_PP_UPDATE_PROFILE = @"
                UPDATE `Player` SET country = '{0}', 
                `last_seen` = {1}, `connections` = `connections` + 1, `name` = '{3}'
                WHERE `id` = {2} LIMIT 1;
            ";
        }
    }
}
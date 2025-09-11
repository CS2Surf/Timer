using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API;

namespace SurfTimer;

public static class Config
{
    public static readonly string PluginLogo = """
                                                
              ____________    ____         ___
             / ___/ __/_  |  / __/_ ______/ _/
            / /___\ \/ __/  _\ \/ // / __/ _/ 
            \___/___/____/ /___/\_,_/_/ /_/   
        """;
    public static string PluginName => Assembly.GetExecutingAssembly().GetName().Name ?? "";
    public static readonly string PluginPrefix = LocalizationService.LocalizerNonNull["prefix"];
    public static string PluginPath =>
        $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/plugins/{PluginName}/";
    public static string ApiUrl => Api.GetApiUrl();

    /// <summary>
    /// Placeholder for amount of styles
    /// </summary>
    public static readonly ImmutableList<int> Styles = [0]; // Add all supported style IDs

    public static readonly bool ReplaysEnabled = TimerSettings.GetReplaysEnabled();
    public static readonly int ReplaysPre = TimerSettings.GetReplaysPre();

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

    /// <summary>
    /// Values from `timer_settings.json`
    /// </summary>
    private static class TimerSettings
    {
        private const string TIMER_CONFIG_PATH = "/csgo/cfg/SurfTimer/timer_settings.json";
        private static JsonDocument ConfigDocument =>
            ConfigLoader.GetConfigDocument(TIMER_CONFIG_PATH);

        public static bool GetReplaysEnabled()
        {
            return ConfigDocument.RootElement.GetProperty("replays_enabled").GetBoolean();
        }

        public static int GetReplaysPre()
        {
            return ConfigDocument.RootElement.GetProperty("replays_pre").GetInt32();
        }
    }

    public static class Api
    {
        private const string API_CONFIG_PATH = "/csgo/cfg/SurfTimer/api_config.json";
        private static JsonDocument ConfigDocument =>
            ConfigLoader.GetConfigDocument(API_CONFIG_PATH);

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
            public const string ENDPOINT_PING = "/api/Utilities/ping/clientUnix={0}";

            // Map.cs related endpoints
            public const string ENDPOINT_MAP_GET_INFO = "/api/Map/mapName={0}";
            public const string ENDPOINT_MAP_INSERT_INFO = "/api/Map";
            public const string ENDPOINT_MAP_UPDATE_INFO = "/api/Map/mapId={0}";
            public const string ENDPOINT_MAP_GET_RUNS = "/api/Map/mapId={0}";
            public const string ENDPOINT_MAP_GET_RUN_CPS =
                "/api/PersonalBest/checkpoints/mapTimeId={0}";

            // CurrentRun.cs
            public const string ENDPOINT_CR_SAVE_MAP_TIME = "/api/CurrentRun/saveMapTime";
            public const string ENDPOINT_CR_UPDATE_MAP_TIME =
                "/api/CurrentRun/updateMapTime/mapTimeId={0}";
            public const string ENDPOINT_CR_SAVE_STAGE_TIME = "/surftimer/savestagetime";

            // PersonalBest.cs
            public const string ENDPOINT_MAP_GET_PB_BY_PLAYER =
                "/api/PersonalBest/playerId={0}&mapId={1}&type={2}&style={3}";
            public const string ENDPOINT_MAP_GET_PB_BY_ID =
                "/api/PersonalBest/runById/mapTimeId={0}";

            // PlayerProfile.cs
            public const string ENDPOINT_PP_GET_PROFILE = "/api/PlayerProfile/steamId={0}";
            public const string ENDPOINT_PP_INSERT_PROFILE = "/api/PlayerProfile";
            public const string ENDPOINT_PP_UPDATE_PROFILE = "/api/PlayerProfile/playerId={0}";

            // PlayerStats.cs
            public const string ENDPOINT_PS_GET_PLAYER_MAP_DATA =
                "/api/PlayerStats/playerId={0}&mapId={1}";
        }
    }

    public static class MySql
    {
        private const string DB_CONFIG_PATH = "/csgo/cfg/SurfTimer/database.json";
        private static JsonDocument ConfigDocument =>
            ConfigLoader.GetConfigDocument(DB_CONFIG_PATH);

        /// <summary>
        /// Retrieves the connection details for connecting to the MySQL Database
        /// </summary>
        /// <returns>A connection string</returns>
        public static string GetConnectionString()
        {
            string host = ConfigDocument.RootElement.GetProperty("host").GetString()!;
            string database = ConfigDocument.RootElement.GetProperty("database").GetString()!;
            string user = ConfigDocument.RootElement.GetProperty("user").GetString()!;
            string password = ConfigDocument.RootElement.GetProperty("password").GetString()!;
            int port = ConfigDocument.RootElement.GetProperty("port").GetInt32()!;
            int timeout = ConfigDocument.RootElement.GetProperty("timeout").GetInt32()!;

            string connString =
                $"Server={host};User={user};Password={password};Database={database};Port={port};Connect Timeout={timeout};Allow User Variables=true";

            return connString;
        }
    }
}

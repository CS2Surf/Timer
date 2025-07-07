using MySqlConnector;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Data;

namespace SurfTimer.Data
{
    public class MySqlDataAccessService : IDataAccessService
    {
        private readonly ILogger<MySqlDataAccessService> _logger;

        public MySqlDataAccessService()
        {
            _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<MySqlDataAccessService>>();
        }

        /* PersonalBest.cs */
        /// <summary>
        /// Loads the Checkpoint data for the given MapTime_ID. Used for loading player's personal bests and Map's world records.
        /// Bonus and Stage runs should NOT have any checkpoints.
        /// </summary>
        public async Task<Dictionary<int, Checkpoint>> LoadCheckpointsAsync(int runId, [CallerMemberName] string methodName = "")
        {
            _logger.LogInformation("[{ClassName}] {MethodName} -> LoadCheckpointsAsync -> Using MySQL data access service.",
                nameof(MySqlDataAccessService), methodName
            );

            var dict = new Dictionary<int, Checkpoint>();

            using (var results = await SurfTimer.DB.QueryAsync(
                       string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_CPS, runId)))
            {
                if (results == null || !results.HasRows)
                    return dict;

                while (results.Read())
                {
                    var cp = new Checkpoint(
                        results.GetInt32("cp"),
                        results.GetInt32("run_time"),
                        results.GetFloat("start_vel_x"),
                        results.GetFloat("start_vel_y"),
                        results.GetFloat("start_vel_z"),
                        results.GetFloat("end_vel_x"),
                        results.GetFloat("end_vel_y"),
                        results.GetFloat("end_vel_z"),
                        results.GetInt32("end_touch"),
                        results.GetInt32("attempts")
                    );
                    cp.ID = results.GetInt32("cp");
                    dict[cp.CP] = cp;
                }
            }

            _logger.LogInformation("[{ClassName}] {MethodName} -> LoadCheckpointsAsync -> Found {Count} checkpoints.",
                nameof(MySqlDataAccessService), methodName, dict.Count
            );

            return dict;
        }

        public async Task<PersonalBestDataModel?> LoadPersonalBestRunAsync(
            int? pbId, int playerId, int mapId, int type, int style, [CallerMemberName] string methodName = ""
        )
        {
            _logger.LogInformation("[{ClassName}] {MethodName} -> LoadPersonalBestRunAsync -> Using MySQL data access service.",
                nameof(MySqlDataAccessService), methodName
            );

            string sql = pbId == null || pbId == -1
                ? string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_TYPE_RUNTIME,
                               playerId, mapId, type, style)
                : string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_SPECIFIC_MAPTIME_DATA,
                               pbId.Value);

            using var results = await SurfTimer.DB.QueryAsync(sql);
            if (results == null || !results.HasRows)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> LoadPersonalBestRunAsync -> No data found. PersonalBestID {PbID} | PlayerID {PlayerID} | MapID {MapID} | Type {Type} | Style {Style}",
                    nameof(MySqlDataAccessService), methodName, pbId, playerId, mapId, type, style
                );
                return null;
            }

            // read the first (and only) row
            await results.ReadAsync();

            _logger.LogInformation("[{ClassName}] {MethodName} -> LoadPersonalBestRunAsync -> Found data for PersonalBestID {PbID} | PlayerID {PlayerID} | MapID {MapID} | Type {Type} | Style {Style}",
                nameof(MySqlDataAccessService), methodName, pbId, playerId, mapId, type, style
            );

            return new PersonalBestDataModel
            {
                ID = results.GetInt32("id"),
                Ticks = results.GetInt32("run_time"),
                Rank = results.GetInt32("rank"),
                StartVelX = (float)results.GetDouble("start_vel_x"),
                StartVelY = (float)results.GetDouble("start_vel_y"),
                StartVelZ = (float)results.GetDouble("start_vel_z"),
                EndVelX = (float)results.GetDouble("end_vel_x"),
                EndVelY = (float)results.GetDouble("end_vel_y"),
                EndVelZ = (float)results.GetDouble("end_vel_z"),
                RunDate = results.GetInt32("run_date")
            };
        }


        /* Map.cs */
        public async Task<MapInfoDataModel?> GetMapInfoAsync(string mapName, [CallerMemberName] string methodName = "")
        {
            using var mapData = await SurfTimer.DB.QueryAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_MAP_GET_INFO, MySqlHelper.EscapeString(mapName)));

            if (mapData.HasRows && mapData.Read())
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> GetMapInfoAsync -> Found MapInfo data",
                    nameof(MySqlDataAccessService), methodName
                );

                return new MapInfoDataModel
                {
                    ID = mapData.GetInt32("id"),
                    Name = mapName,
                    Author = mapData.GetString("author") ?? "Unknown",
                    Tier = mapData.GetInt32("tier"),
                    Ranked = mapData.GetBoolean("ranked"),
                    DateAdded = mapData.GetInt32("date_added"),
                    LastPlayed = mapData.GetInt32("last_played"),
                };
            }

            return null;
        }

        public async Task<int> InsertMapInfoAsync(MapInfoDataModel mapInfo, [CallerMemberName] string methodName = "")
        {
            // int rowsWritten = await SurfTimer.DB.WriteAsync(
            var (rowsWritten, lastId) = await SurfTimer.DB.WriteAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_MAP_INSERT_INFO,
                    MySqlHelper.EscapeString(mapInfo.Name),
                    MySqlHelper.EscapeString(mapInfo.Author),
                    mapInfo.Tier,
                    mapInfo.Stages,
                    mapInfo.Bonuses,
                    mapInfo.Ranked ? 1 : 0,
                    (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            );

            if (rowsWritten != 1)
            {
                throw new Exception($"Failed to insert new map '{mapInfo.Name}' into database.");
            }

            return (int)lastId;
        }

        public async Task UpdateMapInfoAsync(MapInfoDataModel mapInfo, [CallerMemberName] string methodName = "")
        {
            string updateQuery = string.Format(
                Config.MySQL.Queries.DB_QUERY_MAP_UPDATE_INFO_FULL,
                (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                mapInfo.Stages,
                mapInfo.Bonuses,
                mapInfo.ID
            );

            var (rowsUpdated, lastId) = await SurfTimer.DB.WriteAsync(updateQuery);
            if (rowsUpdated != 1)
            {
                throw new Exception($"Failed to update map '{mapInfo.Name}' (ID {mapInfo.ID}) in database.");
            }
        }

        public async Task<List<MapRecordRunDataModel>> GetMapRecordRunsAsync(int mapId, [CallerMemberName] string methodName = "")
        {
            var runs = new List<MapRecordRunDataModel>();

            using var results = await SurfTimer.DB.QueryAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_MAP_GET_RECORD_RUNS_AND_COUNT, mapId));

            if (results.HasRows)
            {
                while (results.Read())
                {
                    string replayFramesBase64;

                    try
                    {
                        replayFramesBase64 = results.GetString("replay_frames");
                    }
                    catch (InvalidCastException)
                    {
                        byte[] replayFramesData = results.GetFieldValue<byte[]>("replay_frames");
                        replayFramesBase64 = System.Text.Encoding.UTF8.GetString(replayFramesData);
                    }

                    runs.Add(new MapRecordRunDataModel
                    {
                        ID = results.GetInt32("id"),
                        RunTime = results.GetInt32("run_time"),
                        Type = results.GetInt32("type"),
                        Stage = results.GetInt32("stage"),
                        Style = results.GetInt32("style"),
                        Name = results.GetString("name"),
                        StartVelX = results.GetFloat("start_vel_x"),
                        StartVelY = results.GetFloat("start_vel_y"),
                        StartVelZ = results.GetFloat("start_vel_z"),
                        EndVelX = results.GetFloat("end_vel_x"),
                        EndVelY = results.GetFloat("end_vel_y"),
                        EndVelZ = results.GetFloat("end_vel_z"),
                        RunDate = results.GetInt32("run_date"),
                        TotalCount = results.GetInt32("total_count"),
                        ReplayFramesBase64 = replayFramesBase64
                    });
                }
            }

            return runs;
        }



        /* PlayerProfile.cs */
        public async Task<PlayerProfileDataModel?> GetPlayerProfileAsync(ulong steamId, [CallerMemberName] string methodName = "")
        {
            using var playerData = await SurfTimer.DB.QueryAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_PP_GET_PROFILE, steamId));

            if (playerData.HasRows && playerData.Read())
            {
                return new PlayerProfileDataModel
                {
                    ID = playerData.GetInt32("id"),
                    SteamID = steamId,
                    Name = playerData.GetString("name"),
                    Country = playerData.GetString("country"),
                    JoinDate = playerData.GetInt32("join_date"),
                    LastSeen = playerData.GetInt32("last_seen"),
                    Connections = playerData.GetInt32("connections")
                };
            }

            return null;
        }

        public async Task<int> InsertPlayerProfileAsync(PlayerProfileDataModel profile, [CallerMemberName] string methodName = "")
        {
            int joinDate = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var (rowsInserted, lastId) = await SurfTimer.DB.WriteAsync(string.Format(
                Config.MySQL.Queries.DB_QUERY_PP_INSERT_PROFILE,
                MySqlConnector.MySqlHelper.EscapeString(profile.Name),
                profile.SteamID,
                profile.Country,
                joinDate,
                joinDate,
                1));

            if (rowsInserted != 1)
                throw new Exception($"Failed to insert new player '{profile.Name}' ({profile.SteamID}).");

            return (int)lastId;
        }

        public async Task UpdatePlayerProfileAsync(PlayerProfileDataModel profile, [CallerMemberName] string methodName = "")
        {
            int lastSeen = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var (rowsUpdated, lastId) = await SurfTimer.DB.WriteAsync(string.Format(
                Config.MySQL.Queries.DB_QUERY_PP_UPDATE_PROFILE,
                profile.Country,
                lastSeen,
                profile.ID,
                MySqlConnector.MySqlHelper.EscapeString(profile.Name)));

            if (rowsUpdated != 1)
                throw new Exception($"Failed to update player '{profile.Name}' ({profile.SteamID}).");
        }



        /* PlayerStats.cs */
        public async Task<List<PlayerMapTimeDataModel>> GetPlayerMapTimesAsync(int playerId, int mapId, [CallerMemberName] string methodName = "")
        {
            var mapTimes = new List<PlayerMapTimeDataModel>();

            using var results = await SurfTimer.DB.QueryAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_PS_GET_ALL_RUNTIMES, playerId, mapId));

            if (results.HasRows)
            {
                while (results.Read())
                {
                    mapTimes.Add(new PlayerMapTimeDataModel
                    {
                        ID = results.GetInt32("id"),
                        RunTime = results.GetInt32("run_time"),
                        Type = results.GetInt32("type"),
                        Stage = results.GetInt32("stage"),
                        Style = results.GetInt32("style"),
                        Rank = results.GetInt32("rank"),
                        StartVelX = (float)results.GetDouble("start_vel_x"),
                        StartVelY = (float)results.GetDouble("start_vel_y"),
                        StartVelZ = (float)results.GetDouble("start_vel_z"),
                        EndVelX = (float)results.GetDouble("end_vel_x"),
                        EndVelY = (float)results.GetDouble("end_vel_y"),
                        EndVelZ = (float)results.GetDouble("end_vel_z"),
                        RunDate = results.GetInt32("run_date")
                    });
                }
            }

            return mapTimes;
        }



        /* CurrentRun.cs */
        public async Task<int> InsertMapTimeAsync(MapTimeDataModel mapTime, [CallerMemberName] string methodName = "")
        {
            var (rowsInserted, lastId) = await SurfTimer.DB.WriteAsync(
                string.Format(
                    Config.MySQL.Queries.DB_QUERY_CR_INSERT_TIME,
                    mapTime.PlayerId,
                    mapTime.MapId,
                    mapTime.Style,
                    mapTime.Type,
                    mapTime.Stage,
                    mapTime.Ticks,
                    mapTime.StartVelX,
                    mapTime.StartVelY,
                    mapTime.StartVelZ,
                    mapTime.EndVelX,
                    mapTime.EndVelY,
                    mapTime.EndVelZ,
                    mapTime.RunDate,
                    mapTime.ReplayFramesBase64)
            );

            if (rowsInserted <= 0)
            {
                throw new Exception($"Failed to insert map time for PlayerId {mapTime.PlayerId}.");
            }

            // Write the checkpoints after we have the `lastId`
            if (mapTime.Checkpoints != null && mapTime.Checkpoints.Count > 0)
            {
                var commands = new List<string>();
                foreach (var cp in mapTime.Checkpoints.Values)
                {
                    commands.Add(string.Format(
                        Config.MySQL.Queries.DB_QUERY_CR_INSERT_CP,
                        lastId, cp.CP, cp.Ticks, cp.StartVelX, cp.StartVelY, cp.StartVelZ,
                        cp.EndVelX, cp.EndVelY, cp.EndVelZ, cp.Attempts, cp.EndTouch));
                }
                await SurfTimer.DB.TransactionAsync(commands);
            }


            return (int)lastId;
        }

        // public async Task SaveRunCheckpointsAsync(int mapTimeId, IEnumerable<Checkpoint> checkpoints, [CallerMemberName] string methodName = "")
        // {
        //     var commands = new List<string>();

        //     foreach (var cp in checkpoints)
        //     {
        //         commands.Add(string.Format(
        //             Config.MySQL.Queries.DB_QUERY_CR_INSERT_CP,
        //             mapTimeId, cp.CP, cp.Ticks, cp.StartVelX, cp.StartVelY, cp.StartVelZ,
        //             cp.EndVelX, cp.EndVelY, cp.EndVelZ, cp.Attempts, cp.EndTouch));
        //     }

        //     await SurfTimer.DB.TransactionAsync(commands);
        // }

    }
}

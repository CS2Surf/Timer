using System.Runtime.CompilerServices;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Shared.DTO;
using SurfTimer.Shared.Entities;
using SurfTimer.Shared.Sql;

namespace SurfTimer.Data
{
    public class MySqlDataAccessService : IDataAccessService
    {
        private readonly ILogger<MySqlDataAccessService> _logger;

        /// <summary>
        /// Add/load data using MySQL connection and queries.
        /// </summary>
        public MySqlDataAccessService()
        {
            _logger = SurfTimer.ServiceProvider.GetRequiredService<
                ILogger<MySqlDataAccessService>
            >();
        }

        public async Task<bool> PingAccessService([CallerMemberName] string methodName = "")
        {
            try
            {
                var val = await SurfTimer.DB.QueryFirstOrDefaultAsync<int>(Queries.DB_QUERY_PING);

                var reachable = val != 0;

                if (reachable)
                {
                    _logger.LogInformation(
                        "[{ClassName}] {MethodName} -> PingAccessService -> MySQL is reachable",
                        nameof(MySqlDataAccessService),
                        methodName
                    );
                }

                return reachable;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex,
                    "[{ClassName}] {MethodName} -> PingAccessService -> MySQL is unreachable",
                    nameof(MySqlDataAccessService),
                    methodName
                );
                return false;
            }
        }

        /* PersonalBest.cs */
        public async Task<Dictionary<int, CheckpointEntity>> LoadCheckpointsAsync(
            int runId,
            [CallerMemberName] string methodName = ""
        )
        {
            // Dapper handles mapping.
            var rows = await SurfTimer.DB.QueryAsync<CheckpointEntity>(
                Queries.DB_QUERY_PB_GET_CPS,
                new { MapTimeID = runId }
            );

            // Key the dictionary by CP.
            var dict = rows.ToDictionary(cp => (int)cp.CP);

            _logger.LogInformation(
                "[{ClassName}] {MethodName} -> LoadCheckpointsAsync -> Found {Count} checkpoints.",
                nameof(MySqlDataAccessService),
                methodName,
                dict.Count
            );

            return dict;
        }

        public async Task<MapTimeRunDataEntity?> LoadPersonalBestRunAsync(
            int? pbId,
            int playerId,
            int mapId,
            int type,
            int style,
            [CallerMemberName] string methodName = ""
        )
        {
            // Choose SQL and parameters based on whether a specific PB id is provided.
            string sql;
            object args;

            if (!pbId.HasValue || pbId == -1)
            {
                sql = Queries.DB_QUERY_PB_GET_TYPE_RUNTIME;
                args = new
                {
                    PlayerId = playerId,
                    MapId = mapId,
                    Type = type,
                    Style = style,
                };
            }
            else
            {
                sql = Queries.DB_QUERY_PB_GET_SPECIFIC_MAPTIME_DATA;
                args = new { MapTimeId = pbId.Value };
            }

            // Fetch a single row (or null).
            var run = await SurfTimer.DB.QueryFirstOrDefaultAsync<MapTimeRunDataEntity>(sql, args);

            if (run is null)
            {
                _logger.LogInformation(
                    "[{ClassName}] {MethodName} -> LoadPersonalBestRunAsync -> No data found. PersonalBestID {PbID} | PlayerID {PlayerID} | MapID {MapID} | Type {Type} | Style {Style}",
                    nameof(MySqlDataAccessService),
                    methodName,
                    pbId,
                    playerId,
                    mapId,
                    type,
                    style
                );
                return null;
            }

            _logger.LogInformation(
                "[{ClassName}] {MethodName} -> LoadPersonalBestRunAsync -> Found data for PersonalBestID {PbID} | PlayerID {PlayerID} | MapID {MapID} | Type {Type} | Style {Style}",
                nameof(MySqlDataAccessService),
                methodName,
                pbId,
                playerId,
                mapId,
                type,
                style
            );

            return run;
        }

        /* Map.cs */
        public async Task<MapEntity?> GetMapInfoAsync(
            string mapName,
            [CallerMemberName] string methodName = ""
        )
        {
            var mapInfo = await SurfTimer.DB.QueryFirstOrDefaultAsync<MapEntity>(
                Queries.DB_QUERY_MAP_GET_INFO,
                new { mapName }
            );

            if (mapInfo is not null)
            {
                _logger.LogInformation(
                    "[{ClassName}] {MethodName} -> GetMapInfoAsync -> Found MapInfo data (ID: {ID})",
                    nameof(MySqlDataAccessService),
                    methodName,
                    mapInfo.ID
                );
            }

            return mapInfo;
        }

        public async Task<int> InsertMapInfoAsync(
            MapDto mapInfo,
            [CallerMemberName] string methodName = ""
        )
        {
            var newId = await SurfTimer.DB.InsertAsync(
                Queries.DB_QUERY_MAP_INSERT_INFO,
                new
                {
                    Name = mapInfo.Name,
                    Author = mapInfo.Author,
                    Tier = mapInfo.Tier,
                    Stages = mapInfo.Stages,
                    Bonuses = mapInfo.Bonuses,
                    Ranked = mapInfo.Ranked ? 1 : 0,
                    DateAdded = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LastPlayed = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                }
            );

            if (newId <= 0)
            {
                Exception ex = new(
                    $"Failed to insert new map '{mapInfo.Name}' into database. LAST_INSERT_ID() was 0."
                );

                _logger.LogError(
                    ex,
                    "[{ClassName}] {MethodName} -> InsertMapInfoAsync -> {ErrorMessage}",
                    nameof(MySqlDataAccessService),
                    methodName,
                    ex.Message
                );

                throw ex;
            }

            return (int)newId;
        }

        public async Task UpdateMapInfoAsync(
            MapDto mapInfo,
            int mapId,
            [CallerMemberName] string methodName = ""
        )
        {
            var rowsUpdated = await SurfTimer.DB.ExecuteAsync(
                Queries.DB_QUERY_MAP_UPDATE_INFO_FULL,
                new
                {
                    LastPlayed = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Stages = mapInfo.Stages,
                    Bonuses = mapInfo.Bonuses,
                    Author = mapInfo.Author,
                    Tier = mapInfo.Tier,
                    Ranked = mapInfo.Ranked ? 1 : 0, // TINYINT(1)
                    Id = mapId,
                }
            );

            if (rowsUpdated != 1)
            {
                Exception ex = new(
                    $"Failed to update map '{mapInfo.Name}' (ID {mapId}) in database. Rows updated: {rowsUpdated}"
                );

                _logger.LogError(
                    ex,
                    "[{ClassName}] {MethodName} -> UpdateMapInfoAsync -> {ErrorMessage}",
                    nameof(MySqlDataAccessService),
                    methodName,
                    ex.Message
                );

                throw ex;
            }
        }

        public async Task<List<MapTimeRunDataEntity>> GetMapRecordRunsAsync(
            int mapId,
            [CallerMemberName] string methodName = ""
        )
        {
            var runs = await SurfTimer.DB.QueryAsync<MapTimeRunDataEntity>(
                Queries.DB_QUERY_MAP_GET_RECORD_RUNS_AND_COUNT,
                new { Id = mapId }
            );

            return runs.ToList();
        }

        /* PlayerProfile.cs */
        public async Task<PlayerProfileEntity?> GetPlayerProfileAsync(
            ulong steamId,
            [CallerMemberName] string methodName = ""
        )
        {
            var playerData = await SurfTimer.DB.QueryFirstOrDefaultAsync<PlayerProfileEntity>(
                Queries.DB_QUERY_PP_GET_PROFILE,
                new { SteamID = steamId }
            );

            return playerData;
        }

        public async Task<int> InsertPlayerProfileAsync(
            PlayerProfileDto profile,
            [CallerMemberName] string methodName = ""
        )
        {
            int joinDate = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var lastId = await SurfTimer.DB.InsertAsync(
                Queries.DB_QUERY_PP_INSERT_PROFILE,
                new
                {
                    Name = profile.Name,
                    SteamID = profile.SteamID,
                    Country = profile.Country,
                    JoinDate = joinDate,
                    LastSeen = joinDate,
                    Connections = 1,
                }
            );

            if (lastId <= 0)
            {
                Exception ex = new(
                    $"Failed to insert new player '{profile.Name}' ({profile.SteamID}). LAST_INSERT_ID() was 0."
                );

                _logger.LogError(
                    ex,
                    "[{ClassName}] {MethodName} -> InsertPlayerProfileAsync -> {ErrorMessage}",
                    nameof(MySqlDataAccessService),
                    methodName,
                    ex.Message
                );

                throw ex;
            }

            return (int)lastId;
        }

        public async Task UpdatePlayerProfileAsync(
            PlayerProfileDto profile,
            int playerId,
            [CallerMemberName] string methodName = ""
        )
        {
            int lastSeen = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var rowsAffected = await SurfTimer.DB.ExecuteAsync(
                Queries.DB_QUERY_PP_UPDATE_PROFILE,
                new
                {
                    Country = profile.Country,
                    LastSeen = lastSeen,
                    Name = profile.Name,
                    Id = playerId,
                }
            );

            if (rowsAffected != 1)
            {
                Exception ex = new(
                    $"Failed to update player '{profile.Name}' ({profile.SteamID})."
                );

                _logger.LogError(
                    ex,
                    "[{ClassName}] {MethodName} -> UpdatePlayerProfileAsync -> {ErrorMessage}",
                    nameof(MySqlDataAccessService),
                    methodName,
                    ex.Message
                );

                throw ex;
            }
        }

        /* PlayerStats.cs */
        public async Task<List<MapTimeRunDataEntity>> GetPlayerMapTimesAsync(
            int playerId,
            int mapId,
            [CallerMemberName] string methodName = ""
        )
        {
            var mapTimes = await SurfTimer.DB.QueryAsync<MapTimeRunDataEntity>(
                Queries.DB_QUERY_PS_GET_ALL_RUNTIMES,
                new { PlayerId = playerId, MapId = mapId }
            );

            // Convert IEnumerable<T> to List<T>
            return mapTimes.ToList();
        }

        /* CurrentRun.cs */
        public async Task<int> InsertMapTimeAsync(
            MapTimeRunDataDto mapTime,
            [CallerMemberName] string methodName = ""
        )
        {
            // 1) Insert the run and get LAST_INSERT_ID()
            var mapTimeId = await SurfTimer.DB.InsertAsync(
                Queries.DB_QUERY_CR_INSERT_TIME,
                new
                {
                    PlayerId = mapTime.PlayerID,
                    MapId = mapTime.MapID,
                    Style = mapTime.Style,
                    Type = mapTime.Type,
                    Stage = mapTime.Stage,
                    RunTime = mapTime.RunTime,
                    StartVelX = mapTime.StartVelX,
                    StartVelY = mapTime.StartVelY,
                    StartVelZ = mapTime.StartVelZ,
                    EndVelX = mapTime.EndVelX,
                    EndVelY = mapTime.EndVelY,
                    EndVelZ = mapTime.EndVelZ,
                    RunDate = mapTime.RunDate,
                    ReplayFrames = mapTime.ReplayFrames, // assuming this matches your type handler
                }
            );

            if (mapTimeId <= 0)
            {
                Exception ex = new(
                    $"Failed to insert map time for PlayerId {mapTime.PlayerID}. LAST_INSERT_ID() was 0."
                );

                _logger.LogError(
                    ex,
                    "[{ClassName}] {MethodName} -> InsertMapTimeAsync -> {ErrorMessage}",
                    nameof(MySqlDataAccessService),
                    methodName,
                    ex.Message
                );

                throw ex;
            }
            // 2) Insert checkpoints in a single transaction (only for Type == 0)
            if (mapTime.Type == 0 && mapTime.Checkpoints is { Count: > 0 })
            {
                await SurfTimer.DB.TransactionAsync(
                    async (conn, tx) =>
                    {
                        // Insert each checkpoint using the same transaction
                        foreach (var cp in mapTime.Checkpoints.Values)
                        {
                            await conn.ExecuteAsync(
                                Queries.DB_QUERY_CR_INSERT_CP,
                                new
                                {
                                    MapTimeId = mapTimeId,
                                    CP = cp.CP,
                                    RunTime = cp.RunTime,
                                    StartVelX = cp.StartVelX,
                                    StartVelY = cp.StartVelY,
                                    StartVelZ = cp.StartVelZ,
                                    EndVelX = cp.EndVelX,
                                    EndVelY = cp.EndVelY,
                                    EndVelZ = cp.EndVelZ,
                                    Attempts = cp.Attempts,
                                    EndTouch = cp.EndTouch,
                                },
                                tx
                            );
                        }
                    }
                );
            }

            return (int)mapTimeId;
        }

        public async Task<int> UpdateMapTimeAsync(
            MapTimeRunDataDto mapTime,
            int mapTimeId,
            [CallerMemberName] string methodName = ""
        )
        {
            // 1) Update the run using it's ID
            var affectedRows = await SurfTimer.DB.ExecuteAsync(
                Queries.DB_QUERY_CR_UPDATE_TIME,
                new
                {
                    RunTime = mapTime.RunTime,
                    StartVelX = mapTime.StartVelX,
                    StartVelY = mapTime.StartVelY,
                    StartVelZ = mapTime.StartVelZ,
                    EndVelX = mapTime.EndVelX,
                    EndVelY = mapTime.EndVelY,
                    EndVelZ = mapTime.EndVelZ,
                    RunDate = mapTime.RunDate,
                    ReplayFrames = mapTime.ReplayFrames, // assuming this matches your type handler
                    MapTimeId = mapTimeId,
                }
            );

            if (affectedRows <= 0)
            {
                Exception ex = new(
                    $"Failed to update map time for MapTimeId {mapTimeId}. affectedRows was {affectedRows}."
                );

                _logger.LogError(
                    ex,
                    "[{ClassName}] {MethodName} -> UpdateMapTimeAsync -> {ErrorMessage}",
                    nameof(MySqlDataAccessService),
                    methodName,
                    ex.Message
                );

                throw ex;
            }

            _logger.LogInformation(
                "[{ClassName}] {MethodName} -> UpdateMapTimeAsync -> Updated MapTimeId {MapTimeId} with {AffectedRows} affected rows.",
                nameof(MySqlDataAccessService),
                methodName,
                mapTimeId,
                affectedRows
            );

            // 2) Insert checkpoints in a single transaction (only for Type == 0)
            if (mapTime.Type == 0 && mapTime.Checkpoints is { Count: > 0 })
            {
                await SurfTimer.DB.TransactionAsync(
                    async (conn, tx) =>
                    {
                        // Insert each checkpoint using the same transaction
                        foreach (var cp in mapTime.Checkpoints.Values)
                        {
                            await conn.ExecuteAsync(
                                Queries.DB_QUERY_CR_INSERT_CP,
                                new
                                {
                                    MapTimeId = mapTimeId,
                                    CP = cp.CP,
                                    RunTime = cp.RunTime,
                                    StartVelX = cp.StartVelX,
                                    StartVelY = cp.StartVelY,
                                    StartVelZ = cp.StartVelZ,
                                    EndVelX = cp.EndVelX,
                                    EndVelY = cp.EndVelY,
                                    EndVelZ = cp.EndVelZ,
                                    Attempts = cp.Attempts,
                                    EndTouch = cp.EndTouch,
                                },
                                tx
                            );
                        }
                    }
                );
            }

            return affectedRows;
        }
    }
}

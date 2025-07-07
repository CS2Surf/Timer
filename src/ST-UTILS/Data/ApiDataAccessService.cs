using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SurfTimer.Data
{
    public class ApiDataAccessService : IDataAccessService
    {
        private readonly ILogger<ApiDataAccessService> _logger;

        public ApiDataAccessService()
        {
            _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<ApiDataAccessService>>();
        }

        /* PersonalBest.cs */
        /// <summary>
        /// Loads the Checkpoint data for the given MapTime_ID. Used for loading player's personal bests and Map's world records.
        /// Bonus and Stage runs should NOT have any checkpoints.
        /// </summary>
        public async Task<Dictionary<int, Checkpoint>> LoadCheckpointsAsync(int runId, [CallerMemberName] string methodName = "")
        {
            _logger.LogInformation("[{ClassName}] {MethodName} -> LoadCheckpointsAsync -> Using API data access service.",
                nameof(ApiDataAccessService), methodName
            );

            var checkpoints = await ApiMethod
                .GET<API_Checkpoint[]>(
                    string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_RUN_CPS, runId)
                );
            if (checkpoints == null || checkpoints.Length == 0)
                return new Dictionary<int, Checkpoint>();

            _logger.LogInformation("[{ClassName}] {MethodName} -> LoadCheckpointsAsync -> Found {Count} checkpoints.",
                nameof(ApiDataAccessService), methodName, checkpoints.Length
            );

            return checkpoints
               .Select(cp =>
               {
                   var c = new Checkpoint(
                       cp.cp,
                       cp.run_time,
                       cp.start_vel_x,
                       cp.start_vel_y,
                       cp.start_vel_z,
                       cp.end_vel_x,
                       cp.end_vel_y,
                       cp.end_vel_z,
                       cp.end_touch,
                       cp.attempts
                   );
                   c.ID = cp.cp;
                   return c;
               })
               .ToDictionary(c => c.CP, c => c);
        }

        public async Task<PersonalBestDataModel?> LoadPersonalBestRunAsync(
            int? pbId, int playerId, int mapId, int type, int style, [CallerMemberName] string methodName = ""
        )
        {
            string url = pbId == null || pbId == -1
                ? string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_PB_BY_PLAYER,
                                playerId, mapId, type, style)
                : string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_PB_BY_ID,
                                pbId.Value);

            var apiResult = await ApiMethod.GET<API_PersonalBest>(url);
            if (apiResult == null)
                return null;

            _logger.LogInformation("[{ClassName}] {MethodName} -> LoadPersonalBestRunAsync -> Personal Best data",
                nameof(ApiDataAccessService), methodName
            );

            return new PersonalBestDataModel
            {
                ID = apiResult.id,
                Ticks = apiResult.run_time,
                Rank = apiResult.rank,
                StartVelX = apiResult.start_vel_x,
                StartVelY = apiResult.start_vel_y,
                StartVelZ = apiResult.start_vel_z,
                EndVelX = apiResult.end_vel_x,
                EndVelY = apiResult.end_vel_y,
                EndVelZ = apiResult.end_vel_z,
                RunDate = apiResult.run_date,
                ReplayFramesBase64 = apiResult.replay_frames
            };
        }


        /* Map.cs */
        public async Task<MapInfoDataModel?> GetMapInfoAsync(string mapName, [CallerMemberName] string methodName = "")
        {
            var mapInfo = await ApiMethod.GET<API_MapInfo>(
                string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_INFO, mapName));

            if (mapInfo != null)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> GetMapInfoAsync -> Found MapInfo data",
                    nameof(ApiDataAccessService), methodName
                );

                return new MapInfoDataModel
                {
                    ID = mapInfo.id,
                    Name = mapInfo.name,
                    Author = mapInfo.author,
                    Tier = mapInfo.tier,
                    Stages = mapInfo.stages,
                    Bonuses = mapInfo.bonuses,
                    Ranked = mapInfo.ranked == 1,
                    DateAdded = mapInfo.date_added ?? 0,
                    LastPlayed = mapInfo.last_played ?? 0
                };
            }

            return null;
        }

        public async Task<int> InsertMapInfoAsync(MapInfoDataModel mapInfo, [CallerMemberName] string methodName = "")
        {
            var apiMapInfo = new API_MapInfo
            {
                id = -1, // API-side will ignore or auto-increment
                name = mapInfo.Name,
                author = mapInfo.Author,
                tier = mapInfo.Tier,
                stages = mapInfo.Stages,
                bonuses = mapInfo.Bonuses,
                ranked = mapInfo.Ranked ? 1 : 0,
            };

            var postResponse = await ApiMethod.POST(Config.API.Endpoints.ENDPOINT_MAP_INSERT_INFO, apiMapInfo);

            if (postResponse == null || postResponse.last_id <= 0)
            {
                throw new Exception($"API failed to insert map '{mapInfo.Name}'.");
            }

            return postResponse.last_id;
        }

        public async Task UpdateMapInfoAsync(MapInfoDataModel mapInfo, [CallerMemberName] string methodName = "")
        {
            var apiMapInfo = new API_MapInfo
            {
                id = mapInfo.ID,
                name = mapInfo.Name,
                author = mapInfo.Author,
                tier = mapInfo.Tier,
                stages = mapInfo.Stages,
                bonuses = mapInfo.Bonuses,
                ranked = mapInfo.Ranked ? 1 : 0,
                // last_played = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var response = await ApiMethod.PUT(Config.API.Endpoints.ENDPOINT_MAP_UPDATE_INFO, apiMapInfo);
            if (response == null)
            {
                throw new Exception($"API failed to update map '{mapInfo.Name}' (ID {mapInfo.ID}).");
            }
        }

        /// <summary>
        /// Gets and loads all the record times for a given map ID
        /// </summary>
        /// <param name="mapId">ID of the map in DB</param>
        /// <returns></returns>
        public async Task<List<MapRecordRunDataModel>> GetMapRecordRunsAsync(int mapId, [CallerMemberName] string methodName = "")
        {
            // TODO: Re-do the API with the new query and fix the API assign of values
            var apiRuns = await ApiMethod.GET<API_MapTime[]>(
                string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_RUNS, mapId));

            var runs = new List<MapRecordRunDataModel>();

            if (apiRuns != null)
            {
                foreach (var time in apiRuns)
                {
                    runs.Add(new MapRecordRunDataModel
                    {
                        ID = time.id,
                        RunTime = time.run_time,
                        Type = time.type, // API currently returns only map times, needs rework
                        Stage = time.stage,
                        Style = time.style, // Fix this when updating API
                        Name = time.name,
                        StartVelX = (float)time.start_vel_x,
                        StartVelY = (float)time.start_vel_y,
                        StartVelZ = (float)time.start_vel_z,
                        EndVelX = (float)time.end_vel_x,
                        EndVelY = (float)time.end_vel_y,
                        EndVelZ = (float)time.end_vel_z,
                        RunDate = time.run_date,
                        TotalCount = time.total_count, // API should return total count, fix this as well
                        ReplayFramesBase64 = time.replay_frames // API should return this
                    });
                }
            }

            return runs;
        }


        /* PlayerProfile.cs */
        public async Task<PlayerProfileDataModel?> GetPlayerProfileAsync(ulong steamId, [CallerMemberName] string methodName = "")
        {
            // TODO: Implement API logic
            // throw new NotImplementedException();

            var player = await ApiMethod.GET<API_PlayerSurfProfile>(
                string.Format(Config.API.Endpoints.ENDPOINT_PP_GET_PROFILE, steamId));

            if (player != null)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> GetPlayerProfileAsync -> Found PlayerProfile data",
                    nameof(ApiDataAccessService), methodName
                );
                return new PlayerProfileDataModel
                {
                    ID = player.id,
                    // SteamID = steamId,
                    Name = player.name,
                    Country = player.country,
                    JoinDate = player.join_date,
                    LastSeen = player.last_seen,
                    Connections = player.connections
                };
            }

            _logger.LogWarning("[{ClassName}] {MethodName} -> GetPlayerProfileAsync -> No PlayerProfile data found for {SteamID}",
                nameof(ApiDataAccessService), methodName, steamId
            );
            return null;
        }

        public async Task<int> InsertPlayerProfileAsync(PlayerProfileDataModel profile, [CallerMemberName] string methodName = "")
        {
            // TODO: Implement API logic
            // throw new NotImplementedException();
            int joinDate = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var apiPlayerProfileInfo = new API_PlayerSurfProfile
            {
                steam_id = profile.SteamID,
                name = profile.Name,
                country = profile.Country,
                join_date = joinDate,
                last_seen = joinDate,
                connections = 1
            };

            var postResponse = await ApiMethod.POST(Config.API.Endpoints.ENDPOINT_PP_INSERT_PROFILE, apiPlayerProfileInfo);

            if (postResponse == null || postResponse.last_id <= 0)
            {
                throw new Exception($"API failed to insert Player Profile for '{profile.Name}'.");
            }

            return postResponse.last_id;
        }

        public async Task UpdatePlayerProfileAsync(PlayerProfileDataModel profile, [CallerMemberName] string methodName = "")
        {
            // TODO: Implement API logic
            // throw new NotImplementedException();
            int lastSeen = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var apiPlayerProfileInfo = new API_PlayerSurfProfile
            {
                id = profile.ID,
                steam_id = profile.SteamID,
                name = profile.Name,
                country = profile.Country,
                join_date = profile.JoinDate,
                last_seen = lastSeen,
                connections = 1
            };

            var response = await ApiMethod.PUT(Config.API.Endpoints.ENDPOINT_PP_UPDATE_PROFILE, apiPlayerProfileInfo);
            if (response == null)
            {
                throw new Exception($"API failed to update Player Profile for '{apiPlayerProfileInfo.name}' (ID {apiPlayerProfileInfo.id}).");
            }
        }


        /* PlayerStats.cs */
        public async Task<List<PlayerMapTimeDataModel>> GetPlayerMapTimesAsync(int playerId, int mapId, [CallerMemberName] string methodName = "")
        {
            // TODO: Implement API logic
            var mapTimes = new List<PlayerMapTimeDataModel>();

            var apiResponse = await ApiMethod.GET<API_PersonalBest[]>(
                string.Format(Config.API.Endpoints.ENDPOINT_PS_GET_PLAYER_MAP_DATA, playerId, mapId)
            );

            if (apiResponse != null)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> GetPlayerMapTimesAsync -> Found maptime data for PlayerID {PlayerID} and MapID {MapID}",
                    nameof(ApiDataAccessService), methodName, playerId, mapId
                );

                foreach (var time in apiResponse)
                {
                    mapTimes.Add(new PlayerMapTimeDataModel
                    {
                        ID = time.id,
                        RunTime = time.run_time,
                        Type = time.type,
                        Stage = time.stage,
                        Style = time.style,
                        Rank = time.rank,
                        StartVelX = (float)time.start_vel_x,
                        StartVelY = (float)time.start_vel_y,
                        StartVelZ = (float)time.start_vel_z,
                        EndVelX = (float)time.end_vel_x,
                        EndVelY = (float)time.end_vel_y,
                        EndVelZ = (float)time.end_vel_z,
                        RunDate = time.run_date,
                        ReplayFramesBase64 = time.replay_frames
                    });
                }
            }

            return mapTimes;
        }



        /* CurrentRun.cs */
        public async Task<int> InsertMapTimeAsync(MapTimeDataModel mapTime, [CallerMemberName] string methodName = "")
        {
            // Convert the Checkpoint object to the API_Checkpoint one
            var runCheckpoints = mapTime.Checkpoints.Select(cp => new API_Checkpoint
            {
                cp = cp.Key,
                run_time = cp.Value.Ticks,
                end_touch = cp.Value.EndTouch,
                start_vel_x = cp.Value.StartVelX,
                start_vel_y = cp.Value.StartVelY,
                start_vel_z = cp.Value.StartVelZ,
                end_vel_x = cp.Value.EndVelX,
                end_vel_y = cp.Value.EndVelY,
                end_vel_z = cp.Value.EndVelZ,
                attempts = cp.Value.Attempts
            }).ToList();

            var apiSaveMapTime = new API_SaveMapTime
            {
                player_id = mapTime.PlayerId,
                map_id = mapTime.MapId,
                run_time = mapTime.Ticks,
                start_vel_x = mapTime.StartVelX,
                start_vel_y = mapTime.StartVelY,
                start_vel_z = mapTime.StartVelZ,
                end_vel_x = mapTime.EndVelX,
                end_vel_y = mapTime.EndVelY,
                end_vel_z = mapTime.EndVelZ,
                style = mapTime.Style,
                type = mapTime.Type,
                stage = mapTime.Stage,
                replay_frames = mapTime.ReplayFramesBase64,
                run_date = mapTime.RunDate,
                checkpoints = runCheckpoints
            };

            /*
            _logger.LogDebug(
                "[{ClassName}] {MethodName} -> Converted and sending API_SaveMapTime:\n" +
                " player_id: {PlayerId}\n" +
                " map_id: {MapId}\n" +
                " run_time: {RunTime}\n" +
                " style: {Style}\n" +
                " type: {Type}\n" +
                " stage: {Stage}\n" +
                " start_vel: ({StartVelX}, {StartVelY}, {StartVelZ})\n" +
                " end_vel: ({EndVelX}, {EndVelY}, {EndVelZ})\n" +
                " replay_frames: {ReplayFramesLength}\n" +
                " checkpoints: {CheckpointsCount}\n" +
                " run_date: {RunDate}",
                nameof(CurrentRun), methodName,
                apiSaveMapTime.player_id,
                apiSaveMapTime.map_id,
                apiSaveMapTime.run_time,
                apiSaveMapTime.style,
                apiSaveMapTime.type,
                apiSaveMapTime.stage,
                apiSaveMapTime.start_vel_x, apiSaveMapTime.start_vel_y, apiSaveMapTime.start_vel_z,
                apiSaveMapTime.end_vel_x, apiSaveMapTime.end_vel_y, apiSaveMapTime.end_vel_z,
                apiSaveMapTime.replay_frames?.Length ?? 0,
                apiSaveMapTime.checkpoints?.Count ?? 0,
                apiSaveMapTime.run_date ?? 0
            );
            */

            var postResponse = await ApiMethod.POST(
                Config.API.Endpoints.ENDPOINT_CR_SAVE_MAP_TIME,
                apiSaveMapTime
            );

            if (postResponse == null || postResponse.last_id <= 0)
            {
                throw new Exception($"API failed to insert MapTime for Player ID '{mapTime.PlayerId}' on Map ID '{mapTime.MapId}'.");
            }

            return postResponse.last_id;
        }

        // public async Task SaveRunCheckpointsAsync(int mapTimeId, IEnumerable<Checkpoint> checkpoints, [CallerMemberName] string methodName = "")
        // {
        //     // TODO: Implement API logic
        //     // throw new NotImplementedException();

        // }

    }
}

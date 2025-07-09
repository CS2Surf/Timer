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

        public async Task<PersonalBestDataModel?> LoadPersonalBestRunAsync(int? pbId, int playerId, int mapId, int type, int style, [CallerMemberName] string methodName = "")
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

            return new PersonalBestDataModel(apiResult);
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

                return new MapInfoDataModel(mapInfo);
            }

            return null;
        }

        public async Task<int> InsertMapInfoAsync(MapInfoDataModel mapInfo, [CallerMemberName] string methodName = "")
        {
            var apiMapInfo = new API_MapInfo(mapInfo);

            var postResponse = await ApiMethod.POST(Config.API.Endpoints.ENDPOINT_MAP_INSERT_INFO, apiMapInfo);

            if (postResponse == null || postResponse.last_id <= 0)
            {
                throw new Exception($"API failed to insert map '{mapInfo.Name}'.");
            }

            return postResponse.last_id;
        }

        public async Task UpdateMapInfoAsync(MapInfoDataModel mapInfo, [CallerMemberName] string methodName = "")
        {
            var apiMapInfo = new API_MapInfo(mapInfo);

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
            var apiRuns = await ApiMethod.GET<API_MapTime[]>(
                string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_RUNS, mapId));

            var runs = new List<MapRecordRunDataModel>();

            if (apiRuns != null)
            {
                foreach (var time in apiRuns)
                {
                    runs.Add(new MapRecordRunDataModel(time));
                }
            }

            return runs;
        }


        /* PlayerProfile.cs */
        public async Task<PlayerProfileDataModel?> GetPlayerProfileAsync(ulong steamId, [CallerMemberName] string methodName = "")
        {
            var player = await ApiMethod.GET<API_PlayerSurfProfile>(
                string.Format(Config.API.Endpoints.ENDPOINT_PP_GET_PROFILE, steamId));

            if (player != null)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> GetPlayerProfileAsync -> Found PlayerProfile data",
                    nameof(ApiDataAccessService), methodName
                );
                return new PlayerProfileDataModel(player);
            }

            _logger.LogWarning("[{ClassName}] {MethodName} -> GetPlayerProfileAsync -> No PlayerProfile data found for {SteamID}",
                nameof(ApiDataAccessService), methodName, steamId
            );
            return null;
        }

        public async Task<int> InsertPlayerProfileAsync(PlayerProfileDataModel profile, [CallerMemberName] string methodName = "")
        {
            var apiPlayerProfileInfo = new API_PlayerSurfProfile(profile);

            var postResponse = await ApiMethod.POST(Config.API.Endpoints.ENDPOINT_PP_INSERT_PROFILE, apiPlayerProfileInfo);

            if (postResponse == null || postResponse.last_id <= 0)
            {
                throw new Exception($"API failed to insert Player Profile for '{profile.Name}'.");
            }

            return postResponse.last_id;
        }

        public async Task UpdatePlayerProfileAsync(PlayerProfileDataModel profile, [CallerMemberName] string methodName = "")
        {
            var apiPlayerProfileInfo = new API_PlayerSurfProfile(profile);

            var response = await ApiMethod.PUT(Config.API.Endpoints.ENDPOINT_PP_UPDATE_PROFILE, apiPlayerProfileInfo);
            if (response == null)
            {
                throw new Exception($"API failed to update Player Profile for '{apiPlayerProfileInfo.name}' (ID {apiPlayerProfileInfo.id}).");
            }
        }


        /* PlayerStats.cs */
        public async Task<List<PlayerMapTimeDataModel>> GetPlayerMapTimesAsync(int playerId, int mapId, [CallerMemberName] string methodName = "")
        {
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
                    mapTimes.Add(new PlayerMapTimeDataModel(time));
                }
            }

            return mapTimes;
        }



        /* CurrentRun.cs */
        public async Task<int> InsertMapTimeAsync(MapTimeDataModel mapTime, [CallerMemberName] string methodName = "")
        {
            // Initialize the API structure for POST request
            var apiSaveMapTime = new API_SaveMapTime(mapTime);

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

            _logger.LogDebug(
                "[{ClassName}] {MethodName} -> Successfully inserted entry with id {ID} with type {Type}",
                nameof(CurrentRun), methodName, postResponse.last_id, mapTime.Type
            );

            return postResponse.last_id;
        }

        // public async Task SaveRunCheckpointsAsync(int mapTimeId, IEnumerable<Checkpoint> checkpoints, [CallerMemberName] string methodName = "")
        // {
        //     // TODO: Implement API logic
        //     // throw new NotImplementedException();

        // }

    }
}

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Shared.DTO;
using SurfTimer.Shared.Entities;

namespace SurfTimer.Data
{
    public class ApiDataAccessService : IDataAccessService
    {
        private readonly ILogger<ApiDataAccessService> _logger;

        /// <summary>
        /// Add/load data using API calls.
        /// </summary>
        public ApiDataAccessService()
        {
            _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<ApiDataAccessService>>();
        }

        public async Task<bool> PingAccessService([CallerMemberName] string methodName = "")
        {
            try
            {
                var response = await ApiMethod.GET<Dictionary<string, double>>(
                    string.Format(
                        Config.Api.Endpoints.ENDPOINT_PING,
                        (double)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    )
                );

                if (response != null && response.ContainsKey("clientUnix"))
                {
                    _logger.LogInformation(
                        "[{ClassName}] {MethodName} -> Success -> Client: {ClientUnix} | Server: {ServerUnix} | Latency: {LatencyS}s | Latency: {LatencyMS}ms",
                        nameof(ApiDataAccessService),
                        methodName,
                        response["clientUnix"],
                        response["serverUnix"],
                        response["latencySeconds"],
                        response["latencyMs"]
                    );
                    return true;
                }

                _logger.LogWarning(
                    "[{ClassName}] {MethodName} -> Unexpected response structure.",
                    nameof(ApiDataAccessService),
                    methodName
                );
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[{ClassName}] {MethodName} -> Failed to reach API.",
                    nameof(ApiDataAccessService),
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
            var checkpoints = await ApiMethod.GET<Dictionary<int, CheckpointEntity>>(
                string.Format(Config.Api.Endpoints.ENDPOINT_MAP_GET_RUN_CPS, runId)
            );
            if (checkpoints == null || checkpoints.Count == 0)
                return new Dictionary<int, CheckpointEntity>();

            _logger.LogInformation(
                "[{ClassName}] {MethodName} -> LoadCheckpointsAsync -> Found {Count} checkpoints for MapTimeId {MapTimeId}.",
                nameof(ApiDataAccessService),
                methodName,
                checkpoints.Count,
                runId
            );

            return checkpoints;
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
            string url =
                pbId == null || pbId == -1
                    ? string.Format(
                        Config.Api.Endpoints.ENDPOINT_MAP_GET_PB_BY_PLAYER,
                        playerId,
                        mapId,
                        type,
                        style
                    )
                    : string.Format(Config.Api.Endpoints.ENDPOINT_MAP_GET_PB_BY_ID, pbId.Value);

            var apiResult = await ApiMethod.GET<MapTimeRunDataEntity>(url);
            if (apiResult == null)
                return null;

            _logger.LogInformation(
                "[{ClassName}] {MethodName} -> LoadPersonalBestRunAsync -> Personal Best data",
                nameof(ApiDataAccessService),
                methodName
            );

            return apiResult;
        }

        /* Map.cs */
        public async Task<MapEntity?> GetMapInfoAsync(
            string mapName,
            [CallerMemberName] string methodName = ""
        )
        {
            var mapInfo = await ApiMethod.GET<MapEntity>(
                string.Format(Config.Api.Endpoints.ENDPOINT_MAP_GET_INFO, mapName)
            );

            if (mapInfo != null)
            {
                _logger.LogInformation(
                    "[{ClassName}] {MethodName} -> GetMapInfoAsync -> Found MapInfo data. MapID {MapId}",
                    nameof(ApiDataAccessService),
                    methodName,
                    mapInfo.ID
                );

                return mapInfo;
            }

            return null;
        }

        public async Task<int> InsertMapInfoAsync(
            MapDto mapInfo,
            [CallerMemberName] string methodName = ""
        )
        {
            var postResponse = await ApiMethod.POST(
                Config.Api.Endpoints.ENDPOINT_MAP_INSERT_INFO,
                mapInfo
            );

            if (postResponse == null || postResponse.Id <= 0)
            {
                Exception ex = new($"API failed to insert map '{mapInfo.Name}'.");
                throw ex;
            }

            return postResponse.Id;
        }

        public async Task UpdateMapInfoAsync(
            MapDto mapInfo,
            int mapId,
            [CallerMemberName] string methodName = ""
        )
        {
            var response = await ApiMethod.PUT(
                string.Format(Config.Api.Endpoints.ENDPOINT_MAP_UPDATE_INFO, mapId),
                mapInfo
            );
            if (response == null)
            {
                Exception ex = new($"API failed to update map '{mapInfo.Name}' (ID {mapId}).");
                throw ex;
            }
        }

        public async Task<List<MapTimeRunDataEntity>> GetMapRecordRunsAsync(
            int mapId,
            [CallerMemberName] string methodName = ""
        )
        {
            var apiRuns = await ApiMethod.GET<List<MapTimeRunDataEntity>>(
                string.Format(string.Format(Config.Api.Endpoints.ENDPOINT_MAP_GET_RUNS, mapId))
            );

            return apiRuns!;
        }

        /* PlayerProfile.cs */
        public async Task<PlayerProfileEntity?> GetPlayerProfileAsync(
            ulong steamId,
            [CallerMemberName] string methodName = ""
        )
        {
            var player = await ApiMethod.GET<PlayerProfileEntity>(
                string.Format(Config.Api.Endpoints.ENDPOINT_PP_GET_PROFILE, steamId)
            );

            if (player != null)
            {
                _logger.LogInformation(
                    "[{ClassName}] {MethodName} -> GetPlayerProfileAsync -> Found PlayerProfile data for ProfileID = {ProfileID}",
                    nameof(ApiDataAccessService),
                    methodName,
                    player.ID
                );

                return player;
            }

            _logger.LogWarning(
                "[{ClassName}] {MethodName} -> GetPlayerProfileAsync -> No PlayerProfile data found for {SteamID}",
                nameof(ApiDataAccessService),
                methodName,
                steamId
            );
            return null;
        }

        public async Task<int> InsertPlayerProfileAsync(
            PlayerProfileDto profile,
            [CallerMemberName] string methodName = ""
        )
        {
            var postResponse = await ApiMethod.POST(
                Config.Api.Endpoints.ENDPOINT_PP_INSERT_PROFILE,
                profile
            );

            if (postResponse == null || postResponse.Id <= 0)
            {
                Exception ex = new($"API failed to insert Player Profile for '{profile.Name}'.");
                throw ex;
            }

            return postResponse.Id;
        }

        public async Task UpdatePlayerProfileAsync(
            PlayerProfileDto profile,
            int playerId,
            [CallerMemberName] string methodName = ""
        )
        {
            var response = await ApiMethod.PUT(
                string.Format(Config.Api.Endpoints.ENDPOINT_PP_UPDATE_PROFILE, playerId),
                profile
            );

            if (response == null)
            {
                Exception ex = new(
                    $"API failed to update Player Profile for '{profile.Name}' (ID {playerId})."
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
            var apiResponse = await ApiMethod.GET<List<MapTimeRunDataEntity>>(
                string.Format(Config.Api.Endpoints.ENDPOINT_PS_GET_PLAYER_MAP_DATA, playerId, mapId)
            );

            if (apiResponse == null)
            {
                Exception ex = new(
                    $"API failed to GET MapTime entries for PlayerID '{playerId}' and MapID '{mapId}'."
                );
                throw ex;
            }

            _logger.LogInformation(
                "[{ClassName}] {MethodName} -> GetPlayerMapTimesAsync -> Found maptime data for PlayerID {PlayerID} and MapID {MapID}",
                nameof(ApiDataAccessService),
                methodName,
                playerId,
                mapId
            );

            return apiResponse;
        }

        /* CurrentRun.cs */
        public async Task<int> InsertMapTimeAsync(
            MapTimeRunDataDto mapTime,
            [CallerMemberName] string methodName = ""
        )
        {
            var postResponse = await ApiMethod.POST(
                Config.Api.Endpoints.ENDPOINT_CR_SAVE_MAP_TIME,
                mapTime
            );

            if (postResponse == null || postResponse.Inserted <= 0)
            {
                Exception ex = new(
                    $"API failed to insert MapTime for Player ID '{mapTime.PlayerID}' on Map ID '{mapTime.MapID}'."
                );
                throw ex;
            }

            _logger.LogDebug(
                "[{ClassName}] {MethodName} -> Successfully inserted entry with id {ID} with type {Type}",
                nameof(ApiDataAccessService),
                methodName,
                postResponse.Id,
                mapTime.Type
            );

            return postResponse.Id;
        }

        public async Task<int> UpdateMapTimeAsync(
            MapTimeRunDataDto mapTime,
            int mapTimeId,
            [CallerMemberName] string methodName = ""
        )
        {
            var postResponse = await ApiMethod.PUT(
                string.Format(Config.Api.Endpoints.ENDPOINT_CR_UPDATE_MAP_TIME, mapTimeId),
                mapTime
            );

            if (postResponse == null || postResponse.Inserted <= 0)
            {
                Exception ex = new(
                    $"API failed to update MapTime {mapTimeId} for Player ID '{mapTime.PlayerID}' on Map ID '{mapTime.MapID}'."
                );
                throw ex;
            }

            _logger.LogDebug(
                "[{ClassName}] {MethodName} -> Successfully updated MapTime entry {ID} with type {Type}",
                nameof(ApiDataAccessService),
                methodName,
                mapTimeId,
                mapTime.Type
            );

            return postResponse.Id;
        }
    }
}

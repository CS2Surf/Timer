using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Modules.Entities;

namespace SurfTimer.Data
{
    public interface IDataAccessService
    {
        /* PersonalBest.cs */
        /// <summary>
        /// Load all checkpoints for a given run ID (map time).
        /// Returns an empty dictionary if none found.
        /// </summary>
        Task<Dictionary<int, Checkpoint>> LoadCheckpointsAsync(
            int runId,
            [CallerMemberName] string methodName = ""
        );

        /// <summary>
        /// Load a personal-best run for a given player.
        /// - If pbId is null or -1, load by playerId/mapId/type/style.
        /// - If pbId has a value, load that specific run.
        /// Returns null if not found.
        /// </summary>
        Task<PersonalBestDataModel?> LoadPersonalBestRunAsync(
            int? pbId,
            int playerId,
            int mapId,
            int type,
            int style,
            [CallerMemberName] string methodName = ""
        );


        /* Map.cs */
        Task<MapInfoDataModel?> GetMapInfoAsync(
            string mapName, [CallerMemberName] string methodName = ""
        );
        Task<int> InsertMapInfoAsync(
            MapInfoDataModel mapInfo, [CallerMemberName] string methodName = ""
        );
        Task UpdateMapInfoAsync(
            MapInfoDataModel mapInfo, [CallerMemberName] string methodName = ""
        );
        Task<List<MapRecordRunDataModel>> GetMapRecordRunsAsync(
            int mapId, [CallerMemberName] string methodName = ""
        );


        /* PlayerProfile.cs */
        Task<PlayerProfileDataModel?> GetPlayerProfileAsync(
            ulong steamId, [CallerMemberName] string methodName = ""
        );
        Task<int> InsertPlayerProfileAsync(
            PlayerProfileDataModel profile, [CallerMemberName] string methodName = ""
        );
        Task UpdatePlayerProfileAsync(
            PlayerProfileDataModel profile, [CallerMemberName] string methodName = ""
        );


        /* PlayerStats.cs */
        Task<List<PlayerMapTimeDataModel>> GetPlayerMapTimesAsync(
            int playerId, int mapId, [CallerMemberName] string methodName = ""
        );


        /* CurrentRun.cs */
        Task<int> InsertMapTimeAsync(
        // Task InsertMapTimeAsync(
            MapTimeDataModel mapTime, [CallerMemberName] string methodName = ""
        );
        /* Merged with InsertMapTimeAsync */
        // Task SaveRunCheckpointsAsync(
        //     int mapTimeId, IEnumerable<Checkpoint> checkpoints, [CallerMemberName] string methodName = ""
        // );

    }
}

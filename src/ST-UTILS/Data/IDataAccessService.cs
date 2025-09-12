using System.Runtime.CompilerServices;
using SurfTimer.Shared.DTO;
using SurfTimer.Shared.Entities;

namespace SurfTimer.Data
{
    /// <summary>
    /// Contains all methods for data retrieval or insertion by all access services (API, MySQL)
    /// </summary>
    public interface IDataAccessService
    {
        /// <summary>
        /// Ping the Data Access Service.
        /// </summary>
        /// <returns>True for successful connection, False otherwise</returns>
        Task<bool> PingAccessService([CallerMemberName] string methodName = "");

        /* PersonalBest.cs */
        /// <summary>
        /// Retrieve Checkpoints table entries for a given run ID (map time).
        /// Bonus and Stage runs should NOT have any checkpoints.
        /// </summary>
        /// <param name="runId">ID of the run from DB</param>
        /// <returns>Dictionary[int, CheckpointEntity] data or NULL if none found</returns>
        Task<Dictionary<int, CheckpointEntity>> LoadCheckpointsAsync(
            int runId,
            [CallerMemberName] string methodName = ""
        );

        /// <summary>
        /// Load a personal-best run for a given player from MapTime table through API or MySQL.
        /// If pbId is null or -1, load by playerId/mapId/type/style.
        /// If pbId has a value, load that specific run.
        /// </summary>
        /// <param name="pbId">[Optional] ID of the run from DB. If present other arguments will be ignored</param>
        /// <param name="playerId">ID of the player from DB. If pbId is null or -1</param>
        /// <param name="mapId">ID of the map from DB. If pbId is null or -1</param>
        /// <param name="type">Run Type (0 = Map ; 1 = Bonus ; 2 = Stage). If pbId is null or -1</param>
        /// <param name="style">If pbId is null or -1</param>
        /// <returns>MapTimeRunDataEntity data or null if not found</returns>
        Task<MapTimeRunDataEntity?> LoadPersonalBestRunAsync(
            int? pbId,
            int playerId,
            int mapId,
            int type,
            int style,
            [CallerMemberName] string methodName = ""
        );

        /* Map.cs */
        /// <summary>
        /// Retrieves Map table entry for map through API or MySQL.
        /// </summary>
        /// <param name="mapName">Name of map</param>
        /// <returns>MapEntity data</returns>
        Task<MapEntity?> GetMapInfoAsync(string mapName, [CallerMemberName] string methodName = "");

        /// <summary>
        /// Adds Map table entry for map through API or MySQL.
        /// </summary>
        /// <param name="mapInfo">Data to add in table</param>
        /// <returns>int mapId</returns>
        Task<int> InsertMapInfoAsync(MapDto mapInfo, [CallerMemberName] string methodName = "");

        /// <summary>
        /// Updates Map table entry for map through API or MySQL.
        /// </summary>
        /// <param name="mapInfo">Data to update in table</param>
        Task UpdateMapInfoAsync(
            MapDto mapInfo,
            int mapId,
            [CallerMemberName] string methodName = ""
        );

        /// <summary>
        /// Retrieves MapTime table record runs for given mapId through API or MySQL.
        /// </summary>
        /// <param name="mapId">ID from DB</param>
        /// <returns>List[MapTimeRunDataEntity] data</returns>
        Task<List<MapTimeRunDataEntity>> GetMapRecordRunsAsync(
            int mapId,
            [CallerMemberName] string methodName = ""
        );

        /* PlayerProfile.cs */
        /// <summary>
        /// Retrieve Player table entry for the player through API or MySQL.
        /// </summary>
        /// <param name="steamId">SteamID for the player</param>
        /// <returns>PlayerProfileEntity data</returns>
        Task<PlayerProfileEntity?> GetPlayerProfileAsync(
            ulong steamId,
            [CallerMemberName] string methodName = ""
        );

        /// <summary>
        /// Adds Player table entry for the player through API or MySQL.
        /// </summary>
        /// <param name="profile">Data to add in table</param>
        /// <returns>int playerId given by DB</returns>
        Task<int> InsertPlayerProfileAsync(
            PlayerProfileDto profile,
            [CallerMemberName] string methodName = ""
        );

        /// <summary>
        /// Updates Player table entry for the player through API or MySQL.
        /// </summary>
        /// <param name="profile">Data to update in table</param>
        Task UpdatePlayerProfileAsync(
            PlayerProfileDto profile,
            int playerId,
            [CallerMemberName] string methodName = ""
        );

        /* PlayerStats.cs */
        /// <summary>
        /// Retrieves ALL MapTime table entries for playerId and mapId combo through API or MySQL.
        /// </summary>
        /// <param name="playerId">ID from DB</param>
        /// <param name="mapId">ID from DB</param>
        /// <returns>List[MapTimeRunDataEntity] data</returns>
        Task<List<MapTimeRunDataEntity>> GetPlayerMapTimesAsync(
            int playerId,
            int mapId,
            [CallerMemberName] string methodName = ""
        );

        /* CurrentRun.cs */
        /// <summary>
        /// Adds a MapTime table entry through API or MySQL. Deals with checkpoints for map runs of type 0
        /// </summary>
        /// <param name="mapTime">Data to insert/update in table</param>
        /// <returns>int mapTimeId given by DB</returns>
        Task<int> InsertMapTimeAsync(
            MapTimeRunDataDto mapTime,
            [CallerMemberName] string methodName = ""
        );

        /// <summary>
        /// Updates a MapTime table entry through API or MySQL. Deals with checkpoints for map runs of type 0
        /// </summary>
        /// <param name="mapTime">Data to update in table</param>
        /// <returns>int mapTimeId that was updated</returns>
        Task<int> UpdateMapTimeAsync(
            MapTimeRunDataDto mapTime,
            int mapTimeId,
            [CallerMemberName] string methodName = ""
        );
    }
}

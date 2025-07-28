using System.Runtime.CompilerServices;

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
        /// <returns>Dictionary[int, Checkpoint] data or NULL if none found</returns>
        Task<Dictionary<int, Checkpoint>> LoadCheckpointsAsync(
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
        /// <returns>PersonalBestDataModel data or null if not found</returns>
        Task<PersonalBestDataModel?> LoadPersonalBestRunAsync(
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
        /// <returns>MapInfoDataModel data</returns>
        Task<MapInfoDataModel?> GetMapInfoAsync(
            string mapName, [CallerMemberName] string methodName = ""
        );
        /// <summary>
        /// Adds Map table entry for map through API or MySQL.
        /// </summary>
        /// <param name="mapInfo">Data to add in table</param>
        /// <returns>int mapId</returns>
        Task<int> InsertMapInfoAsync(
            MapInfoDataModel mapInfo, [CallerMemberName] string methodName = ""
        );
        /// <summary>
        /// Updates Map table entry for map through API or MySQL.
        /// </summary>
        /// <param name="mapInfo">Data to update in table</param>
        Task UpdateMapInfoAsync(
            MapInfoDataModel mapInfo, [CallerMemberName] string methodName = ""
        );
        /// <summary>
        /// Retrieves MapTime table record runs for given mapId through API or MySQL.
        /// </summary>
        /// <param name="mapId">ID from DB</param>
        /// <returns>List[MapRecordRunDataModel] data</returns>
        Task<List<MapRecordRunDataModel>> GetMapRecordRunsAsync(
            int mapId, [CallerMemberName] string methodName = ""
        );


        /* PlayerProfile.cs */
        /// <summary>
        /// Retrieve Player table entry for the player through API or MySQL.
        /// </summary>
        /// <param name="steamId">SteamID for the player</param>
        /// <returns>PlayerProfileDataModel data</returns>
        Task<PlayerProfileDataModel?> GetPlayerProfileAsync(
            ulong steamId, [CallerMemberName] string methodName = ""
        );
        /// <summary>
        /// Adds Player table entry for the player through API or MySQL.
        /// </summary>
        /// <param name="profile">Data to add in table</param>
        /// <returns>int playerId given by DB</returns>
        Task<int> InsertPlayerProfileAsync(
            PlayerProfileDataModel profile, [CallerMemberName] string methodName = ""
        );
        /// <summary>
        /// Updates Player table entry for the player through API or MySQL.
        /// </summary>
        /// <param name="profile">Data to update in table</param>
        Task UpdatePlayerProfileAsync(
            PlayerProfileDataModel profile, [CallerMemberName] string methodName = ""
        );


        /* PlayerStats.cs */
        /// <summary>
        /// Retrieves ALL MapTime table entries for playerId and mapId combo through API or MySQL.
        /// </summary>
        /// <param name="playerId">ID from DB</param>
        /// <param name="mapId">ID from DB</param>
        /// <returns>List[PlayerMapTimeDataModel] data</returns>
        Task<List<PlayerMapTimeDataModel>> GetPlayerMapTimesAsync(
            int playerId, int mapId, [CallerMemberName] string methodName = ""
        );


        /* CurrentRun.cs */
        /// <summary>
        /// Adds/updates a MapTime table entry through API or MySQL. Deals with checkpoints for map runs of type 0
        /// </summary>
        /// <param name="mapTime">Data to insert/update in table</param>
        /// <returns>int mapTimeId given by DB</returns>
        Task<int> InsertMapTimeAsync(
            MapTimeDataModel mapTime, [CallerMemberName] string methodName = ""
        );
    }
}

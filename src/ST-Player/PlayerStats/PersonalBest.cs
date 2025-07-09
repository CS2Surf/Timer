using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Data;

namespace SurfTimer;

/// <summary>
/// As the PersonalBest object is being used for each different style, we shouldn't need a separate `Style` variable in here because each style entry will have unique ID in the Database
/// and will therefore be a unique PersonalBest entry.
/// </summary>
internal class PersonalBest : RunStats
{
    public int ID { get; set; } = -1; // Exclude from constructor, retrieve from Database when loading/saving
    public int Rank { get; set; } = -1; // Exclude from constructor, retrieve from Database when loading/saving
    public int Type { get; set; } = -1; // Identifies bonus # - 0 for map time -> huh, why o_O?
    public string Name { get; set; } = ""; // This is used only for WRs
    private readonly ILogger<PersonalBest> _logger;
    private readonly IDataAccessService _dataService;
    // Add other properties as needed

    // Constructor
    public PersonalBest() : base()
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<PersonalBest>>();
        _dataService = SurfTimer.ServiceProvider.GetRequiredService<IDataAccessService>();
    }

    /// <summary>
    /// Loads the Checkpoint data for the given MapTime_ID. Used for loading player's personal bests and Map's world records.
    /// Bonus and Stage runs should NOT have any checkpoints.
    /// </summary>
    public async Task LoadCheckpoints([CallerMemberName] string methodName = "")
    {
        // 1) ask the data service for your checkpoints
        var cps = await _dataService.LoadCheckpointsAsync(this.ID);

        // 2) if none, just return
        if (cps == null || cps.Count == 0)
        {
            _logger.LogInformation(
                "[{Class}] {Method} -> No checkpoints found for run {RunId}.",
                nameof(PersonalBest), methodName, this.ID
            );
            return;
        }

        // 3) otherwise assign
        this.Checkpoints = cps;

        // 4) log how many you got
        _logger.LogInformation(
            "[{ClassName}] {MethodName} -> Loaded {Count} checkpoints for run {RunId}.",
            nameof(PersonalBest), methodName, cps.Count, this.ID
        );
    }


    /// <summary>
    /// Loads specific type/style MapTime data for the player (run without checkpoints) from the database for their personal best runs.
    /// Should be used to reload data from a specific `PersonalBest` object
    /// </summary>
    /// <param name="player">Player object</param>
    public async Task LoadPlayerSpecificMapTimeData(Player player, [CallerMemberName] string methodName = "")
    {
        // 1) call the data service, passing only the primitives:
        var model = await _dataService.LoadPersonalBestRunAsync(
            pbId: this.ID == -1 ? (int?)null : this.ID,
            playerId: player.Profile.ID,
            mapId: SurfTimer.CurrentMap.ID,
            type: this.Type,
            style: player.Timer.Style
        );

        // 2) if nothing found, log & return
        if (model == null)
        {
            _logger.LogTrace(
                "[{ClassName}] {MethodName} -> No personal best found for player {Player} (ID={Id} ; Type={Type}).",
                nameof(PersonalBest), methodName,
                player.Profile.Name, player.Profile.ID, this.Type
            );
            return;
        }

        // 3) map back into your instance
        this.ID = model.ID;
        this.Ticks = model.Ticks;
        this.Rank = model.Rank;
        this.StartVelX = model.StartVelX;
        this.StartVelY = model.StartVelY;
        this.StartVelZ = model.StartVelZ;
        this.EndVelX = model.EndVelX;
        this.EndVelY = model.EndVelY;
        this.EndVelZ = model.EndVelZ;
        this.RunDate = model.RunDate;
        this.ReplayFramesBase64 = model.ReplayFramesBase64; // Won't work with MySQL load? - Not tested

        _logger.LogDebug(
            "[{ClassName}] {MethodName} -> Loaded PB run {RunId} for {Player}.",
            nameof(PersonalBest), methodName,
            this.ID, player.Profile.Name
        );
    }

    /* Delete after testing with API?
        public async Task PB_LoadPlayerSpecificMapTimeData(Player player, [CallerMemberName] string methodName = "")
        {
            // Console.WriteLine($"CS2 Surf ERROR >> internal class PersonalBest -> public async Task PB_LoadPlayerSpecificMapTimeData -> QUERY:\n{string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_RUNTIME, player.Profile.ID, player.CurrMap.ID, 0, player.Timer.Style)}");
            // using (var results = await SurfTimer.DB.QueryAsync(string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_RUNTIME, player.Profile.ID, player.CurrMap.ID, 0, player.Timer.Style)))
            if (this == null)
            {
    #if DEBUG
                _logger.LogDebug("[{ClassName}] {MethodName} -> PB_LoadPlayerSpecificMapTimeData -> PersonalBest object is null.",
                    nameof(PersonalBest), methodName
                );
    #endif

                return;
            }

            MySqlConnector.MySqlDataReader? results = null;

            // Console.WriteLine(string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_TYPE_RUNTIME, player.Profile.ID, SurfTimer.CurrentMap.ID, this.Type, player.Timer.Style));

            if (this.ID == -1)
                results = await SurfTimer.DB.QueryAsync(string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_TYPE_RUNTIME, player.Profile.ID, SurfTimer.CurrentMap.ID, this.Type, player.Timer.Style));
            else
                results = await SurfTimer.DB.QueryAsync(string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_SPECIFIC_MAPTIME_DATA, this.ID));

    #if DEBUG
            Console.WriteLine($"----> public async Task PB_LoadPlayerSpecificMapTimeData -> this.ID {this.ID} ");
            Console.WriteLine($"----> public async Task PB_LoadPlayerSpecificMapTimeData -> this.Ticks {this.Ticks} ");
            Console.WriteLine($"----> public async Task PB_LoadPlayerSpecificMapTimeData -> this.RunDate {this.RunDate} ");
    #endif

            if (results == null || !results.HasRows)
            {
                // #if DEBUG
                _logger.LogTrace("[{ClassName}] {MethodName} -> PB_LoadPlayerSpecificMapTimeData -> No MapTime data found for '{playerName}' ({playerID}). (Results Null? {IsNull})",
                    nameof(PersonalBest), methodName, player.Profile.Name, player.Profile.ID, results == null
                );
                // #endif

                return;
            }

            while (results.Read())
            {
    #if DEBUG
                _logger.LogDebug("[{ClassName}] {MethodName} -> PB_LoadPlayerSpecificMapTimeData -> Loading MapTime Run: RunID {RunID} | RunTicks {RunTicks} | StartVelX {StartVelX} | StartVelY {StartVelY}.",
                    nameof(PersonalBest), methodName, results.GetInt32("id"), results.GetInt32("run_time"), results.GetFloat("start_vel_x"), results.GetFloat("start_vel_y")
                );
    #endif

                this.ID = results.GetInt32("id");
                this.Ticks = results.GetInt32("run_time");
                this.Rank = results.GetInt32("rank");
                this.StartVelX = (float)results.GetDouble("start_vel_x");
                this.StartVelY = (float)results.GetDouble("start_vel_y");
                this.StartVelZ = (float)results.GetDouble("start_vel_z");
                this.EndVelX = (float)results.GetDouble("end_vel_x");
                this.EndVelY = (float)results.GetDouble("end_vel_y");
                this.EndVelZ = (float)results.GetDouble("end_vel_z");
                this.RunDate = results.GetInt32("run_date");
            }

            // #if DEBUG
            _logger.LogDebug("[{ClassName}] {MethodName} -> PB_LoadPlayerSpecificMapTimeData -> MapTime ID {ID} (Type: {Type}) loaded for '{PlayerName}' with time {RunTime}",
                nameof(PersonalBest), methodName, this.ID, this.Type, player.Profile.Name, PlayerHUD.FormatTime(this.Ticks)
            );
            // #endif
        }
    */

}
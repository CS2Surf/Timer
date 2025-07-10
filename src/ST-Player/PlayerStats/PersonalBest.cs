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
        var cps = await _dataService.LoadCheckpointsAsync(this.ID);

        // If nothing found, log and return
        if (cps == null || cps.Count == 0)
        {
            _logger.LogInformation(
                "[{Class}] {Method} -> No checkpoints found for run {RunId}.",
                nameof(PersonalBest), methodName, this.ID
            );
            return;
        }

        this.Checkpoints = cps;

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
        var model = await _dataService.LoadPersonalBestRunAsync(
            pbId: this.ID == -1 ? (int?)null : this.ID,
            playerId: player.Profile.ID,
            mapId: SurfTimer.CurrentMap.ID,
            type: this.Type,
            style: player.Timer.Style
        );

        // If nothing found, log and return
        if (model == null)
        {
            _logger.LogTrace(
                "[{ClassName}] {MethodName} -> No personal best found for player {Player} (ID={Id} ; Type={Type}).",
                nameof(PersonalBest), methodName,
                player.Profile.Name, player.Profile.ID, this.Type
            );
            return;
        }

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
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Data;
using System.Runtime.CompilerServices;

namespace SurfTimer;

public class PlayerStats
{
    /// <summary>
    /// Map Personal Best - Refer to as PB[style]
    /// </summary>
    public Dictionary<int, PersonalBest> PB { get; set; } = new Dictionary<int, PersonalBest>();
    /// <summary>
    /// Bonus Personal Best - Refer to as BonusPB[bonus#][style]
    /// </summary>
    public Dictionary<int, PersonalBest>[] BonusPB { get; set; }
    /// <summary>
    /// Stage Personal Best - Refer to as StagePB[stage#][style]
    /// </summary>
    public Dictionary<int, PersonalBest>[] StagePB { get; set; }
    /// <summary>
    /// This object tracks data for the Player's current run.
    /// </summary>
    public CurrentRun ThisRun { get; set; } = new CurrentRun();

    private readonly ILogger<PlayerStats> _logger;
    private readonly IDataAccessService _dataService;


    internal PlayerStats([CallerMemberName] string methodName = "")
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<PlayerStats>>();
        _dataService = SurfTimer.ServiceProvider.GetRequiredService<IDataAccessService>();

        // Initialize PB variables
        this.StagePB = new Dictionary<int, PersonalBest>[SurfTimer.CurrentMap.Stages + 1];
        this.BonusPB = new Dictionary<int, PersonalBest>[SurfTimer.CurrentMap.Bonuses + 1];
        int initStage = 0;
        int initBonus = 0;

        foreach (int style in Config.Styles)
        {
            PB[style] = new PersonalBest { Type = 0 };

            for (int i = 1; i <= SurfTimer.CurrentMap.Stages; i++)
            {
                this.StagePB[i] = new Dictionary<int, PersonalBest>();
                this.StagePB[i][style] = new PersonalBest { Type = 2 };
                initStage++;
            }

            for (int i = 1; i <= SurfTimer.CurrentMap.Bonuses; i++)
            {
                this.BonusPB[i] = new Dictionary<int, PersonalBest>();
                this.BonusPB[i][style] = new PersonalBest { Type = 1 };
                initBonus++;
            }
        }


        _logger.LogTrace("[{ClassName}] {MethodName} -> PlayerStats -> Initialized {StagesInitialized} Stages and {BonusesInitialized} Bonuses",
            nameof(PlayerStats), methodName, initStage, initBonus
        );
    }

    /// <summary>
    /// Loads the player's map time data from the database along with their ranks. For all types and styles (may not work correctly for Stages/Bonuses)
    /// `Checkpoints` are loaded separately from another method in the `PresonalBest` class as it uses the unique `ID` for the run. (This method calls it if needed)
    /// This populates all the `style` and `type` stats the player has for the map
    /// </summary>
    internal async Task LoadPlayerMapTimesData(Player player, int playerId = 0, int mapId = 0, [CallerMemberName] string methodName = "")
    {
        var playerMapTimes = await _dataService.GetPlayerMapTimesAsync(player.Profile.ID, SurfTimer.CurrentMap.ID);

        if (!playerMapTimes.Any())
        {
            _logger.LogTrace("[{ClassName}] {MethodName} -> No MapTimes data found for Player {PlayerName} (ID {PlayerID}).",
                nameof(PlayerStats), methodName, player.Profile.Name, player.Profile.ID);
            return;
        }

        foreach (var mapTime in playerMapTimes)
        {
            int style = mapTime.Style;
            switch (mapTime.Type)
            {
                case 1: // Bonus time
#if DEBUG
                    _logger.LogDebug("[{ClassName}] {MethodName} -> LoadPlayerMapTimesData >> BonusPB with ID {ID}", nameof(PlayerStats), methodName, mapTime.ID);
#endif
                    BonusPB[mapTime.Stage][style].ID = mapTime.ID;
                    BonusPB[mapTime.Stage][style].RunTime = mapTime.RunTime;
                    BonusPB[mapTime.Stage][style].Type = mapTime.Type;
                    BonusPB[mapTime.Stage][style].Rank = mapTime.Rank;
                    BonusPB[mapTime.Stage][style].StartVelX = mapTime.StartVelX;
                    BonusPB[mapTime.Stage][style].StartVelY = mapTime.StartVelY;
                    BonusPB[mapTime.Stage][style].StartVelZ = mapTime.StartVelZ;
                    BonusPB[mapTime.Stage][style].EndVelX = mapTime.EndVelX;
                    BonusPB[mapTime.Stage][style].EndVelY = mapTime.EndVelY;
                    BonusPB[mapTime.Stage][style].EndVelZ = mapTime.EndVelZ;
                    BonusPB[mapTime.Stage][style].RunDate = mapTime.RunDate;
                    break;

                case 2: // Stage time
#if DEBUG
                    _logger.LogDebug("[{ClassName}] {MethodName} -> LoadPlayerMapTimesData >> StagePB with ID {ID}", nameof(PlayerStats), methodName, mapTime.ID);
#endif
                    StagePB[mapTime.Stage][style].ID = mapTime.ID;
                    StagePB[mapTime.Stage][style].RunTime = mapTime.RunTime;
                    StagePB[mapTime.Stage][style].Type = mapTime.Type;
                    StagePB[mapTime.Stage][style].Rank = mapTime.Rank;
                    StagePB[mapTime.Stage][style].StartVelX = mapTime.StartVelX;
                    StagePB[mapTime.Stage][style].StartVelY = mapTime.StartVelY;
                    StagePB[mapTime.Stage][style].StartVelZ = mapTime.StartVelZ;
                    StagePB[mapTime.Stage][style].EndVelX = mapTime.EndVelX;
                    StagePB[mapTime.Stage][style].EndVelY = mapTime.EndVelY;
                    StagePB[mapTime.Stage][style].EndVelZ = mapTime.EndVelZ;
                    StagePB[mapTime.Stage][style].RunDate = mapTime.RunDate;
                    break;

                default: // Map time
#if DEBUG
                    _logger.LogDebug("[{ClassName}] {MethodName} -> LoadPlayerMapTimesData >> MapPB with ID {ID}", nameof(PlayerStats), methodName, mapTime.ID);
#endif
                    PB[style].ID = mapTime.ID;
                    PB[style].RunTime = mapTime.RunTime;
                    PB[style].Type = mapTime.Type;
                    PB[style].Rank = mapTime.Rank;
                    PB[style].StartVelX = mapTime.StartVelX;
                    PB[style].StartVelY = mapTime.StartVelY;
                    PB[style].StartVelZ = mapTime.StartVelZ;
                    PB[style].EndVelX = mapTime.EndVelX;
                    PB[style].EndVelY = mapTime.EndVelY;
                    PB[style].EndVelZ = mapTime.EndVelZ;
                    PB[style].RunDate = mapTime.RunDate;

                    await PB[style].LoadCheckpoints();
                    break;
            }

#if DEBUG
            _logger.LogDebug("[{ClassName}] {MethodName} -> Loaded PB[{Style}] run {RunID} (Rank {Rank}) for '{PlayerName}' (ID {PlayerID}).",
                nameof(PlayerStats), methodName, style, mapTime.ID, mapTime.Rank, player.Profile.Name, player.Profile.ID);
#endif
        }
    }
}
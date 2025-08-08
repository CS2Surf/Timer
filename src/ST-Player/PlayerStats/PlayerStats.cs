using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Data;

namespace SurfTimer;

public class PlayerStats
{
    // To-Do: Each stat should be a class of its own, with its own methods and properties - easier to work with. 
    //        Temporarily, we store ticks + basic info so we can experiment
    // These account for future style support and a relevant index.

    /// <summary>
    /// Map Personal Best - Refer to as PB[style]
    /// </summary>
    public Dictionary<int, PersonalBest> PB { get; set; } = new Dictionary<int, PersonalBest>();
    /// <summary>
    /// Bonus Personal Best - Refer to as BonusPB[bonus#][style]
    /// Need to figure out a way to NOT hardcode to `32` but to total amount of bonuses
    /// </summary>
    public Dictionary<int, PersonalBest>[] BonusPB { get; set; } = new Dictionary<int, PersonalBest>[32];
    /// <summary>
    /// Stage Personal Best - Refer to as StagePB[stage#][style]
    /// Need to figure out a way to NOT hardcode to `32` but to total amount of stages
    /// </summary>
    public Dictionary<int, PersonalBest>[] StagePB { get; set; } = new Dictionary<int, PersonalBest>[32];
    /// <summary>
    /// This object tracks data for the Player's current run.
    /// </summary>
    public CurrentRun ThisRun { get; set; } = new CurrentRun();
    private readonly ILogger<PlayerStats> _logger;
    private readonly IDataAccessService _dataService;



    // Initialize PersonalBest for each `style` (e.g., 0 for normal)
    // Here we can loop through all available styles at some point and initialize them
    internal PlayerStats([CallerMemberName] string methodName = "")
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<PlayerStats>>();
        _dataService = SurfTimer.ServiceProvider.GetRequiredService<IDataAccessService>();

        // Initialize MapPB for each style
        foreach (int style in Config.Styles)
        {
            PB[style] = new PersonalBest();
            PB[style].Type = 0;
        }

        int initialized = 0;
        for (int i = 0; i < 32; i++)
        {
            this.BonusPB[i] = new Dictionary<int, PersonalBest>();
            this.BonusPB[i][0] = new PersonalBest();
            this.BonusPB[i][0].Type = 1;

            this.StagePB[i] = new Dictionary<int, PersonalBest>();
            this.StagePB[i][0] = new PersonalBest();
            this.StagePB[i][0].Type = 2;
            initialized++;
        }
        _logger.LogTrace("[{ClassName}] {MethodName} -> PlayerStats -> Initialized {initialized} Stages and Bonuses",
            nameof(PlayerStats), methodName, initialized
        );
    }

    /// <summary>
    /// Not used 
    /// </summary>
    // API - Can be replaced with `ENDPOINT_MAP_GET_PB_BY_PLAYER`
    internal async void LoadMapTime(Player player, int style = 0, [CallerMemberName] string methodName = "")
    {
        var player_maptime = await ApiMethod.GET<API_MapTime>($"/surftimer/playerspecificdata?player_id={player.Profile.ID}&map_id={SurfTimer.CurrentMap.ID}&style={style}&type=0");
        if (player_maptime == null)
        {
            _logger.LogTrace("[{ClassName}] {MethodName} -> LoadMapTime -> No MapTime data found for Player {PlayerName} (ID {PlayerID}).",
                nameof(PlayerStats), methodName, player.Profile.Name, player.Profile.ID
            );
            return;
        }

        PB[style].ID = player_maptime.id;
        PB[style].RunTime = player_maptime.run_time;
        PB[style].Type = player_maptime.type;
        PB[style].StartVelX = player_maptime.start_vel_x;
        PB[style].StartVelY = player_maptime.start_vel_y;
        PB[style].StartVelZ = player_maptime.start_vel_z;
        PB[style].EndVelX = player_maptime.end_vel_x;
        PB[style].EndVelY = player_maptime.end_vel_y;
        PB[style].EndVelZ = player_maptime.end_vel_z;
        // PB[style].RunDate = player_maptime.run_date ?? 0;
        PB[style].RunDate = player_maptime.run_date;

        if (player_maptime.checkpoints == null)
        {
            _logger.LogTrace("[{ClassName}] {MethodName} -> LoadMapTime -> No Checkpoints data found for Player {PlayerName} (ID {PlayerID}).",
                nameof(PlayerStats), methodName, player.Profile.Name, player.Profile.ID
            );
            return;
        }

        foreach (var cp in player_maptime.checkpoints)
        {
            PB[style].Checkpoints[cp.cp] = new Checkpoint(cp.cp, cp.run_time, cp.start_vel_x, cp.start_vel_y, cp.start_vel_z, cp.end_vel_x, cp.end_vel_y, cp.end_vel_z, cp.end_touch, cp.attempts);
        }
    }

    /// <summary>
    /// Not used 
    /// </summary>
    // API - Can be replaced with `ENDPOINT_MAP_GET_PB_BY_PLAYER`
    internal async void LoadStageTime(Player player, int style = 0, [CallerMemberName] string methodName = "")
    {
        var player_maptime = await ApiMethod.GET<API_MapTime[]>($"/surftimer/playerspecificdata?player_id={player.Profile.ID}&map_id={SurfTimer.CurrentMap.ID}&style={style}&type=2");
        if (player_maptime == null)
        {
            _logger.LogTrace("[{ClassName}] {MethodName} -> LoadStageTime -> No MapTime data found for Player {PlayerName} (ID {PlayerID}).",
                nameof(PlayerStats), methodName, player.Profile.Name, player.Profile.ID
            );
            return;
        }

        foreach (API_MapTime mt in player_maptime)
        {
            StagePB[mt.stage][style].ID = mt.id;
            StagePB[mt.stage][style].RunTime = mt.run_time;
            StagePB[mt.stage][style].Type = mt.type;
            StagePB[mt.stage][style].StartVelX = mt.start_vel_x;
            StagePB[mt.stage][style].StartVelY = mt.start_vel_y;
            StagePB[mt.stage][style].StartVelZ = mt.start_vel_z;
            StagePB[mt.stage][style].EndVelX = mt.end_vel_x;
            StagePB[mt.stage][style].EndVelY = mt.end_vel_y;
            StagePB[mt.stage][style].EndVelZ = mt.end_vel_z;
            // StagePB[mt.stage][style].RunDate = mt.run_date ?? 0;
            StagePB[mt.stage][style].RunDate = mt.run_date;
        }
    }

    /// <summary>
    /// Not used 
    /// </summary>
    // API - Can be replaced with `ENDPOINT_MAP_GET_PB_BY_PLAYER`
    internal async void LoadBonusTime(Player player, int style = 0, [CallerMemberName] string methodName = "")
    {
        var player_maptime = await ApiMethod.GET<API_MapTime[]>($"/surftimer/playerspecificdata?player_id={player.Profile.ID}&map_id={SurfTimer.CurrentMap.ID}&style={style}&type=1");
        if (player_maptime == null)
        {
            _logger.LogTrace("[{ClassName}] {MethodName} -> LoadBonusTime -> No MapTime data found for Player {PlayerName} (ID {PlayerID}).",
                nameof(PlayerStats), methodName, player.Profile.Name, player.Profile.ID
            );
            return;
        }

        foreach (API_MapTime mt in player_maptime)
        {
            BonusPB[mt.stage][style].ID = mt.id;
            BonusPB[mt.stage][style].RunTime = mt.run_time;
            BonusPB[mt.stage][style].Type = mt.type;
            BonusPB[mt.stage][style].StartVelX = mt.start_vel_x;
            BonusPB[mt.stage][style].StartVelY = mt.start_vel_y;
            BonusPB[mt.stage][style].StartVelZ = mt.start_vel_z;
            BonusPB[mt.stage][style].EndVelX = mt.end_vel_x;
            BonusPB[mt.stage][style].EndVelY = mt.end_vel_y;
            BonusPB[mt.stage][style].EndVelZ = mt.end_vel_z;
            // BonusPB[mt.stage][style].RunDate = mt.run_date ?? 0;
            BonusPB[mt.stage][style].RunDate = mt.run_date;
        }
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
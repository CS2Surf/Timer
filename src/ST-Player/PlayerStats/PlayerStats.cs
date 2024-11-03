using MySqlConnector;

namespace SurfTimer;

internal class PlayerStats
{
    // To-Do: Each stat should be a class of its own, with its own methods and properties - easier to work with. 
    //        Temporarily, we store ticks + basic info so we can experiment
    // These account for future style support and a relevant index.

    // /// <summary>
    // /// Stage Personal Best - Refer to as StagePB[style][stage#]
    // /// To-do: DEPRECATE THIS WHEN IMPLEMENTING STAGES, FOLLOW NEW PB STRUCTURE
    // /// </summary>
    // public int[,] StagePB { get; set; } = { { 0, 0 } };
    // /// <summary>
    // /// Stage Personal Best - Refer to as StageRank[style][stage#]
    // /// To-do: DEPRECATE THIS WHEN IMPLEMENTING STAGES, FOLLOW NEW PB STRUCTURE
    // /// </summary>
    // public int[,] StageRank { get; set; } = { { 0, 0 } }; 

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

    // Initialize PersonalBest for each `style` (e.g., 0 for normal)
    // Here we can loop through all available styles at some point and initialize them
    public PlayerStats()
    {
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
            this.BonusPB[i][0] = new PersonalBest(); // To-do: Implement styles
            this.BonusPB[i][0].Type = 1;

            this.StagePB[i] = new Dictionary<int, PersonalBest>();
            this.StagePB[i][0] = new PersonalBest(); // To-do: Implement styles
            this.StagePB[i][0].Type = 2;
            initialized++;
        }
        Console.WriteLine($"====== INITIALIZED {initialized} STAGES AND BONUSES FOR PLAYERSTATS");
    }

    // API
    public async void LoadMapTime(Player player, int style = 0)
    {
        var player_maptime = await ApiMethod.GET<API_MapTime>($"/surftimer/playerspecificdata?player_id={player.Profile.ID}&map_id={player.CurrMap.ID}&style={style}&type=0");
        if (player_maptime == null)
        {
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> LoadMapTime -> No MapTime data found for Player.");
            return;
        }

        PB[style].ID = player_maptime.id;
        PB[style].Ticks = player_maptime.run_time;
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
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> LoadMapTime -> No Checkpoints data found for Player.");
            return;
        }

        foreach (var cp in player_maptime.checkpoints)
        {
            PB[style].Checkpoints[cp.cp] = new Checkpoint(cp.cp, cp.run_time, cp.start_vel_x, cp.start_vel_y, cp.start_vel_z, cp.end_vel_x, cp.end_vel_y, cp.end_vel_z, cp.end_touch, cp.attempts);
        }
    }

    // API
    public async void LoadStageTime(Player player, int style = 0)
    {
        var player_maptime = await ApiMethod.GET<API_MapTime[]>($"/surftimer/playerspecificdata?player_id={player.Profile.ID}&map_id={player.CurrMap.ID}&style={style}&type=2");
        if (player_maptime == null)
        {
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> LoadStageTime -> No MapTime data found for Player.");
            return;
        }

        foreach (API_MapTime mt in player_maptime)
        {
            StagePB[mt.stage][style].ID = mt.id;
            StagePB[mt.stage][style].Ticks = mt.run_time;
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

    // API
    public async void LoadBonusTime(Player player, int style = 0)
    {
        var player_maptime = await ApiMethod.GET<API_MapTime[]>($"/surftimer/playerspecificdata?player_id={player.Profile.ID}&map_id={player.CurrMap.ID}&style={style}&type=1");
        if (player_maptime == null)
        {
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> LoadBonusTime -> No MapTime data found for Player.");
            return;
        }

        foreach (API_MapTime mt in player_maptime)
        {
            BonusPB[mt.stage][style].ID = mt.id;
            BonusPB[mt.stage][style].Ticks = mt.run_time;
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
    /// `Checkpoints` are loaded separately from another method in the `PresonalBest` class as it uses the unique `ID` for the run.
    /// This populates all the `style` and `type` stats the player has for the map
    /// </summary>
    public async Task LoadPlayerMapTimesData(Player player, int playerId = 0, int mapId = 0)
    {
        using (var playerStats = await SurfTimer.DB.QueryAsync(
            string.Format(Config.MySQL.Queries.DB_QUERY_PS_GET_ALL_RUNTIMES, player.Profile.ID, SurfTimer.CurrentMap.ID)))
        {
            // int style = player.Timer.Style;
            int style;
            if (!playerStats.HasRows)
            {
                Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> public async Task LoadPlayerMapTimesData -> No MapTimes data found for Player ({player.Profile.ID}).");
                return;
            }
            while (playerStats.Read())
            {
                // Load data into each PersonalBest object
                if (playerStats.GetInt32("type") == 1) // Bonus time
                {
#if DEBUG
                    System.Console.WriteLine("DEBUG >> (func) LoadPlayerMapTimesData >> BonusPB");
#endif
                    int bonusNum = playerStats.GetInt32("stage");
                    style = playerStats.GetInt32("style"); // To-do: Uncomment when style is implemented
                    BonusPB[bonusNum][style].ID = playerStats.GetInt32("id");
                    BonusPB[bonusNum][style].Ticks = playerStats.GetInt32("run_time");
                    BonusPB[bonusNum][style].Type = playerStats.GetInt32("type");
                    BonusPB[bonusNum][style].Rank = playerStats.GetInt32("rank");
                    BonusPB[bonusNum][style].StartVelX = (float)playerStats.GetDouble("start_vel_x");
                    BonusPB[bonusNum][style].StartVelY = (float)playerStats.GetDouble("start_vel_y");
                    BonusPB[bonusNum][style].StartVelZ = (float)playerStats.GetDouble("start_vel_z");
                    BonusPB[bonusNum][style].EndVelX = (float)playerStats.GetDouble("end_vel_x");
                    BonusPB[bonusNum][style].EndVelY = (float)playerStats.GetDouble("end_vel_y");
                    BonusPB[bonusNum][style].EndVelZ = (float)playerStats.GetDouble("end_vel_z");
                    BonusPB[bonusNum][style].RunDate = playerStats.GetInt32("run_date");
                }
                else if (playerStats.GetInt32("type") == 2) // Stage time
                {
#if DEBUG
                    System.Console.WriteLine("DEBUG >> (func) LoadPlayerMapTimesData >> StagePB");
#endif
                    int stageNum = playerStats.GetInt32("stage");
                    style = playerStats.GetInt32("style"); // To-do: Uncomment when style is implemented
                    StagePB[stageNum][style].ID = playerStats.GetInt32("id");
                    StagePB[stageNum][style].Ticks = playerStats.GetInt32("run_time");
                    StagePB[stageNum][style].Type = playerStats.GetInt32("type");
                    StagePB[stageNum][style].Rank = playerStats.GetInt32("rank");
                    StagePB[stageNum][style].StartVelX = (float)playerStats.GetDouble("start_vel_x");
                    StagePB[stageNum][style].StartVelY = (float)playerStats.GetDouble("start_vel_y");
                    StagePB[stageNum][style].StartVelZ = (float)playerStats.GetDouble("start_vel_z");
                    StagePB[stageNum][style].EndVelX = (float)playerStats.GetDouble("end_vel_x");
                    StagePB[stageNum][style].EndVelY = (float)playerStats.GetDouble("end_vel_y");
                    StagePB[stageNum][style].EndVelZ = (float)playerStats.GetDouble("end_vel_z");
                    StagePB[stageNum][style].RunDate = playerStats.GetInt32("run_date");
                    Console.WriteLine(@$"DEBUG >> (func) LoadPlayerMapTimesData >> StagePB Loaded:
                    StagePB[{stageNum}][{style}] =
                    Stage: {stageNum} | ID: {StagePB[stageNum][style].ID} | Ticks: {StagePB[stageNum][style].Ticks} | Rank: {StagePB[stageNum][style].Rank} | Type: {StagePB[stageNum][style].Type}");
                }
                else // Map time
                {
#if DEBUG
                    System.Console.WriteLine("DEBUG >> (func) LoadPlayerMapTimesData >> MapPB");
#endif
                    style = playerStats.GetInt32("style"); // To-do: Uncomment when style is implemented
                    PB[style].ID = playerStats.GetInt32("id");
                    PB[style].Ticks = playerStats.GetInt32("run_time");
                    PB[style].Type = playerStats.GetInt32("type");
                    PB[style].Rank = playerStats.GetInt32("rank");
                    PB[style].StartVelX = (float)playerStats.GetDouble("start_vel_x");
                    PB[style].StartVelY = (float)playerStats.GetDouble("start_vel_y");
                    PB[style].StartVelZ = (float)playerStats.GetDouble("start_vel_z");
                    PB[style].EndVelX = (float)playerStats.GetDouble("end_vel_x");
                    PB[style].EndVelY = (float)playerStats.GetDouble("end_vel_y");
                    PB[style].EndVelZ = (float)playerStats.GetDouble("end_vel_z");
                    PB[style].RunDate = playerStats.GetInt32("run_date");
                    Console.WriteLine(@$"DEBUG >> (func) LoadPlayerMapTimesData >> PB Loaded:
                    PB[{style}] =
                    ID: {PB[style].ID} | Ticks: {PB[style].Ticks} | Rank: {PB[style].Rank} | Type: {PB[style].Type}");
                    await this.PB[style].PB_LoadCheckpointsData();
                }
                // Console.WriteLine($"============== CS2 Surf DEBUG >> internal class PlayerStats -> public async Task LoadPlayerMapTimesData -> PlayerID: {player.Profile.ID} | Rank: {PB[style].Rank} | ID: {PB[style].ID} | RunTime: {PB[style].Ticks} | SVX: {PB[style].StartVelX} | SVY: {PB[style].StartVelY} | SVZ: {PB[style].StartVelZ} | EVX: {PB[style].EndVelX} | EVY: {PB[style].EndVelY} | EVZ: {PB[style].EndVelZ} | Run Date (UNIX): {PB[style].RunDate}");
#if DEBUG
                Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> public async Task LoadPlayerMapTimesData -> PlayerStats.PB (ID {PB[style].ID}) loaded from DB.");
#endif
            }
        }

        // // This would have to go inside the `Map Time` else statement in order to load checkpoints for each `style`
        // if (PB[player.Timer.Style].ID != -1)
        // {
        //     // await LoadCheckpointsData(player);
        //     await this.PB[player.Timer.Style].PB_LoadCheckpointsData();
        // }
    }

}
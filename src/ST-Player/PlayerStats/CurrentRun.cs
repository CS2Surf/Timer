namespace SurfTimer;

/// <summary>
/// This class stores data for the current run.
/// </summary>
internal class CurrentRun
{
    public Dictionary<int, Checkpoint> Checkpoints { get; set; } // Current RUN checkpoints tracker
    public int Ticks { get; set; } // To-do: will be the last (any) zone end touch time
    public float StartVelX { get; set; } // This will store MAP START VELOCITY X
    public float StartVelY { get; set; } // This will store MAP START VELOCITY Y
    public float StartVelZ { get; set; } // This will store MAP START VELOCITY Z
    public float EndVelX { get; set; } // This will store MAP END VELOCITY X
    public float EndVelY { get; set; } // This will store MAP END VELOCITY Y
    public float EndVelZ { get; set; } // This will store MAP END VELOCITY Z
    public int RunDate { get; set; }
    // Add other properties as needed

    // Constructor
    public CurrentRun()
    {
        Checkpoints = new Dictionary<int, Checkpoint>();
        Ticks = 0;
        StartVelX = 0.0f;
        StartVelY = 0.0f;
        StartVelZ = 0.0f;
        EndVelX = 0.0f;
        EndVelY = 0.0f;
        EndVelZ = 0.0f;
        RunDate = 0;
    }

    public void Reset()
    {
        Checkpoints.Clear();
        Ticks = 0;
        StartVelX = 0.0f;
        StartVelY = 0.0f;
        StartVelZ = 0.0f;
        EndVelX = 0.0f;
        EndVelY = 0.0f;
        EndVelZ = 0.0f;
        RunDate = 0;
        // Reset other properties as needed
    }

    /// <summary>
    /// Saves the player's run to the database. 
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="bonus">Bonus number</param>
    /// <param name="stage">Stage number</param>
    /// <param name="run_ticks">Ticks for the run - used for Stage and Bonus entries</param>
    public async Task SaveMapTime(Player player, int bonus = 0, int stage = 0, int run_ticks = -1)
    {
        // Add entry in DB for the run
        // PrintSituations(player);
        string replay_frames = player.ReplayRecorder.TrimReplay(player, stage != 0 ? 2 : bonus != 0 ? 1 : 0, stage == SurfTimer.CurrentMap.Stages);

        Console.WriteLine($"CS2 Surf DEBUG >> internal class CurrentRun -> public async Task SaveMapTime -> Sending total of {replay_frames.Length} replay frames");
        if (Config.API.GetApiOnly())
        {
            return;
        }
        else
        {
            await InsertMapTime(player, bonus, stage, run_ticks, replay_frames, true);

            if (stage != 0 || bonus != 0)
            {
                Console.WriteLine($"CS2 Surf DEBUG >> internal class CurrentRun -> public async Task SaveMapTime -> Inserted an entry for {(stage != 0 ? "Stage" : "Bonus")} {(stage != 0 ? stage : bonus)} - {run_ticks}");
            }
            else
            {
                await SaveCurrentRunCheckpoints(player, true); // Save this run's checkpoints
            }

            await player.CurrMap.Get_Map_Record_Runs(); // Reload the times for the Map
        }
    }

    public void PrintSituations(Player player)
    {
        Console.WriteLine($"========================== FOUND SITUATIONS ==========================");
        for (int i = 0; i < player.ReplayRecorder.Frames.Count; i++)
        {
            ReplayFrame x = player.ReplayRecorder.Frames[i];
            switch (x.Situation)
            {
                case ReplayFrameSituation.START_ZONE_ENTER:
                    Console.WriteLine($"START_ZONE_ENTER: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.START_ZONE_EXIT:
                    Console.WriteLine($"START_ZONE_EXIT: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.STAGE_ZONE_ENTER:
                    Console.WriteLine($"STAGE_ZONE_ENTER: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.STAGE_ZONE_EXIT:
                    Console.WriteLine($"STAGE_ZONE_EXIT: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.CHECKPOINT_ZONE_ENTER:
                    Console.WriteLine($"CHECKPOINT_ZONE_ENTER: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.CHECKPOINT_ZONE_EXIT:
                    Console.WriteLine($"CHECKPOINT_ZONE_EXIT: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.END_ZONE_ENTER:
                    Console.WriteLine($"END_ZONE_ENTER: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.END_ZONE_EXIT:
                    Console.WriteLine($"END_ZONE_EXIT: {i} | Situation {x.Situation}");
                    break;
            }
        }
        Console.WriteLine($"==========================                  ==========================");
    }

    /// <summary>
    /// Saves the CurrentRun of the player to the database. Does NOT support Bonus entries yet.
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="bonus">Bonus number</param>
    /// <param name="stage">Stage number</param>
    /// <param name="run_ticks">Ticks for the run</param>
    /// <param name="replay_frames">Replay frames</param>
    /// <param name="reloadData">Whether to reload the PersonalBest data for the Player.</param>
    /// <returns></returns>
    public async Task InsertMapTime(Player player, int bonus = 0, int stage = 0, int run_ticks = -1, string replay_frames = "", bool reloadData = false)
    {
        int playerId = player.Profile.ID;
        int mapId = player.CurrMap.ID;
        int style = player.Timer.Style;
        int ticks = run_ticks == -1 ? player.Stats.ThisRun.Ticks : run_ticks;
        // int ticks = player.Stats.ThisRun.Ticks;
        int type = stage != 0 ? 2 : bonus != 0 ? 1 : 0;
        float startVelX = player.Stats.ThisRun.StartVelX;
        float startVelY = player.Stats.ThisRun.StartVelY;
        float startVelZ = player.Stats.ThisRun.StartVelZ;
        float endVelX = player.Stats.ThisRun.EndVelX;
        float endVelY = player.Stats.ThisRun.EndVelY;
        float endVelZ = player.Stats.ThisRun.EndVelZ;

        if (Config.API.GetApiOnly()) // API Calls
        {
            // API Insert map goes here
        }
        else // MySQL Queries
        {
            int updatePlayerRunTask = await SurfTimer.DB.WriteAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_CR_INSERT_TIME, playerId, mapId, style, type, type == 2 ? stage : type == 1 ? bonus : 0, ticks, startVelX, startVelY, startVelZ, endVelX, endVelY, endVelZ, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), replay_frames));
            if (updatePlayerRunTask <= 0)
            {
                Exception ex = new($"CS2 Surf ERROR >> internal class CurrentRun -> public async Task InsertMapTime -> Failed to insert/update player run in database. Player: {player.Profile.Name} ({player.Profile.SteamID})");
                throw ex;
            }

            if (reloadData && type == 0)
            {
                Console.WriteLine($"CS2 Surf DEBUG >> internal class CurrentRun -> public async Task InsertMapTime -> Will reload MapTime (Type {type}) data for '{player.Profile.Name}' (ID {player.Stats.PB[player.Timer.Style].ID}))");
                await player.Stats.PB[style].PB_LoadPlayerSpecificMapTimeData(player); // Load the Map MapTime PB data again (will refresh the MapTime ID for the Checkpoints query)
            }
            else if (reloadData && type == 1)
            {
                Console.WriteLine($"CS2 Surf DEBUG >> internal class CurrentRun -> public async Task InsertMapTime -> Will reload Bonus MapTime (Type {type}) data for '{player.Profile.Name}' (ID {player.Stats.BonusPB[bonus][style].ID}))");
                await player.Stats.BonusPB[bonus][style].PB_LoadPlayerSpecificMapTimeData(player); // Load the Bonus MapTime PB data again (will refresh the MapTime ID)
            }
            else if (reloadData && type == 2)
            {
                Console.WriteLine($"CS2 Surf DEBUG >> internal class CurrentRun -> public async Task InsertMapTime -> Will reload Stage MapTime (Type {type}) data for '{player.Profile.Name}' (ID {player.Stats.StagePB[stage][style].ID}))");
                await player.Stats.StagePB[stage][style].PB_LoadPlayerSpecificMapTimeData(player); // Load the Stage MapTime PB data again (will refresh the MapTime ID)
            }
        }
    }

    /// <summary>
    /// Saves the `CurrentRunCheckpoints` dictionary to the database
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="reloadData">Whether to reload the PersonalBest Checkpoints data for the Player.</param>
    public async Task SaveCurrentRunCheckpoints(Player player, bool reloadData = false)
    {
        Console.WriteLine($"CS2 Surf DEBUG >> internal class CurrentRun -> SaveCurrentRunCheckpoints -> Will send {player.Stats.ThisRun.Checkpoints.Count} ({this.Checkpoints.Count}) checkpoints to DB....");
        int style = player.Timer.Style;
        int mapTimeId = player.Stats.PB[style].ID;
        List<string> commands = new List<string>();
        // Loop through the checkpoints and insert/update them in the database for the run
        // foreach (var item in player.Stats.ThisRun.Checkpoints)
        foreach (var item in this.Checkpoints)
        {
            int cp = item.Key;
            int ticks = item.Value!.Ticks;
            int endTouch = item.Value!.EndTouch;
            double startVelX = item.Value!.StartVelX;
            double startVelY = item.Value!.StartVelY;
            double startVelZ = item.Value!.StartVelZ;
            double endVelX = item.Value!.EndVelX;
            double endVelY = item.Value!.EndVelY;
            double endVelZ = item.Value!.EndVelZ;
            int attempts = item.Value!.Attempts;

#if DEBUG
            Console.WriteLine($"CP: {cp} | MapTime ID: {mapTimeId} | Time: {endTouch} | Ticks: {ticks} | startVelX: {startVelX} | startVelY: {startVelY} | startVelZ: {startVelZ} | endVelX: {endVelX} | endVelY: {endVelY} | endVelZ: {endVelZ}");
            Console.WriteLine($@"CS2 Surf DEBUG >> internal class CurrentRun -> SaveCurrentRunCheckpoints -> 
                {string.Format(
                    Config.MySQL.Queries.DB_QUERY_CR_INSERT_CP,
                    mapTimeId, cp, ticks, startVelX, startVelY, startVelZ, endVelX, endVelY, endVelZ, attempts, endTouch)}
            ");
#endif

            // Insert/Update CPs to database
            // Check if the player has PB object initialized and if the player's character is currently active in the game
            if (item.Value != null && player.Controller.PlayerPawn.Value != null)
            {
                string command = string.Format(
                    Config.MySQL.Queries.DB_QUERY_CR_INSERT_CP,
                    mapTimeId, cp, ticks, startVelX, startVelY, startVelZ, endVelX, endVelY, endVelZ, attempts, endTouch
                );
                commands.Add(command);
            }
        }
        await SurfTimer.DB.TransactionAsync(commands);
        player.Stats.ThisRun.Checkpoints.Clear();

        if (reloadData)
        {
            Console.WriteLine($"CS2 Surf DEBUG >> internal class CurrentRun -> public async Task SaveCurrentRunCheckpoints -> Will reload Checkpoints data for {player.Profile.Name} (ID {player.Stats.PB[player.Timer.Style].ID})");
            await player.Stats.PB[player.Timer.Style].PB_LoadCheckpointsData(); // Load the Checkpoints data again
        }
    }
}

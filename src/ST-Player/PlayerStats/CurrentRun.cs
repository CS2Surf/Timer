namespace SurfTimer;

/// <summary>
/// This class stores data for the current run.
/// </summary>
internal class CurrentRun
{
    public Dictionary<int, Checkpoint> Checkpoint { get; set; } // Current RUN checkpoints tracker
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
        Checkpoint = new Dictionary<int, Checkpoint>();
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
        Checkpoint.Clear();
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
    public void SaveMapTime(Player player, TimerDatabase DB, int bonus = 0, int stage = 0, int run_ticks = -1)
    {
        // Add entry in DB for the run
        // To-do: add `type`
        string replay_frames = player.ReplayRecorder.SerializeReplay();

        if (run_ticks == -1 || bonus != 2)
        {
            run_ticks = player.Stats.ThisRun.Ticks;
            replay_frames = player.ReplayRecorder.SerializeReplayPortion(player.ReplayRecorder.Frames.Count-1-run_ticks, run_ticks);
        }

        int style = player.Timer.Style;
        Task<int> updatePlayerRunTask = DB.Write($@"
            INSERT INTO `MapTimes` 
            (`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`, `replay_frames`) 
            VALUES ({player.Profile.ID}, {player.CurrMap.ID}, {style}, {bonus}, {stage}, {run_ticks}, 
            {player.Stats.ThisRun.StartVelX}, {player.Stats.ThisRun.StartVelY}, {player.Stats.ThisRun.StartVelZ}, {player.Stats.ThisRun.EndVelX}, {player.Stats.ThisRun.EndVelY}, {player.Stats.ThisRun.EndVelZ}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, '{replay_frames}') 
            ON DUPLICATE KEY UPDATE run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), 
            start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date), replay_frames=VALUES(replay_frames);
        ");
        if (updatePlayerRunTask.Result <= 0)
            throw new Exception($"CS2 Surf ERROR >> internal class PersonalBest -> SaveMapTime -> Failed to insert/update player run in database. Player: {player.Profile.Name} ({player.Profile.SteamID})");
        updatePlayerRunTask.Dispose();

        // Will have to LoadMapTimesData right here as well to get the ID of the run we just inserted
        // this.SaveCurrentRunCheckpoints(player, DB); // Save checkpoints for this run
        // this.LoadCheckpointsForRun(DB); // Re-Load checkpoints for this run
    }

    /// <summary>
    /// Saves the `CurrentRunCheckpoints` dictionary to the database
    /// We need the correct `this.ID` to be populated before calling this method otherwise Query will fail
    /// </summary>
    public async Task SaveCurrentRunCheckpoints(Player player, TimerDatabase DB)
    {
        int style = player.Timer.Style;
        List<string> commands = new List<string>();
        // Loop through the checkpoints and insert/update them in the database for the run
        foreach (var item in player.Stats.ThisRun.Checkpoint)
        {
            int cp = item.Key;
            int ticks = item.Value!.Ticks;
            int runTime = item.Value!.Ticks / 64; // Runtime in decimal
            double startVelX = item.Value!.StartVelX;
            double startVelY = item.Value!.StartVelY;
            double startVelZ = item.Value!.StartVelZ;
            double endVelX = item.Value!.EndVelX;
            double endVelY = item.Value!.EndVelY;
            double endVelZ = item.Value!.EndVelZ;
            int attempts = item.Value!.Attempts;

            #if DEBUG
            Console.WriteLine($"CP: {cp} | MapTime ID: {player.Stats.PB[style].ID} | Time: {runTime} | Ticks: {ticks} | startVelX: {startVelX} | startVelY: {startVelY} | startVelZ: {startVelZ} | endVelX: {endVelX} | endVelY: {endVelY} | endVelZ: {endVelZ}");
            Console.WriteLine($@"CS2 Surf DEBUG >> internal class Checkpoint : PersonalBest -> SaveCurrentRunCheckpoints -> 
                INSERT INTO `Checkpoints` 
                (`maptime_id`, `cp`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, 
                `end_vel_x`, `end_vel_y`, `end_vel_z`, `attempts`, `end_touch`) 
                VALUES ({player.Stats.PB[style].ID}, {cp}, {runTime}, {startVelX}, {startVelY}, {startVelZ}, {endVelX}, {endVelY}, {endVelZ}, {attempts}, {ticks}) ON DUPLICATE KEY UPDATE 
                run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), 
                end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), attempts=VALUES(attempts), end_touch=VALUES(end_touch);
            ");
            #endif

            // Insert/Update CPs to database
            // Check if the player has PB object initialized and if the player's character is currently active in the game
            if (item.Value != null && player.Controller.PlayerPawn.Value != null)
            {
                string command = $@"
                    INSERT INTO `Checkpoints` 
                    (`maptime_id`, `cp`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, 
                    `end_vel_x`, `end_vel_y`, `end_vel_z`, `attempts`, `end_touch`) 
                    VALUES ({player.Stats.PB[style].ID}, {cp}, {runTime}, {startVelX}, {startVelY}, {startVelZ}, {endVelX}, {endVelY}, {endVelZ}, {attempts}, {ticks}) 
                    ON DUPLICATE KEY UPDATE 
                    run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), 
                    end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), attempts=VALUES(attempts), end_touch=VALUES(end_touch);
                ";
                commands.Add(command);
            }
        }
        await DB.Transaction(commands);
        player.Stats.ThisRun.Checkpoint.Clear();
    }
}

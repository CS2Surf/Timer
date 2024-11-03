namespace SurfTimer;

/// <summary>
/// As the PersonalBest object is being used for each different style, we shouldn't need a separate `Style` variable in here because each style entry will have unique ID in the Database
/// and will therefore be a unique PersonalBest entry.
/// </summary>
internal class PersonalBest
{
    public int ID { get; set; } = -1; // Exclude from constructor, retrieve from Database when loading/saving
    public int Ticks { get; set; }
    public int Rank { get; set; } = -1; // Exclude from constructor, retrieve from Database when loading/saving
    public Dictionary<int, Checkpoint> Checkpoints { get; set; }
    public int Type { get; set; } // Identifies bonus # - 0 for map time -> huh, why o_O?
    public float StartVelX { get; set; }
    public float StartVelY { get; set; }
    public float StartVelZ { get; set; }
    public float EndVelX { get; set; }
    public float EndVelY { get; set; }
    public float EndVelZ { get; set; }
    public int RunDate { get; set; }
    public string Name { get; set; } = ""; // This is used only for WRs
    // Add other properties as needed

    // Constructor
    public PersonalBest()
    {
        Ticks = -1;
        Checkpoints = new Dictionary<int, Checkpoint>();
        Type = -1;
        StartVelX = -1.0f;
        StartVelY = -1.0f;
        StartVelZ = -1.0f;
        EndVelX = -1.0f;
        EndVelY = -1.0f;
        EndVelZ = -1.0f;
        RunDate = -1;
    }

    /// <summary>
    /// Loads the Checkpoint data for the given MapTime_ID. Used for loading player's personal bests and Map's world records.
    /// Automatically detects whether to use API Calls or MySQL query.
    /// Bonus and Stage runs should NOT have any checkpoints.
    /// </summary>
    public async Task PB_LoadCheckpointsData()
    {
        if (this == null)
        {
#if DEBUG
            Console.WriteLine("CS2 Surf ERROR >> internal class PersonalBest -> public async Task PB_LoadCheckpointsData -> PersonalBest object is null.");
#endif
            return;
        }
        if (this.Checkpoints == null)
        {
#if DEBUG
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadCheckpointsData -> PB Checkpoints list is not initialized.");
#endif
            this.Checkpoints = new Dictionary<int, Checkpoint>(); // Initialize if null
        }

        if (Config.API.GetApiOnly()) // Load with API
        {
            var checkpoints = await ApiMethod.GET<API_Checkpoint[]>(string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_RUN_CPS, this.ID));
            if (checkpoints == null || checkpoints.Length == 0)
                return;

            foreach (API_Checkpoint checkpoint in checkpoints)
            {
                Checkpoint cp = new Checkpoint
                (
                    checkpoint.cp,
                    checkpoint.run_time,
                    checkpoint.start_vel_x,
                    checkpoint.start_vel_y,
                    checkpoint.start_vel_z,
                    checkpoint.end_vel_x,
                    checkpoint.end_vel_y,
                    checkpoint.end_vel_z,
                    checkpoint.end_touch,
                    checkpoint.attempts
                );
                cp.ID = checkpoint.cp;

                this.Checkpoints[cp.CP] = cp;
            }
        }
        else // Load with MySQL
        {
            using (var results = await SurfTimer.DB.QueryAsync(string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_CPS, this.ID)))
            {
#if DEBUG
                Console.WriteLine($"this.Checkpoint.Count {this.Checkpoints.Count} ");
                Console.WriteLine($"this.ID {this.ID} ");
                Console.WriteLine($"this.Ticks {this.Ticks} ");
                Console.WriteLine($"this.RunDate {this.RunDate} ");
#endif

                if (!results.HasRows)
                {
#if DEBUG
                    Console.WriteLine($"CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadCheckpointsData -> No checkpoints found for this mapTimeId {this.ID}.");
#endif

                    return;
                }

#if DEBUG
                Console.WriteLine($"======== CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadCheckpointsData -> Checkpoints found for this mapTimeId");
#endif

                while (results.Read())
                {
#if DEBUG
                    Console.WriteLine($"cp {results.GetInt32("cp")} ");
                    Console.WriteLine($"run_time {results.GetInt32("run_time")} ");
                    Console.WriteLine($"sVelX {results.GetFloat("start_vel_x")} ");
                    Console.WriteLine($"sVelY {results.GetFloat("start_vel_y")} ");
#endif

                    Checkpoint cp = new(results.GetInt32("cp"),
                                        results.GetInt32("run_time"),
                                        results.GetFloat("start_vel_x"),
                                        results.GetFloat("start_vel_y"),
                                        results.GetFloat("start_vel_z"),
                                        results.GetFloat("end_vel_x"),
                                        results.GetFloat("end_vel_y"),
                                        results.GetFloat("end_vel_z"),
                                        results.GetInt32("end_touch"),
                                        results.GetInt32("attempts"));
                    cp.ID = results.GetInt32("cp");
                    // To-do: cp.ID = calculate Rank # from DB

                    this.Checkpoints[cp.CP] = cp;

#if DEBUG
                    Console.WriteLine($"======= CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadCheckpointsData -> Loaded CP {cp.CP} with RunTime {cp.Ticks}.");
#endif
                }
            }
        }

        // #if DEBUG
        Console.WriteLine($"======= CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadCheckpointsData -> [{(Config.API.GetApiOnly() ? "API" : "DB")}] {this.Checkpoints.Count} Checkpoints loaded from DB for run ID {this.ID}.");
        // #endif
    }

    /// <summary>
    /// Loads specific type/style MapTime data for the player (run without checkpoints) from the database for their personal best runs.
    /// Should be used to reload data from a specific `PersonalBest` object
    /// </summary>
    /// <param name="player">Player object</param>
    public async Task PB_LoadPlayerSpecificMapTimeData(Player player)
    {
        // Console.WriteLine($"CS2 Surf ERROR >> internal class PersonalBest -> public async Task PB_LoadPlayerSpecificMapTimeData -> QUERY:\n{string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_RUNTIME, player.Profile.ID, player.CurrMap.ID, 0, player.Timer.Style)}");
        // using (var results = await SurfTimer.DB.QueryAsync(string.Format(Config.MySQL.Queries.DB_QUERY_PB_GET_RUNTIME, player.Profile.ID, player.CurrMap.ID, 0, player.Timer.Style)))
        if (this == null)
        {
#if DEBUG
            Console.WriteLine("CS2 Surf ERROR >> internal class PersonalBest -> public async Task PB_LoadPlayerSpecificMapTimeData -> PersonalBest object is null.");
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
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadPlayerSpecificMapTimeData -> No MapTime data found for '{player.Profile.Name}' ({player.Profile.ID}). (Null? {results == null})");
            // #endif

            return;
        }

#if DEBUG
        Console.WriteLine($"======== CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadPlayerSpecificMapTimeData -> Found a run for '{player.Profile.Name}' ({player.Profile.ID}).");
#endif

        while (results.Read())
        {
#if DEBUG
            Console.WriteLine($"cp {results.GetInt32("cp")} ");
            Console.WriteLine($"run_time {results.GetInt32("run_time")} ");
            Console.WriteLine($"sVelX {results.GetFloat("start_vel_x")} ");
            Console.WriteLine($"sVelY {results.GetFloat("start_vel_y")} ");
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

#if DEBUG
            Console.WriteLine($"======= CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadPlayerSpecificMapTimeData -> Loaded run (MapId = {this.ID}) by {player.Profile.Name} - {PlayerHUD.FormatTime(this.Ticks)}");
#endif
        }

        // #if DEBUG
        Console.WriteLine($"======= CS2 Surf DEBUG >> internal class PersonalBest -> public async Task PB_LoadPlayerSpecificMapTimeData -> MapTime (Type: {this.Type}) loaded from DB. ID: {this.ID} for {player.Profile.Name}");
        // #endif
    }
}
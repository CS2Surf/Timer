using MySqlConnector;

namespace SurfTimer;

internal class CurrentRun
{
    public Dictionary<int, PersonalBest.CheckpointObject> Checkpoint { get; set; } // Current RUN checkpoints tracker
    public int RunTime { get; set; } // To-do: will be the last (any) zone end touch time
    public float StartVelX { get; set; }
    public float StartVelY { get; set; }
    public float StartVelZ { get; set; }
    public float EndVelX { get; set; }
    public float EndVelY { get; set; }
    public float EndVelZ { get; set; }
    public int RunDate { get; set; }
    // Add other properties as needed

    // Constructor
    public CurrentRun()
    {
        Checkpoint = new Dictionary<int, PersonalBest.CheckpointObject>();
        RunTime = 0;
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
        RunTime = 0;
        StartVelX = 0.0f;
        StartVelY = 0.0f;
        StartVelZ = 0.0f;
        EndVelX = 0.0f;
        EndVelY = 0.0f;
        EndVelZ = 0.0f;
        RunDate = 0;
        // Reset other properties as needed
    }
}

// To-do: make Style (currently 0) be dynamic
// To-do: add `Type`
internal class PersonalBest
{
    public int ID { get; set; }
    public int RunTime { get; set; }
    public Dictionary<int, CheckpointObject> Checkpoint { get; set; }
    // public int Type { get; set; }
    public float StartVelX { get; set; }
    public float StartVelY { get; set; }
    public float StartVelZ { get; set; }
    public float EndVelX { get; set; }
    public float EndVelY { get; set; }
    public float EndVelZ { get; set; }
    public int RunDate { get; set; }
    public float currentRunStartVelX { get; set; }
    public float currentRunStartVelY { get; set; }
    public float currentRunStartVelZ { get; set; }
    // Add other properties as needed

    internal class CheckpointObject
    {
        public int CP { get; set; }
        public int RunTime { get; set; } // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
        public int Ticks { get; set; } // To-do: this was supposed to be the ticks but that is used for run_time for HUD????
        public float Speed { get; set; }
        public float StartVelX { get; set; }
        public float StartVelY { get; set; }
        public float StartVelZ { get; set; }
        public float EndVelX { get; set; }
        public float EndVelY { get; set; }
        public float EndVelZ { get; set; }
        public float EndTouch { get; set; }
        public int Attempts { get; set; }

        public CheckpointObject(int cp, int runTime, int ticks, float speed, float startVelX, float startVelY, float startVelZ, float endVelX, float endVelY, float endVelZ, float endTouch, int attempts)
        {
            CP = cp;
            RunTime = runTime; // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
            Ticks = ticks; // To-do: this was supposed to be the ticks but that is used for run_time for HUD????
            Speed = speed;
            StartVelX = startVelX;
            StartVelY = startVelY;
            StartVelZ = startVelZ;
            EndVelX = endVelX;
            EndVelY = endVelY;
            EndVelZ = endVelZ;
            EndTouch = endTouch;
            Attempts = attempts;
        }
    }

    // Constructor
    public PersonalBest(int id, int runTime, float startVelX, float startVelY, float startVelZ, float endVelX, float endVelY, float endVelZ, int runDate)
    {
        ID = id;
        RunTime = runTime; // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
        Checkpoint = new Dictionary<int, CheckpointObject>();
        // Type = type;
        StartVelX = startVelX;
        StartVelY = startVelY;
        StartVelZ = startVelZ;
        EndVelX = endVelX;
        EndVelY = endVelY;
        EndVelZ = endVelZ;
        RunDate = runDate;
    }

    // Executes the DB query to get all the checkpoints and store them in the Checkpoint dictionary
    public void LoadCheckpointsForRun(int mapTimeId, TimerDatabase DB)
    {
        Task<MySqlDataReader> dbTask = DB.Query($"SELECT * FROM `Checkpoints` WHERE `maptime_id` = {mapTimeId};");
        MySqlDataReader results = dbTask.Result;
        if (this == null)
        {
            #if DEBUG
            Console.WriteLine("CS2 Surf ERROR >> internal class PersonalBest -> LoadCheckpointsForRun -> PersonalBest object is null.");
            #endif

            results.Close();
            return;
        }

        if (this.Checkpoint == null)
        {
            #if DEBUG
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PersonalBest -> LoadCheckpointsForRun -> Checkpoints list is not initialized.");
            #endif

            this.Checkpoint = new Dictionary<int, CheckpointObject>(); // Initialize if null
        }

        #if DEBUG
        Console.WriteLine($"this.Checkpoint.Count {this.Checkpoint.Count} ");
        Console.WriteLine($"this.ID {this.ID} ");
        Console.WriteLine($"this.RunTime {this.RunTime} ");
        Console.WriteLine($"this.RunDate {this.RunDate} ");
        #endif

        if (!results.HasRows)
        {
            #if DEBUG
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PersonalBest -> LoadCheckpointsForRun -> No checkpoints found for this mapTimeId {this.ID}.");
            #endif

            results.Close();
            return;
        }

        #if DEBUG
        Console.WriteLine($"======== CS2 Surf DEBUG >> internal class PersonalBest -> LoadCheckpointsForRun -> Checkpoints found for this mapTimeId");
        #endif

        while (results.Read())
        {
            #if DEBUG
            Console.WriteLine($"cp {results.GetInt32("cp")} ");
            Console.WriteLine($"run_time {results.GetFloat("run_time")} ");
            Console.WriteLine($"sVelX {results.GetFloat("start_vel_x")} ");
            Console.WriteLine($"sVelY {results.GetFloat("start_vel_y")} ");
            #endif

            CheckpointObject cp = new(results.GetInt32("cp"),
                                results.GetInt32("run_time"), // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
                                results.GetInt32("run_time"), // To-do: this was supposed to be the ticks but that is used for run_time for HUD
                                666.666f,
                                results.GetFloat("start_vel_x"),
                                results.GetFloat("start_vel_y"),
                                results.GetFloat("start_vel_z"),
                                results.GetFloat("end_vel_x"),
                                results.GetFloat("end_vel_y"),
                                results.GetFloat("end_vel_z"),
                                results.GetFloat("end_touch"),
                                results.GetInt32("attempts"));
            Checkpoint[cp.CP] = cp;

            #if DEBUG
            Console.WriteLine($"======= CS2 Surf DEBUG >> internal class PersonalBest -> LoadCheckpointsForRun -> Loaded CP {cp.CP} with RunTime {cp.RunTime}.");
            #endif
        }
        results.Close();

        #if DEBUG
        Console.WriteLine($"======= CS2 Surf DEBUG >> internal class PersonalBest -> LoadCheckpointsForRun -> Checkpoints loaded from DB. Count: {Checkpoint.Count}");
        #endif
    }

    // To-do: Transactions? Server freezes for a bit here sometimes
    // Saves the CurrentRunCheckpoints to the database
    public void SaveCurrentRunCheckpoints(Player player, TimerDatabase DB)
    {
        // Loop through the checkpoints and insert/update them in the database for the run
        foreach (var item in player.Stats.ThisRun.Checkpoint) // player.Timer.CurrentRunCheckpoints
        {
            int cp = item.Key;
            int runTime = item.Value.RunTime; // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
            int ticks = item.Value.Ticks; // To-do: this was supposed to be the ticks but that is used for run_time for HUD
            double speed = item.Value.Speed;
            double startVelX = item.Value.StartVelX;
            double startVelY = item.Value.StartVelY;
            double startVelZ = item.Value.StartVelZ;
            double endVelX = item.Value.EndVelX;
            double endVelY = item.Value.EndVelY;
            double endVelZ = item.Value.EndVelZ;
            int attempts = item.Value.Attempts;

            #if DEBUG
            Console.WriteLine($"CP: {cp} | Time: {runTime} | Ticks: {ticks} | Speed: {speed} | startVelX: {startVelX} | startVelY: {startVelY} | startVelZ: {startVelZ} | endVelX: {endVelX} | endVelY: {endVelY} | endVelZ: {endVelZ}");
            Console.WriteLine($"CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> " +
                                $"INSERT INTO `Checkpoints` " +
                                $"(`maptime_id`, `cp`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, " +
                                $"`end_vel_x`, `end_vel_y`, `end_vel_z`, `attempts`, `end_touch`) " +
                                $"VALUES ({player.Stats.PB[0].ID}, {cp}, {runTime}, {startVelX}, {startVelY}, {startVelZ}, {endVelX}, {endVelY}, {endVelZ}, {attempts}, {ticks}) ON DUPLICATE KEY UPDATE " +
                                $"run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), " +
                                $"end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), attempts=VALUES(attempts), end_touch=VALUES(end_touch);");
            #endif

            // Insert/Update CPs to database
            // To-do: Transactions?
            // Check if the player has PB object initialized and if the player's character is currently active in the game
            if (player.Stats.PB[0] != null && player.Controller.PlayerPawn.Value != null)
            {
                Task<int> newPbTask = DB.Write($"INSERT INTO `Checkpoints` " +
                                $"(`maptime_id`, `cp`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, " +
                                $"`end_vel_x`, `end_vel_y`, `end_vel_z`, `attempts`, `end_touch`) " +
                                $"VALUES ({player.Stats.PB[0].ID}, {cp}, {runTime}, {startVelX}, {startVelY}, {startVelZ}, {endVelX}, {endVelY}, {endVelZ}, {attempts}, {ticks}) ON DUPLICATE KEY UPDATE " +
                                $"run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), " +
                                $"end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), attempts=VALUES(attempts), end_touch=VALUES(end_touch);");
                if (newPbTask.Result <= 0)
                    throw new Exception($"CS2 Surf ERROR >> OnTriggerStartTouch (Checkpoint zones) -> Inserting Checkpoints. CP: {cp} | Name: {player.Profile.Name}");
                newPbTask.Dispose();
            }
        }
    }
}

internal class PlayerStats
{
    // To-Do: Each stat should be a class of its own, with its own methods and properties - easier to work with. 
    //        Temporarily, we store ticks + basic info so we can experiment

    // These account for future style support and a relevant index.
    // public int[,] PB {get; set;} = {{0,0}}; // First dimension: style (0 = normal), second dimension: map/bonus (0 = map, 1+ = bonus index)
    public Dictionary<int, PersonalBest> PB { get; set; } = new Dictionary<int, PersonalBest>();

    // Initialize default styles (e.g., 0 for normal)
    public PlayerStats()
    {
        PB[0] = new PersonalBest(0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0);
        // Add more styles as needed
    }
    public CurrentRun ThisRun {get; set;} = new CurrentRun(); // This is a CurrenntRun object that tracks the data for the Player's current run
    public int[,] Rank { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: map/bonus (0 = map, 1+ = bonus index)
    // public int[,] Checkpoints { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: checkpoint index
    public int[,] StagePB { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: stage index
    public int[,] StageRank { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: stage index

    // This can populate all the `style` stats the player has for the map - currently only 1 style is supported
    public void LoadMapTimesData(int playerId, int mapId, TimerDatabase DB)
    {
        Task<MySqlDataReader> dbTask2 = DB.Query($"SELECT * FROM `MapTimes` WHERE `player_id` = {playerId} AND `map_id` = {mapId};");
        MySqlDataReader playerStats = dbTask2.Result;
        int style = 0;
        if (!playerStats.HasRows)
        {
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> LoadMapTimesData -> No MapTimes data found for Player.");
        }
        else
        {
            while (playerStats.Read())
            {
                // style = playerStats.GetInt32("style"); // Uncomment when style is implemented
                // Load data into PersonalBest object
                PB[style].ID = playerStats.GetInt32("id");
                PB[style].StartVelX = (float)playerStats.GetDouble("start_vel_x");
                PB[style].StartVelY = (float)playerStats.GetDouble("start_vel_y");
                PB[style].StartVelZ = (float)playerStats.GetDouble("start_vel_z");
                PB[style].EndVelX = (float)playerStats.GetDouble("end_vel_x");
                PB[style].EndVelY = (float)playerStats.GetDouble("end_vel_y");
                PB[style].EndVelZ = (float)playerStats.GetDouble("end_vel_z");
                PB[style].RunTime = playerStats.GetInt32("run_time");
                PB[style].RunDate = playerStats.GetInt32("run_date");

                Console.WriteLine($"============== CS2 Surf DEBUG >> LoadMapTimesData -> {PB[style].ID} | {PB[style].RunTime} | {PB[style].StartVelX} | {PB[style].StartVelY} | {PB[style].StartVelZ} | {PB[style].EndVelX} | {PB[style].EndVelY} | {PB[style].EndVelZ} | {PB[style].RunDate}");
                #if DEBUG
                Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> LoadMapTimesData -> PlayerStats.PB (ID {PB[style].ID}) loaded from DB.");
                #endif
            }
        }
        playerStats.Close();
    }

}
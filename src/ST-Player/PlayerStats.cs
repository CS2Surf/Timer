using MySqlConnector;

namespace SurfTimer;

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

    // Executes the DB query to parse the checkpoints
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
    public int[,] Rank { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: map/bonus (0 = map, 1+ = bonus index)
    // public int[,] Checkpoints { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: checkpoint index
    public int[,] StagePB { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: stage index
    public int[,] StageRank { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: stage index

    public void LoadMapTimesData(int playerId, int mapId, TimerDatabase DB)
    // public void LoadMapTimesData(MySqlDataReader results)
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
                Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerStats -> LoadMapTimesData -> PlayerStats (ID {PB[style].ID}) loaded from DB.");
                #endif
            }
        }
        playerStats.Close();        
    }

}
namespace SurfTimer;

// To-do: make Style (currently 0) be dynamic
// To-do: add `Type`
internal class PersonalBest
{
    public int ID { get; set; }
    public int RunTime { get; set; }
    // public int Type { get; set; }
    public float StartVelX { get; set; }
    public float StartVelY { get; set; }
    public float StartVelZ { get; set; }
    public float EndVelX { get; set; }
    public float EndVelY { get; set; }
    public float EndVelZ { get; set; }
    public int RunDate { get; set; }
    // Add other properties as needed

    // Constructor
    public PersonalBest(int id, int runTime, float startVelX, float startVelY, float startVelZ, float endVelX, float endVelY, float endVelZ, int runDate)
    {
        ID = id;
        RunTime = runTime;
        // Type = type;
        StartVelX = startVelX;
        StartVelY = startVelY;
        StartVelZ = startVelZ;
        EndVelX = endVelX;
        EndVelY = endVelY;
        EndVelZ = endVelZ;
        RunDate = runDate;
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
    public int[,] Checkpoints { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: checkpoint index
    public int[,] StagePB { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: stage index
    public int[,] StageRank { get; set; } = { { 0, 0 } }; // First dimension: style (0 = normal), second dimension: stage index
}
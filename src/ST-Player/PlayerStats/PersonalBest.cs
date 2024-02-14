namespace SurfTimer;

// To-do: make Style (currently 0) be dynamic
// To-do: add `Type`
internal class PersonalBest
{
    public int ID { get; set; } = -1; // Exclude from constructor, retrieve from Database when loading/saving
    public int Ticks { get; set; }
    public int Rank { get; set; } = -1; // Exclude from constructor, retrieve from Database when loading/saving
    public Dictionary<int, Checkpoint> Checkpoint { get; set; }
    // public int Type { get; set; }
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
        Checkpoint = new Dictionary<int, Checkpoint>();
        // Type = type;
        StartVelX = -1.0f;
        StartVelY = -1.0f;
        StartVelZ = -1.0f;
        EndVelX = -1.0f;
        EndVelY = -1.0f;
        EndVelZ = -1.0f;
        RunDate = 0;
    }
}
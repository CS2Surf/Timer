namespace SurfTimer;

public abstract class RunStats
{
    public Dictionary<int, Checkpoint> Checkpoints { get; set; }
    public int Ticks { get; set; }
    public float StartVelX { get; set; }
    public float StartVelY { get; set; }
    public float StartVelZ { get; set; }
    public float EndVelX { get; set; }
    public float EndVelY { get; set; }
    public float EndVelZ { get; set; }
    public int RunDate { get; set; }
    public string ReplayFramesBase64 { get; set; } = "";

    protected RunStats()
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

    // Shared Method
    public virtual void Reset()
    {
        Checkpoints.Clear();
        Ticks = 0;
        StartVelX = StartVelY = StartVelZ = 0.0f;
        EndVelX = EndVelY = EndVelZ = 0.0f;
        RunDate = 0;
    }
}

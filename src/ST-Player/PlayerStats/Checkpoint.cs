public class Checkpoint
{
    public int CP { get; set; }
    public int Ticks { get; set; }
    public float StartVelX { get; set; }
    public float StartVelY { get; set; }
    public float StartVelZ { get; set; }
    public float EndVelX { get; set; }
    public float EndVelY { get; set; }
    public float EndVelZ { get; set; }
    public int EndTouch { get; set; }
    public int Attempts { get; set; }
    public int ID { get; set; }

    public Checkpoint() { }

    public Checkpoint(int cp, int ticks, float startVelX, float startVelY, float startVelZ, float endVelX, float endVelY, float endVelZ, int endTouch, int attempts)
    {
        CP = cp;
        Ticks = ticks;
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

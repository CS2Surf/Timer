namespace SurfTimer;

internal class Checkpoint : PersonalBest
{
    public int CP { get; set; }
    public int EndTouch { get; set; }
    public int Attempts { get; set; }

    public Checkpoint(int cp, int ticks, float startVelX, float startVelY, float startVelZ, float endVelX, float endVelY, float endVelZ, int endTouch, int attempts)
    {
        CP = cp;
        Ticks = ticks; // To-do: this was supposed to be the ticks but that is used for run_time for HUD????
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
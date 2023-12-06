namespace SurfTimer;

internal class PlayerTimer
{
    // Status
    public bool Enabled {get; set;} = true; // Enable toggle for entire timer
    public bool Paused {get; set;} = false; // Pause toggle for timer
    public bool IsRunning {get; set;} = false; // Is the timer currently running?

    // Mode
    public bool Practice {get; set;} = false;

    // Timing
    public int Ticks {get; set;} = 0; // To-do: sub-tick counting? This currently goes on OnTick, which is not sub-tick I believe? Needs investigating

    // Methods
    public void Reset()
    {
        this.Stop();
        this.Ticks = 0;
        this.Paused = false;
        this.Practice = false;
    }

    public void Pause()
    {
        this.Paused = true;
    }

    public void Start()
    {
        // Timer Start method - notes: OnStartTimerPress
        if (this.Enabled)
            this.IsRunning = true;
    }

    public void Stop()
    {
        // Timer Stop method - notes: OnStopTimerPress
        this.IsRunning = false;
    }

    public void Tick()
    {
        // Tick the timer - this checks for any restrictions, so can be conveniently called from anywhere
        // without worry for any timing restrictions (eg: Paused, Enabled, etc)
        if (this.Paused || !this.Enabled || !this.IsRunning)
            return;
        
        this.Ticks++;
    }
}
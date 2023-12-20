using System.Runtime.InteropServices;
using System.Text.Json;

namespace SurfTimer;

internal class PlayerTimer
{
    // Status
    public bool Enabled { get; set; } = true; // Enable toggle for entire timer
    public bool Paused { get; set; } = false; // Pause toggle for timer
    public bool IsRunning { get; set; } = false; // Is the timer currently running?

    // Modes
    public bool PracticeMode { get; set; } = false; // Practice mode toggle
    public bool StageMode { get; set; } = false; // Stage mode toggle

    // Tracking
    public int Stage {get; set;} = 0; // Current stage tracker
    public int Checkpoint {get; set;} = 0; // Current checkpoint tracker
    public List<JsonElement> CurrentRunCheckpoints { get; set; } = new List<JsonElement>(); // Current run cps list
    public int Bonus {get; set;} = 0; // Current bonus tracker - To-do: bonus implementation
    // public int Style = 0; // To-do: style implementation

    // Timing
    public int Ticks { get; set; } = 0; // To-do: sub-tick counting? This currently goes on OnTick, which is not sub-tick I believe? Needs investigating

    // Time Formatting
    public enum TimeFormatStyle
    {
        Compact,
        Full,
        Verbose
    }

    // Methods
    public void Reset()
    {
        this.Stop();
        this.Ticks = 0;
        this.Stage = 0;
        this.Checkpoint = 0;
        this.Paused = false;
        this.PracticeMode = false;
        this.CurrentRunCheckpoints.Clear();
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
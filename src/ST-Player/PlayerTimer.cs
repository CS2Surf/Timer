namespace SurfTimer;

public class PlayerTimer
{
    // Status
    public bool IsEnabled { get; set; } = true; // Enable toggle for entire timer
    public bool IsPaused { get; set; } = false; // Pause toggle for timer
    public bool IsRunning { get; set; } = false; // Is the timer currently running?

    // Modes
    public bool IsPracticeMode { get; set; } = false; // Practice mode toggle
    public bool IsStageMode { get; set; } = false; // Stage mode toggle
    public bool IsBonusMode { get; set; } = false; // Bonus mode toggle

    // Tracking
    public short Stage { get; set; } = 0; // Current stage tracker
    public short Checkpoint { get; set; } = 0; // Current checkpoint tracker
    public short Bonus { get; set; } = 0; // To-do: bonus implementation - Current bonus tracker 
    public short Style { get; set; } = 0; // To-do: functionality for player to change this value and the actual styles implementation - Current style tracker

    // Timing
    public int Ticks { get; set; } = 0; // To-do: sub-tick counting? This currently goes on OnTick, which is not sub-tick I believe? Needs investigating

    /// <summary>
    /// Different types of time formatting for chat and HUD
    /// </summary>
    // Time Formatting - To-do: Move to player settings maybe?
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
        this.IsPaused = false;
        this.IsPracticeMode = false;
        this.IsStageMode = false;
        this.IsBonusMode = false;
    }

    public void Pause()
    {
        this.IsPaused = true;
    }

    public void Start()
    {
        // Timer Start method - notes: OnStartTimerPress
        if (this.IsEnabled)
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
        if (this.IsPaused || !this.IsEnabled || !this.IsRunning)
            return;

        this.Ticks++;
    }
}
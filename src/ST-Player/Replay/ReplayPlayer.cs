using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

internal class ReplayPlayer
{
    /// <summary>
    /// Enable or Disable the replay bots.
    /// </summary>
    public bool IsEnabled { get; set; } = Config.ReplaysEnabled;
    public bool IsPlaying { get; set; } = false;
    public bool IsPaused { get; set; } = false;
    public bool IsPlayable { get; set; } = false;

    // Tracking for replay counting
    public int RepeatCount { get; set; } = -1;

    public int MapID { get; set; } = -1;
    public int MapTimeID { get; set; } = -1;
    public int Type { get; set; } = -1;
    public int Stage { get; set; } = -1;

    public int RecordRank { get; set; } = -1; // This is used to determine whether replay is for wr or for pb
    public string RecordPlayerName { get; set; } = "N/A";
    public int RecordRunTime { get; set; } = -1;
    public int ReplayCurrentRunTime { get; set; } = 0;
    public bool IsReplayOutsideZone { get; set; } = false;

    // Tracking
    public List<ReplayFrame> Frames { get; set; } = new List<ReplayFrame>();
    public List<int> StageEnterSituations { get; set; } = new List<int>();
    public List<int> StageExitSituations { get; set; } = new List<int>();
    public List<int> CheckpointEnterSituations { get; set; } = new List<int>();
    public List<int> CheckpointExitSituations { get; set; } = new List<int>();
    /// <summary>
    /// Indexes should always follow this pattern: START_ZONE_ENTER > START_ZONE_EXIT > END_ZONE_ENTER > END_ZONE_EXIT
    /// Where END_ZONE_EXIT is not guaranteed
    /// </summary>
    public List<int> MapSituations { get; set; } = new List<int>();
    /// <summary>
    /// Indexes should always follow this pattern: START_ZONE_ENTER > START_ZONE_EXIT > END_ZONE_ENTER > END_ZONE_EXIT
    /// Where END_ZONE_EXIT is not guaranteed
    /// </summary>
    public List<int> BonusSituations { get; set; } = new List<int>();

    // Playing
    public int CurrentFrameTick { get; set; } = 0;
    public int FrameTickIncrement { get; set; } = 1;

    public CCSPlayerController? Controller { get; set; }

    public void ResetReplay()
    {
        this.CurrentFrameTick = 0;
        this.FrameTickIncrement = 1;
        if (this.RepeatCount > 0)
            this.RepeatCount--;

        this.IsReplayOutsideZone = false;
        this.ReplayCurrentRunTime = 0;
    }

    public void Reset()
    {
        this.IsPlaying = false;
        this.IsPaused = false;
        this.IsPlayable = false;
        this.RepeatCount = -1;

        this.Frames.Clear();

        this.ResetReplay();

        this.Controller = null;
    }

    public void SetController(CCSPlayerController c, int repeat_count = -1)
    {
        this.Controller = c;
        if (repeat_count != -1)
            this.RepeatCount = repeat_count;
        this.IsPlayable = true;

        // Console.WriteLine($"===== public void SetController -> Set controller for {c.PlayerName}");
    }

    public void Start()
    {
        if (!this.IsPlayable || !this.IsEnabled)
            return;

        this.IsPlaying = true;

        // Console.WriteLine($"CS2 Surf DEBUG >> internal class ReplayPlayer -> public void Start() -> Starting replay for run {this.MapTimeID} (Map ID {this.MapID}) - {this.RecordPlayerName} (Stage {this.Stage})");
    }

    public void Stop()
    {
        this.IsPlaying = false;

        // Console.WriteLine($"CS2 Surf DEBUG >> internal class ReplayPlayer -> public void Stop() -> Stopping replay for run {this.MapTimeID} (Map ID {this.MapID}) - {this.RecordPlayerName} (Stage {this.Stage})");
    }

    public void Pause()
    {
        if (!this.IsPlaying || !this.IsEnabled)
            return;

        this.IsPaused = !this.IsPaused;
        this.IsReplayOutsideZone = !this.IsReplayOutsideZone;

        // Console.WriteLine($"CS2 Surf DEBUG >> internal class ReplayPlayer -> public void Pause() -> Pausing replay for run {this.MapTimeID} (Map ID {this.MapID}) - {this.RecordPlayerName} (Stage {this.Stage})");
    }

    public void Tick()
    {
        if (this.MapID == -1 || !this.IsEnabled || !this.IsPlaying || !this.IsPlayable || this.Frames.Count == 0)
            return;

        ReplayFrame current_frame = this.Frames[this.CurrentFrameTick];

        // SOME BLASHPEMY FOR YOU
        if (this.FrameTickIncrement >= 0)
        {
            if (current_frame.Situation == ReplayFrameSituation.START_ZONE_EXIT)
            {
                this.IsReplayOutsideZone = true;
                this.ReplayCurrentRunTime = 0;
            }
            else if (current_frame.Situation == ReplayFrameSituation.END_ZONE_ENTER)
            {
                this.IsReplayOutsideZone = false;
            }
        }
        else
        {
            if (current_frame.Situation == ReplayFrameSituation.START_ZONE_EXIT)
            {
                this.IsReplayOutsideZone = false;
            }
            else if (current_frame.Situation == ReplayFrameSituation.END_ZONE_ENTER)
            {
                this.IsReplayOutsideZone = true;
                this.ReplayCurrentRunTime = this.CurrentFrameTick - (64 * 2); // (64*2) counts for the 2 seconds before run actually starts
            }
        }
        // END OF BLASPHEMY

        var current_pos = this.Controller!.PlayerPawn.Value!.AbsOrigin!;
        var current_frame_pos = current_frame.GetPos();
        var current_frame_ang = current_frame.GetAng();

        bool is_on_ground = (current_frame.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;

        Vector velocity = (current_frame_pos - current_pos) * 64;

        if (is_on_ground)
            this.Controller.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
        else
            this.Controller.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NOCLIP;

        if ((current_pos - current_frame_pos).Length() > 200)
            this.Controller.PlayerPawn.Value.Teleport(current_frame_pos, current_frame_ang, new Vector(nint.Zero));
        else
            this.Controller.PlayerPawn.Value.Teleport(new Vector(nint.Zero), current_frame_ang, velocity);


        if (!this.IsPaused)
        {
            this.CurrentFrameTick = Math.Max(0, this.CurrentFrameTick + this.FrameTickIncrement);
            if (this.IsReplayOutsideZone)
                this.ReplayCurrentRunTime = Math.Max(0, this.ReplayCurrentRunTime + this.FrameTickIncrement);
        }

        if (this.CurrentFrameTick >= this.Frames.Count)
            this.ResetReplay();
        // if(RepeatCount != -1)    // Spam City 
        //     Console.WriteLine($"CS2 Surf DEBUG >> internal class ReplayPlayer -> Tick -> ====================> {this.RepeatCount} <====================");
    }

    public void LoadReplayData(int repeat_count = -1)
    {
        if (!this.IsPlayable || !this.IsEnabled)
            return;

        // Console.WriteLine($"CS2 Surf DEBUG >> internal class ReplayPlayer -> [{(this.Type == 2 ? "Stage Replay" : this.Type == 1 ? "Bonus Replay" : "Map Replay")}] public void LoadReplayData -> We got MapID = {this.MapID}");

        if (this.MapID == -1)
        {
            Console.WriteLine($"CS2 Surf DEBUG >> internal class ReplayPlayer -> public void LoadReplayData -> [{(this.Type == 2 ? "Stage Replay" : this.Type == 1 ? "Bonus Replay" : "Map Replay")}] No replay data found for Player.");
            return;
        }

        // Console.WriteLine($"CS2 Surf DEBUG >> internal class ReplayPlayer -> public void LoadReplayData -> [{(this.Type == 2 ? "Stage Replay" : this.Type == 1 ? "Bonus Replay" : "Map Replay")}] Loaded replay data for Player '{this.RecordPlayerName}'. MapTime ID: {this.MapTimeID} | Repeat {repeat_count} | Frames {this.Frames.Count} | Ticks {this.RecordRunTime}");
        this.ResetReplay();
        this.RepeatCount = repeat_count;
    }

    public void FormatBotName()
    {
        if (!this.IsPlayable || !this.IsEnabled)
            return;

        string prefix;
        if (this.RecordRank == 1)
        {
            prefix = "WR";
        }
        else
        {
            prefix = $"Rank #{this.RecordRank}";
        }

        if (this.Type == 1)
            prefix = prefix + $" B{this.Stage}";
        else if (this.Type == 2)
            prefix = prefix + $" S{this.Stage}";

        SchemaString<CBasePlayerController> bot_name = new SchemaString<CBasePlayerController>(this.Controller!, "m_iszPlayerName");

        string replay_name = $"[{prefix}] {this.RecordPlayerName} | {PlayerHUD.FormatTime(this.RecordRunTime)}";
        if (this.RecordRunTime <= 0)
            replay_name = $"[{prefix}] {this.RecordPlayerName}";

        bot_name.Set(replay_name);
        Utilities.SetStateChanged(this.Controller!, "CBasePlayerController", "m_iszPlayerName");
    }
}
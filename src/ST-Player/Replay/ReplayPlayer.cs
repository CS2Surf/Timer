using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    private readonly ILogger<ReplayPlayer> _logger;

    // Constructor
    internal ReplayPlayer()
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<ReplayPlayer>>();
    }

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

    public void SetController(CCSPlayerController c, int repeat_count = -1, [CallerMemberName] string methodName = "")
    {
        this.Controller = c;
        if (repeat_count != -1)
            this.RepeatCount = repeat_count;
        this.IsPlayable = true;

        _logger.LogTrace("[{ClassName}] {MethodName} -> Set controller for {PlayerName}",
            nameof(ReplayPlayer), methodName, c.PlayerName
        );
    }

    public void Start([CallerMemberName] string methodName = "")
    {
        if (!this.IsPlayable || !this.IsEnabled)
            return;

        Server.NextFrame(() =>
    {
        this.FormatBotName();
        this.IsPlaying = true;

#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Starting replay for run {MapTimeID} (Map ID {MapID}) - {RecordPlayerName} (Stage {Stage})",
            nameof(ReplayPlayer), methodName, this.MapTimeID, this.MapID, this.RecordPlayerName, this.Stage
        );
#endif
    });
    }

    public void Stop([CallerMemberName] string methodName = "")
    {
        this.IsPlaying = false;
#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Stopping replay for run {MapTimeID} (Map ID {MapID}) - {RecordPlayerName} (Stage {Stage})",
            nameof(ReplayPlayer), methodName, this.MapTimeID, this.MapID, this.RecordPlayerName, this.Stage
        );
#endif
    }

    public void Pause([CallerMemberName] string methodName = "")
    {
        if (!this.IsPlaying || !this.IsEnabled)
            return;

        this.IsPaused = !this.IsPaused;
        this.IsReplayOutsideZone = !this.IsReplayOutsideZone;
#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Pausing replay for run {MapTimeID} (Map ID {MapID}) - {RecordPlayerName} (Stage {Stage})",
            nameof(ReplayPlayer), methodName, this.MapTimeID, this.MapID, this.RecordPlayerName, this.Stage
        );
#endif
    }

    public void Tick()
    {
        if (this.MapID == -1 || !this.IsEnabled || !this.IsPlaying || !this.IsPlayable || this.Frames.Count == 0)
            return;

        ReplayFrame current_frame = this.Frames[this.CurrentFrameTick];

        this.FormatBotName();

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

        var current_pos = Controller!.PlayerPawn.Value!.AbsOrigin!.ToVector_t();
        var current_frame_pos = current_frame.GetPos();
        var current_frame_ang = current_frame.GetAng();

        bool is_on_ground = (current_frame.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;

        Vector_t velocity = (current_frame_pos - current_pos) * 64;

        if (is_on_ground)
            this.Controller.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
        else
            this.Controller.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NOCLIP;

        if ((current_pos - current_frame_pos).Length() > 200)
            Extensions.Teleport(Controller.PlayerPawn.Value, current_frame_pos, current_frame_ang, null);
        else
            Extensions.Teleport(Controller.PlayerPawn.Value, null, current_frame_ang, velocity);


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

    public void LoadReplayData(int repeat_count = -1, [CallerMemberName] string methodName = "")
    {
        if (!this.IsPlayable || !this.IsEnabled)
            return;

        if (this.MapID == -1)
        {
            _logger.LogWarning("[{ClassName}] {MethodName} -> [{Type}] No replay data found for Player. MapID {MapID} | MapTimeID {MapTimeID} | RecordPlayerName {RecordPlayerName}",
                nameof(ReplayPlayer), methodName, (this.Type == 2 ? "Stage Replay" : this.Type == 1 ? "Bonus Replay" : this.Type == 0 ? "Map Replay" : "Unknown Type"), this.MapID, this.MapTimeID, RecordPlayerName
            );
            return;
        }

        _logger.LogTrace("[{ClassName}] {MethodName} -> [{Type}] Loaded replay data for Player '{RecordPlayerName}' | MapTime ID: {MapTimeID} | Repeat {Repeat} | Frames {TotalFrames} | Ticks {RecordTicks}",
            nameof(ReplayPlayer), methodName, (this.Type == 2 ? "Stage Replay" : this.Type == 1 ? "Bonus Replay" : this.Type == 0 ? "Map Replay" : "Unknown Type"), this.RecordPlayerName, this.MapTimeID, repeat_count, this.Frames.Count, this.RecordRunTime
        );

        this.ResetReplay();
        this.RepeatCount = repeat_count;
    }

    public void FormatBotName([CallerMemberName] string methodName = "")
    {
        if (!this.IsPlayable || !this.IsEnabled || this.MapID == -1)
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
            prefix += $"B {this.Stage}";
        else if (this.Type == 2)
            prefix += $"CP {this.Stage}";

        SchemaString<CBasePlayerController> bot_name = new SchemaString<CBasePlayerController>(this.Controller!, "m_iszPlayerName");

        string replay_name = $"[{prefix}] {this.RecordPlayerName} | {PlayerHUD.FormatTime(this.RecordRunTime)}";
        if (this.RecordRunTime <= 0)
            replay_name = $"[{prefix}] {this.RecordPlayerName}";

        bot_name.Set(replay_name);
        Server.NextFrame(() =>
            Utilities.SetStateChanged(this.Controller!, "CBasePlayerController", "m_iszPlayerName")
        );

        // _logger.LogTrace("[{ClassName}] {MethodName} -> Changed replay bot name from '{OldName}' to '{NewName}'",
        //     nameof(ReplayPlayer), methodName, bot_name, replay_name
        // );
    }
}
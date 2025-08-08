using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SurfTimer;

public class ReplayRecorder
{
    private readonly ILogger<ReplayRecorder> _logger;

    internal ReplayRecorder()
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<ReplayRecorder>>();
    }

    public bool IsRecording { get; set; } = false;
    public bool IsSaving { get; set; } = false;
    public ReplayFrameSituation CurrentSituation { get; set; } = ReplayFrameSituation.NONE;
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

    internal void Reset([CallerMemberName] string methodName = "")
    {
        this.IsRecording = false;
        this.Frames.Clear();
        this.StageEnterSituations.Clear();
        this.StageExitSituations.Clear();
        this.CheckpointEnterSituations.Clear();
        this.CheckpointExitSituations.Clear();
        this.MapSituations.Clear();
        this.BonusSituations.Clear();

#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Recording has been reset",
            nameof(ReplayRecorder), methodName
        );
#endif
    }

    internal void Start([CallerMemberName] string methodName = "")
    {
        this.IsRecording = true;

#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Recording has been started",
            nameof(ReplayRecorder), methodName
        );
#endif
    }

    internal void Stop([CallerMemberName] string methodName = "")
    {
        this.IsRecording = false;

#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Recording has been stopped",
            nameof(ReplayRecorder), methodName
        );
#endif
    }

    internal void Tick(Player player, [CallerMemberName] string methodName = "")
    {
        if (!this.IsRecording || player == null)
            return;

        // Disabling Recording if timer disabled
        // if (!player.Timer.IsEnabled)
        if (!player.Timer.IsEnabled && !player.ReplayRecorder.IsSaving)
        {
            this.Stop();
            this.Reset();
            _logger.LogTrace("[{ClassName}] {MethodName} -> Recording has stopped and reset for player {Name}",
                nameof(ReplayRecorder), methodName, player.Profile.Name
            );
            return;
        }

        var player_pos = player.Controller.Pawn.Value!.AbsOrigin!;
        var player_angle = player.Controller.PlayerPawn.Value!.EyeAngles;
        var player_button = player.Controller.Pawn.Value.MovementServices!.Buttons.ButtonStates[0];
        var player_flags = player.Controller.Pawn.Value.Flags;
        var player_move_type = player.Controller.Pawn.Value.MoveType;
        /*
                switch (this.CurrentSituation)
                {
                    case ReplayFrameSituation.START_ZONE_ENTER:
                        player.Controller.PrintToChat($"Start Enter: {this.Frames.Count}        | Situation {this.CurrentSituation}");
                        break;
                    case ReplayFrameSituation.START_ZONE_EXIT:
                        player.Controller.PrintToChat($"Start Exit: {this.Frames.Count}         | Situation {this.CurrentSituation}");
                        break;
                    case ReplayFrameSituation.STAGE_ZONE_ENTER:
                        player.Controller.PrintToChat($"Stage Enter: {this.Frames.Count}        | Situation {this.CurrentSituation}");
                        break;
                    case ReplayFrameSituation.STAGE_ZONE_EXIT:
                        player.Controller.PrintToChat($"Stage Exit: {this.Frames.Count}         | Situation {this.CurrentSituation}");
                        break;
                    case ReplayFrameSituation.CHECKPOINT_ZONE_ENTER:
                        player.Controller.PrintToChat($"Checkpoint Enter: {this.Frames.Count}   | Situation {this.CurrentSituation}");
                        break;
                    case ReplayFrameSituation.CHECKPOINT_ZONE_EXIT:
                        player.Controller.PrintToChat($"Checkpoint Exit: {this.Frames.Count}    | Situation {this.CurrentSituation}");
                        break;
                }
        */
        var frame = new ReplayFrame
        {
            pos = [player_pos.X, player_pos.Y, player_pos.Z],
            ang = [player_angle.X, player_angle.Y, player_angle.Z],
            Situation = this.CurrentSituation,
            Flags = player_flags,
        };

        this.Frames.Add(frame);

        // Every Situation should last for at most, 1 tick
        this.CurrentSituation = ReplayFrameSituation.NONE;
    }

    internal string SerializeReplay()
    {
        // JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() }};
        // string replay_frames = JsonSerializer.Serialize(Frames, options);
        string replay_frames = JsonSerializer.Serialize(Frames);
        return Compressor.Compress(replay_frames);
    }

    internal string SerializeReplayPortion(int start_idx, int end_idx) // Not used anymore
    {
        // JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() }};
        // string replay_frames = JsonSerializer.Serialize(Frames.GetRange(start_idx, end_idx), options);
        string replay_frames = JsonSerializer.Serialize(Frames.GetRange(start_idx, end_idx));
        return Compressor.Compress(replay_frames);
    }

    internal void SetLastTickSituation(ReplayFrameSituation situation)
    {
        if (this.Frames.Count == 0)
            return;

        this.Frames[this.Frames.Count - 2].Situation = situation;
    }

    internal string TrimReplay(Player player, int type = 0, bool lastStage = false, [CallerMemberName] string methodName = "")
    {
        this.IsSaving = true;

        List<ReplayFrame>? trimmed_frames = new List<ReplayFrame>();

        _logger.LogTrace(">>> [{ClassName}] {MethodName} -> Trimming replay for '{PlayerName}' | type = {type} | lastStage = {lastStage} ",
            nameof(ReplayRecorder), methodName, player.Profile.Name, type, lastStage
        );

        if (this.Frames.Count == 0)
        {
            _logger.LogError("[{ClassName}] {MethodName} -> There are no Frames available for replay trimming for player {Name}",
                 nameof(ReplayRecorder), methodName, player.Profile.Name
             );
            throw new Exception("There are no Frames available for trimming");
        }
        switch (type)
        {
            case 0: // Map Run
                {
                    trimmed_frames = TrimMapRun(player);
                    break;
                }
            case 1: // Bonus Run
                {
                    trimmed_frames = TrimBonusRun(player);
                    break;
                }
            case 2: // Stage Run
                {
                    trimmed_frames = TrimStageRun(player, lastStage);
                    break;
                }
        }

        this.IsSaving = false;
        _logger.LogTrace("[{ClassName}] {MethodName} -> Sending total of {Frames} replay frames.",
            nameof(CurrentRun), methodName, trimmed_frames?.Count
        );
        var trimmed = JsonSerializer.Serialize(trimmed_frames);
        return Compressor.Compress(trimmed);
    }

    internal List<ReplayFrame>? TrimMapRun(Player player, [CallerMemberName] string methodName = "")
    {
        List<ReplayFrame>? new_frames = new List<ReplayFrame>();

        var start_enter_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.START_ZONE_ENTER);
        var start_exit_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.START_ZONE_EXIT);
        var end_enter_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.END_ZONE_ENTER);

        _logger.LogInformation("[{ClassName}] {MethodName} -> Trimming Map Run replay. Last start enter {start_enter_index} | last start exit {start_exit_index} | end enter {end_enter_index}",
        nameof(ReplayRecorder), methodName, start_enter_index, start_exit_index, end_enter_index);

        if (start_enter_index == -1)
        {
            _logger.LogError("[{ClassName}] {MethodName} -> Player '{Name}' got '-1' for start_enter_index during Map replay trimming. Setting 'start_enter_index' to '0' | IsStageMode = {StageMode} | IsBonusMode = {BonusMode}",
                nameof(ReplayRecorder), methodName, player.Profile.Name, player.Timer.IsStageMode, player.Timer.IsBonusMode
            );
            start_enter_index = start_enter_index == -1 ? 0 : start_enter_index;
            // start_exit_index = start_exit_index == -1 ? 0 : start_exit_index;
        }

        if (start_enter_index != -1 && start_exit_index != -1 && end_enter_index != -1)
        {
            int startIndex = CalculateStartIndex(start_enter_index, start_exit_index, Config.ReplaysPre);
            int endIndex = CalculateEndIndex(end_enter_index, Frames.Count, Config.ReplaysPre);
            new_frames = GetTrimmedFrames(startIndex, endIndex);

            _logger.LogDebug("<<< [{ClassName}] {MethodName} -> Trimmed from {StartIndex} to {EndIndex} (new_frames = {NewFramesCount}) - from total {TotalFrames}",
            nameof(ReplayRecorder), methodName, startIndex, endIndex, new_frames.Count, this.Frames.Count);

            return new_frames;
        }
        else
        {
            _logger.LogError("[{ClassName}] {MethodName} -> Got a '-1' value while trimming Map replay for '{Name}'. start_enter_index = {start_enter_index} | start_exit_index = {start_exit_index} | end_enter_index = {end_enter_index}",
                nameof(ReplayRecorder), methodName, player.Profile.Name, start_enter_index, start_exit_index, end_enter_index
            );

            return new_frames;
        }
    }

    internal List<ReplayFrame>? TrimBonusRun(Player player, [CallerMemberName] string methodName = "")
    {
        List<ReplayFrame>? new_frames = new List<ReplayFrame>();

        var bonus_enter_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.START_ZONE_ENTER);
        var bonus_exit_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.START_ZONE_EXIT);
        var bonus_end_enter_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.END_ZONE_ENTER);
        _logger.LogInformation("[{ClassName}] {MethodName} -> Looking for Bonus Run replay trim indexes. Last start enter {bonus_enter_index}, last start exit {bonus_exit_index}, end enter {bonus_end_enter_index}",
            nameof(ReplayRecorder), methodName, bonus_enter_index, bonus_exit_index, bonus_end_enter_index
        );

        if (bonus_enter_index == -1)
        {
            _logger.LogError("[{ClassName}] {MethodName} -> Player '{Name}' got '-1' for bonus_enter_index during Bonus ({BonusNumber}) replay trimming. Settinng 'bonus_enter_index' to '0'",
                nameof(ReplayRecorder), methodName, player.Profile.Name, player.Timer.Bonus
            );
            bonus_enter_index = 0;
        }

        if (bonus_enter_index != -1 && bonus_exit_index != -1 && bonus_end_enter_index != -1)
        {
            int startIndex = CalculateStartIndex(bonus_enter_index, bonus_exit_index, Config.ReplaysPre);
            int endIndex = CalculateEndIndex(bonus_end_enter_index, Frames.Count, Config.ReplaysPre);
            new_frames = GetTrimmedFrames(startIndex, endIndex);

            _logger.LogDebug("<<< [{ClassName}] {MethodName} -> Trimmed Bonus replay from {startIndex} to {endIndex} ({new_frames}) - from total {OldFrames}",
                nameof(ReplayRecorder), methodName, startIndex, endIndex, new_frames.Count, this.Frames.Count
            );

            return new_frames;
        }
        else
        {
            _logger.LogError("[{ClassName}] {MethodName} -> Got a '-1' value while trimming Bonus ({BonusNumber}) replay for '{Name}'. bonus_enter_index = {bonus_enter_index} | bonus_exit_index = {bonus_exit_index} | bonus_end_enter_index = {bonus_end_enter_index}",
                nameof(ReplayRecorder), methodName, player.Timer.Bonus, player.Profile.Name, bonus_enter_index, bonus_exit_index, bonus_end_enter_index
            );

            return new_frames;
        }
    }

    internal List<ReplayFrame>? TrimStageRun(Player player, bool lastStage = false, [CallerMemberName] string methodName = "")
    {
        List<ReplayFrame>? new_frames = new List<ReplayFrame>();

        int stage_end_index;
        int stage_exit_index;
        int stage_enter_index;

        int stage = player.Timer.Stage - 1;

        ReplayFrameSituation enterZone;
        ReplayFrameSituation exitZone;
        ReplayFrameSituation endZone;

        // Select the correct enums for trimming
        if (stage == 1)
        {
            _logger.LogDebug("Stage replay trimming will use START_ZONE_*");
            enterZone = ReplayFrameSituation.START_ZONE_ENTER;
            exitZone = ReplayFrameSituation.START_ZONE_EXIT;
            endZone = ReplayFrameSituation.STAGE_ZONE_ENTER;
        }
        else
        {
            _logger.LogDebug("Stage replay trimming will use STAGE_ZONE_*");
            enterZone = ReplayFrameSituation.STAGE_ZONE_ENTER;
            exitZone = ReplayFrameSituation.STAGE_ZONE_EXIT;
            endZone = ReplayFrameSituation.STAGE_ZONE_ENTER;

            // If it's the last stage we need to use END_ZONE_ENTER for trimming
            if (lastStage)
            {
                _logger.LogDebug("This is the last stage, will end on END_ZONE_ENTER.");
                endZone = ReplayFrameSituation.END_ZONE_ENTER;
                stage += 1;
            }
        }

        _logger.LogInformation("[{ClassName}] {MethodName} -> Player is on Stage {Stage} and we are trimming replay for Stage {TrimmingStage}",
            nameof(ReplayRecorder), methodName, player.Timer.Stage, stage
        );

        stage_end_index = Frames.FindLastIndex(f => f.Situation == endZone);
        stage_exit_index = Frames.FindLastIndex(stage_end_index - 1, f => f.Situation == exitZone);
        stage_enter_index = Frames.FindLastIndex(stage_end_index - 1, f => f.Situation == enterZone);

        _logger.LogInformation("[{ClassName}] {MethodName} -> Trimming Stage Run replay. Stage {Stage}, enter {EnterIndex}, exit {ExitIndex}, end {EndIndex}",
            nameof(ReplayRecorder), methodName, stage, stage_enter_index, stage_exit_index, stage_end_index
        );

        if (stage_enter_index == -1 || stage_exit_index == -1 || stage_end_index == -1)
        {
            _logger.LogError("[{ClassName}] {MethodName} -> Could not find necessary frame indexes for trimming Stage {Stage} replay for player '{Name}'. ENTER: {Enter}, EXIT: {Exit}, END: {End}",
                nameof(ReplayRecorder), methodName, stage, player.Profile.Name,
                stage_enter_index, stage_exit_index, stage_end_index
            );
            return new_frames;
        }

        int startIndex = CalculateStartIndex(stage_enter_index, stage_exit_index, Config.ReplaysPre);
        int endIndex = CalculateEndIndex(stage_end_index, Frames.Count, Config.ReplaysPre);

        new_frames = GetTrimmedFrames(startIndex, endIndex);

        _logger.LogInformation("<<< [{ClassName}] {MethodName} -> Trimmed Stage {Stage} replay from {Start} to {End} (Total Frames: {NewFrames})",
            nameof(ReplayRecorder), methodName, stage, startIndex, endIndex, new_frames.Count
        );

        return new_frames;
    }

    internal int LastEnterTick(int start_idx = 0)
    {
        if (start_idx == 0)
            start_idx = this.Frames.Count - 1;
        for (int i = start_idx; i > 0; i--)
        {
            if (
                this.Frames[i].Situation == ReplayFrameSituation.START_ZONE_ENTER ||
                this.Frames[i].Situation == ReplayFrameSituation.STAGE_ZONE_ENTER ||
                this.Frames[i].Situation == ReplayFrameSituation.CHECKPOINT_ZONE_ENTER ||
                this.Frames[i].Situation == ReplayFrameSituation.END_ZONE_ENTER
            )
                return i;
        }
        return 0;
    }

    internal int LastExitTick(int start_idx = 0)
    {
        if (start_idx == 0)
            start_idx = this.Frames.Count - 1;
        for (int i = start_idx; i > 0; i--)
        {
            if (
                this.Frames[i].Situation == ReplayFrameSituation.START_ZONE_EXIT ||
                this.Frames[i].Situation == ReplayFrameSituation.STAGE_ZONE_EXIT ||
                this.Frames[i].Situation == ReplayFrameSituation.CHECKPOINT_ZONE_EXIT ||
                this.Frames[i].Situation == ReplayFrameSituation.END_ZONE_EXIT
            )
                return i;
        }
        return 0;
    }

    private int CalculateStartIndex(int start_enter, int start_exit, int buffer)
    {
        if (start_exit - (buffer * 2) >= start_enter)
            return start_exit - (buffer * 2);
        else if (start_exit - buffer >= start_enter)
            return start_exit - buffer;
        else if (start_exit - (buffer / 2) >= start_enter)
            return start_exit - (buffer / 2);
        else
            return start_enter;
    }

    private int CalculateEndIndex(int end_enter, int totalFrames, int buffer)
    {
        if (end_enter + (buffer * 2) < totalFrames)
        {
            // _logger.LogTrace("CalculateEndIndex 1st -> end_enter + (buffer * 2) < totalFrames ({Logic}) -> {Value}", end_enter + (buffer * 2) < totalFrames, end_enter + (buffer * 2));
            return end_enter + (buffer * 2);
        }
        else if (end_enter + buffer < totalFrames)
        {
            // _logger.LogTrace("CalculateEndIndex 2nd -> end_enter + buffer < totalFrames ({Logic}) -> {Value}", end_enter + buffer < totalFrames, end_enter + buffer);
            return end_enter + buffer;
        }
        else if (end_enter + (buffer / 2) < totalFrames)
        {
            // _logger.LogTrace("CalculateEndIndex 3rd -> end_enter + (buffer / 2) < totalFrames ({Logic}) -> {Value}", end_enter + (buffer / 2) < totalFrames, end_enter + (buffer / 2));
            return end_enter + (buffer / 2);
        }
        else
        {
            // _logger.LogTrace("CalculateEndIndex 4th -> Last -> {Value}", end_enter);
            return end_enter;
        }
    }

    private List<ReplayFrame> GetTrimmedFrames(int startIndex, int endIndex)
    {
        return Frames.GetRange(startIndex, endIndex - startIndex + 1);
    }
}
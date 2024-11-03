using System.Text.Json;
using CounterStrikeSharp.API.Modules.Utils;
namespace SurfTimer;

internal class ReplayRecorder
{
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

    public void Reset()
    {
        this.IsRecording = false;
        this.Frames.Clear();
        this.StageEnterSituations.Clear();
        this.StageExitSituations.Clear();
        this.CheckpointEnterSituations.Clear();
        this.CheckpointExitSituations.Clear();
        this.MapSituations.Clear();
        this.BonusSituations.Clear();

        Console.WriteLine($"===== ReplayRecorder -> Reset() -> Recording has been reset");
    }

    public void Start()
    {
        this.IsRecording = true;
    }

    public void Stop()
    {
        this.IsRecording = false;
    }

    public void Tick(Player player)
    {
        if (!this.IsRecording || player == null)
            return;

        // Disabling Recording if timer disabled
        if (!player.Timer.IsEnabled)
        {
            this.Stop();
            this.Reset();
            Console.WriteLine($"===== ReplayRecorder -> Tick() -> Recording has stopped and reset");
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

    public string SerializeReplay()
    {
        // JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() }};
        // string replay_frames = JsonSerializer.Serialize(Frames, options);
        string replay_frames = JsonSerializer.Serialize(Frames);
        return Compressor.Compress(replay_frames);
    }

    public string SerializeReplayPortion(int start_idx, int end_idx) // Not used anymore
    {
        // JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() }};
        // string replay_frames = JsonSerializer.Serialize(Frames.GetRange(start_idx, end_idx), options);
        string replay_frames = JsonSerializer.Serialize(Frames.GetRange(start_idx, end_idx));
        return Compressor.Compress(replay_frames);
    }

    public void SetLastTickSituation(ReplayFrameSituation situation)
    {
        if (this.Frames.Count == 0)
            return;

        this.Frames[this.Frames.Count - 2].Situation = situation;
    }

    public string TrimReplay(Player player, int type = 0, bool lastStage = false)
    {
        this.IsSaving = true;

        List<ReplayFrame> new_frames = new List<ReplayFrame>();

        if (this.Frames.Count == 0)
        {
            Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> There are no Frames for trimming");
            throw new Exception("There are no Frames available for trimming");
        }
            switch (type)
            {
                case 0: // Trim Map replays
                        // Map/Bonus runs
                    var start_enter_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.START_ZONE_ENTER);
                    var start_exit_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.START_ZONE_EXIT);
                    var end_enter_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.END_ZONE_ENTER);

                    Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> Trimming Map Run replay. Last start enter {start_enter_index}, last start exit {start_exit_index}, end enter {end_enter_index}");

                    if (start_enter_index != -1 && start_exit_index != -1 && end_enter_index != -1)
                    {
                        // Try different buffer sizes for start index
                        int startIndex;
                        if (start_exit_index - (Config.ReplaysPre * 2) >= start_enter_index)
                            startIndex = start_exit_index - (Config.ReplaysPre * 2);
                        else if (start_exit_index - Config.ReplaysPre >= start_enter_index)
                            startIndex = start_exit_index - Config.ReplaysPre;
                        else if (start_exit_index - (Config.ReplaysPre / 2) >= start_enter_index)
                            startIndex = start_exit_index - (Config.ReplaysPre / 2);
                        else
                            startIndex = start_enter_index;  // fallback to minimum allowed

                        // Try different buffer sizes for end index
                        int endIndex;
                        if (end_enter_index + (Config.ReplaysPre * 2) < Frames.Count)
                            endIndex = end_enter_index + (Config.ReplaysPre * 2);
                        else if (end_enter_index + Config.ReplaysPre < Frames.Count)
                            endIndex = end_enter_index + Config.ReplaysPre;
                        else if (end_enter_index + (Config.ReplaysPre / 2) < Frames.Count)
                            endIndex = end_enter_index + (Config.ReplaysPre / 2);
                        else
                            // endIndex = Frames.Count - 1;  // fallback to maximum allowed
                            endIndex = end_enter_index;  // fallback to maximum allowed

                        // Get the range of frames
                        new_frames = Frames.GetRange(startIndex, endIndex - startIndex + 1);
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> Trimmed from {startIndex} to {endIndex} ({new_frames.Count}) - from total {this.Frames.Count}");
                    }
                    break;
                case 1: // Trim Bonus replays
                    break;
                case 2: // Trim Stage replays
                    int stage_end_index;
                    int stage_exit_index;
                    int stage_enter_index;

                    Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> Will trim Stage Run replay. Stage {player.Timer.Stage - 1}, available frames {Frames.Count}");

                    // Stage runs
                    if (lastStage) // Last stage
                    {
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> Stage replay trimming will use `STAGE_ZONE_X` + `END_ZONE_ENTER`");
                        stage_end_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.END_ZONE_ENTER); // Last stage enter (finishing the stage)
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_end_index = {stage_end_index}");
                        stage_exit_index = Frames.FindLastIndex(stage_end_index - 1, f => f.Situation == ReplayFrameSituation.STAGE_ZONE_EXIT); // Exiting the previous stage zone (what we are looking for start of the stage run)    
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_exit_index = {stage_exit_index}");
                        stage_enter_index = Frames.FindLastIndex(stage_end_index - 1, f => f.Situation == ReplayFrameSituation.STAGE_ZONE_ENTER); // Entering the previous stage zone (what we are looking for pre-speed trim)
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_enter_index = {stage_enter_index}");
                    }
                    else if (player.Timer.Stage - 1 > 1) // Not first stage
                    {
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> Stage replay trimming will use `STAGE_ZONE_X`");
                        stage_end_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.STAGE_ZONE_ENTER); // Last stage enter (finishing the stage)
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_end_index = {stage_end_index}");
                        stage_exit_index = Frames.FindLastIndex(stage_end_index - 1, f => f.Situation == ReplayFrameSituation.STAGE_ZONE_EXIT); // Exiting the previous stage zone (what we are looking for start of the stage run)    
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_exit_index = {stage_exit_index}");
                        stage_enter_index = Frames.FindLastIndex(stage_end_index - 1, f => f.Situation == ReplayFrameSituation.STAGE_ZONE_ENTER); // Entering the previous stage zone (what we are looking for pre-speed trim)
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_enter_index = {stage_enter_index}");
                    }
                    else // First stage is always the start of the map so we are looking for START_ZONE_X
                    {
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> Stage replay trimming will use `START_ZONE_X`");
                        stage_end_index = Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.STAGE_ZONE_ENTER); // Last stage enter (finishing the stage)
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_end_index = {stage_end_index}");
                        stage_exit_index = Frames.FindLastIndex(stage_end_index - 1, f => f.Situation == ReplayFrameSituation.START_ZONE_EXIT); // Exiting the previous stage zone (what we are looking for start of the stage run)    
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_exit_index = {stage_exit_index}");
                        stage_enter_index = Frames.FindLastIndex(stage_end_index - 1, f => f.Situation == ReplayFrameSituation.START_ZONE_ENTER); // Entering the previous stage zone (what we are looking for pre-speed trim)
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> stage_enter_index = {stage_enter_index}");
                    }

                    Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> Trimming Stage Run replay. Stage {player.Timer.Stage - 1} enter {stage_enter_index}, stage exit {stage_exit_index}, stage end {stage_end_index}");

                    if (stage_enter_index != -1 && stage_exit_index != -1 && stage_end_index != -1)
                    {
                        // Try different buffer sizes for start index
                        int startIndex;
                        if (stage_exit_index - (Config.ReplaysPre * 2) >= stage_enter_index)
                            startIndex = stage_exit_index - (Config.ReplaysPre * 2);
                        else if (stage_exit_index - Config.ReplaysPre >= stage_enter_index)
                            startIndex = stage_exit_index - Config.ReplaysPre;
                        else if (stage_exit_index - (Config.ReplaysPre / 2) >= stage_enter_index)
                            startIndex = stage_exit_index - (Config.ReplaysPre / 2);
                        else
                            startIndex = stage_enter_index;  // fallback to minimum allowed

                        // Try different buffer sizes for end index
                        int endIndex;
                        if (stage_end_index + (Config.ReplaysPre * 2) < Frames.Count)
                            endIndex = stage_end_index + (Config.ReplaysPre * 2);
                        else if (stage_end_index + Config.ReplaysPre < Frames.Count)
                            endIndex = stage_end_index + Config.ReplaysPre;
                        else if (stage_end_index + (Config.ReplaysPre / 2) < Frames.Count)
                            endIndex = stage_end_index + (Config.ReplaysPre / 2);
                        else
                            // endIndex = Frames.Count - 1;  // fallback to maximum allowed
                            endIndex = stage_end_index;  // fallback to maximum allowed

                        // Get the range of frames
                        new_frames = Frames.GetRange(startIndex, endIndex - startIndex + 1);
                        Console.WriteLine($"======== internal class ReplayRecorder -> public string TrimReplay -> Trimmed Stage replay from {startIndex} to {endIndex} ({new_frames.Count}) - from total {this.Frames.Count}");
                    }
                    break;
            }

        this.IsSaving = false;
        string trimmed = JsonSerializer.Serialize(new_frames);
        return Compressor.Compress(trimmed);
    }

    public int LastEnterTick(int start_idx = 0)
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

    public int LastExitTick(int start_idx = 0)
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
}
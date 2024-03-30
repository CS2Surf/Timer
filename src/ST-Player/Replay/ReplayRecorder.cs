using System.Text.Json;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

internal class ReplayRecorder
{
    public bool IsRecording { get; set; } = false;
    public ReplayFrameSituation CurrentSituation { get; set; } = ReplayFrameSituation.NONE;
    public List<ReplayFrame> Frames { get; set; } = new List<ReplayFrame>();

    public void Reset() 
    {
        this.IsRecording = false;
        this.Frames.Clear();
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

        // Disabeling Recording if timer disabled
        if (!player.Timer.IsEnabled) 
        {
            this.Stop();
            this.Reset();
            return;
        }

        var player_pos = player.Controller.Pawn.Value!.AbsOrigin!;
        var player_angle = player.Controller.PlayerPawn.Value!.EyeAngles;
        var player_button = player.Controller.Pawn.Value.MovementServices!.Buttons.ButtonStates[0];
        var player_flags = player.Controller.Pawn.Value.Flags;
        var player_move_type = player.Controller.Pawn.Value.MoveType;

        var frame = new ReplayFrame 
        {
            pos = [player_pos.X, player_pos.Y, player_pos.Z],
            ang = [player_angle.X, player_angle.Y, player_angle.Z],
            Situation = (byte)this.CurrentSituation,
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

    public string SerializeReplayPortion(int start_idx, int end_idx)
    {
        // JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() }};
        // string replay_frames = JsonSerializer.Serialize(Frames.GetRange(start_idx, end_idx), options);
        string replay_frames = JsonSerializer.Serialize(Frames.GetRange(start_idx, end_idx));
        return Compressor.Compress(replay_frames);
    }

    public int CalculateTicksFromLastSituation(int idx = -1)
    {
        if (idx == -1)
            idx = this.Frames.Count-1;
        for (int i = idx; i > 0; i--)
            if (
                this.Frames[i].Situation == (byte)ReplayFrameSituation.START_RUN || 
                this.Frames[i].Situation == (byte)ReplayFrameSituation.START_STAGE || 
                this.Frames[i].Situation == (byte)ReplayFrameSituation.END_RUN || 
                this.Frames[i].Situation == (byte)ReplayFrameSituation.END_STAGE)
                return i; // Fact check me
        return 0;
    }
}
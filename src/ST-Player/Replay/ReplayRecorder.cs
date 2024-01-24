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
            Pos = new Vector(player_pos.X, player_pos.Y, player_pos.Z),
            Ang = new QAngle(player_angle.X, player_angle.Y, player_angle.Z),
            Situation = (uint)this.CurrentSituation,
            Button = player_button,
            Flags = player_flags,
            MoveType = player_move_type,
        };

        this.Frames.Add(frame);

        // Every Situation should last for at most, 1 tick
        this.CurrentSituation = ReplayFrameSituation.NONE;
    }

    public string SerializeReplay()
    {
        JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() }};
        string replay_frames = JsonSerializer.Serialize(Frames, options);
        return Compressor.Compress(replay_frames);
    }
}
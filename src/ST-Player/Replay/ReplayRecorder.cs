using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

internal class ReplayRecorder
{
    public bool IsRecording { get; set; } = false;
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
            Button = player_button,
            Flags = player_flags,
            MoveType = player_move_type,
        };

        this.Frames.Add(frame);
    }

    /// <summary>
    /// [ player_id | maptime_id | replay_frames ]
    /// @ Adding a replay data for a run (PB/WR)
    /// @ Data saved can be accessed with `ReplayPlayer.LoadReplayData`
    /// </summary>
    public void SaveReplayData(Player player, TimerDatabase DB) 
    {
        JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() }};
        string replay_frames = JsonSerializer.Serialize(Frames, options);
        string compressed_replay_frames = Compressor.Compress(replay_frames);
        Task<int> updatePlayerReplayTask = DB.Write($@"
            INSERT INTO `MapTimeReplay` 
            (`player_id`, `maptime_id`, `map_id`, `replay_frames`) 
            VALUES ({player.Profile.ID}, {player.Stats.PB[0].ID}, {player.CurrMap.ID}, '{compressed_replay_frames}') 
            ON DUPLICATE KEY UPDATE replay_frames=VALUES(replay_frames)
        ");
        if (updatePlayerReplayTask.Result <= 0)
            throw new Exception($"CS2 Surf ERROR >> internal class PlayerReplay -> SaveReplayData -> Failed to insert/update player run in database. Player: {player.Profile.Name} ({player.Profile.SteamID})");
        updatePlayerReplayTask.Dispose();
    }  
}
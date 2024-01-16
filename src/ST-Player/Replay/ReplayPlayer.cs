using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;

namespace SurfTimer;

internal class ReplayPlayer
{
    public bool IsPlaying { get; set; } = false;
    public bool IsPaused { get; set; } = false;
    public bool IsOnRepeat { get; set; } = true; // Currently should always repeat

    // Tracking
    public List<ReplayFrame> Frames { get; set; } = new List<ReplayFrame>();

    // Playing
    public int CurrentFrameTick { get; set; } = 0;
    public int FrameTickIncrement { get; set; } = 1;

    public CCSPlayerController? Controller { get; set; }

    public void ResetReplay() 
    {
        this.CurrentFrameTick = 0;
        this.FrameTickIncrement = 1;
    }

    public void Reset() 
    {
        this.IsPlaying = false;
        this.IsPaused = false;

        this.Frames.Clear();

        this.ResetReplay();

        this.Controller = null;
    }

    public void Start() 
    {
        if (this.Controller == null)
            return;

        this.IsPlaying = true;
        this.Controller.Pawn.Value!.MoveType = MoveType_t.MOVETYPE_NOCLIP;
    }

    public void Stop() 
    {
        this.IsPlaying = false;
    }

    public void Pause() 
    {
        if (this.IsPlaying)
            this.IsPaused = !this.IsPaused;
    }

    public void Tick() 
    {
        if (!this.IsPlaying || this.Controller == null || this.Frames.Count == 0)
            return;

        ReplayFrame current_frame = this.Frames[this.CurrentFrameTick];
        var current_pos = this.Controller.PlayerPawn.Value!.AbsOrigin!;

        bool is_on_ground = (current_frame.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;
        bool is_ducking = (current_frame.Flags & (uint)PlayerFlags.FL_DUCKING) != 0;

        Vector velocity = (current_frame.Pos - current_pos) * 64;

        if (is_on_ground)
            this.Controller.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
        else
            this.Controller.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NOCLIP;

        if ((current_pos - current_frame.Pos).Length() > 200)
                this.Controller.PlayerPawn.Value.Teleport(current_frame.Pos, current_frame.Ang, new Vector(nint.Zero));
            else
                this.Controller.PlayerPawn.Value.Teleport(new Vector(nint.Zero), current_frame.Ang, velocity);
                

        if (!this.IsPaused)
            this.CurrentFrameTick = Math.Max(0, this.CurrentFrameTick + this.FrameTickIncrement);

        if(this.CurrentFrameTick >= this.Frames.Count) 
            this.ResetReplay();
    }

    public void LoadReplayData(TimerDatabase DB, Map current_map) 
    {
        if (this.Controller == null)
            return;
        // TODO: make query for wr too
        Task<MySqlDataReader> dbTask = DB.Query($"SELECT `replay_frames` FROM MapTimeReplay " +
                                                $"WHERE `map_id`={current_map.ID} AND `maptime_id`={current_map.WR[0].ID} ");
        MySqlDataReader mapTimeReplay = dbTask.Result;
        if(!mapTimeReplay.HasRows) 
        {
            Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerReplay -> Load -> No replay data found for Player.");
        }
        else 
        {
            JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() }};
            while(mapTimeReplay.Read()) 
            {
                string json = Compressor.Decompress(Encoding.UTF8.GetString((byte[])mapTimeReplay[0]));
                this.Frames = JsonSerializer.Deserialize<List<ReplayFrame>>(json, options)!;
            }
        }
        mapTimeReplay.Close();
        dbTask.Dispose();

        FormatBotName(current_map);
    }

    private void FormatBotName(Map current_map)
    {
        if (this.Controller == null)
            return;

        SchemaString<CBasePlayerController> bot_name = new SchemaString<CBasePlayerController>(this.Controller, "m_iszPlayerName");
        // Revisit, FORMAT CORECTLLY
        bot_name.Set($"[WR] {PlayerHUD.FormatTime(current_map.WR[0].Ticks)}");
        Utilities.SetStateChanged(this.Controller, "CBasePlayerController", "m_iszPlayerName");
    }
}
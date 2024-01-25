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
    public bool IsPlayable { get; set; } = false;

    // Tracking for replay counting
    public int RepeatCount { get; set; } = -1;

    // Stats for replay displaying
    public string Stat_Prefix { get; set; } = "WR";
    public string Stat_PlayerName { get; set; } = "N/A";
    public int Stat_MapTimeID { get; set; } = -1;
    public int Stat_RunTime { get; set; } = 0;
    public bool Stat_IsRunning { get; set; } = false;
    public int Stat_RunTick { get; set; } = 0;

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
        if(this.RepeatCount > 0)
            this.RepeatCount--;

        this.Stat_IsRunning = false;
        this.Stat_RunTick = 0;
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
        this.RepeatCount = repeat_count;
        this.IsPlayable = true;
    }

    public void Start() 
    {
        if (!this.IsPlayable)
            return;

        this.IsPlaying = true;
    }

    public void Stop() 
    {
        this.IsPlaying = false;
    }

    public void Pause() 
    {
        if (!this.IsPlaying)
            return;

        this.IsPaused = !this.IsPaused;
        this.Stat_IsRunning = !this.Stat_IsRunning;
    }

    public void Tick() 
    {
        if (!this.IsPlaying || !this.IsPlayable || this.Frames.Count == 0)
            return;

        ReplayFrame current_frame = this.Frames[this.CurrentFrameTick];

        // SOME BLASHPEMY FOR YOU
        if (this.FrameTickIncrement >= 0)
        {
            if (current_frame.Situation == (uint)ReplayFrameSituation.START_RUN)
            {
                this.Stat_IsRunning = true;
                this.Stat_RunTick = 0;
            }
            else if (current_frame.Situation == (uint)ReplayFrameSituation.END_RUN)
            {
                this.Stat_IsRunning = false;
            }
        }
        else
        {
            if (current_frame.Situation == (uint)ReplayFrameSituation.START_RUN)
            {
                this.Stat_IsRunning = false;
            }
            else if (current_frame.Situation == (uint)ReplayFrameSituation.END_RUN)
            {
                this.Stat_IsRunning = true;
                this.Stat_RunTick = this.CurrentFrameTick - (64*2); // (64*2) counts for the 2 seconds before run actually starts
            }
        }
        // END OF BLASPHEMY

        var current_pos = this.Controller!.PlayerPawn.Value!.AbsOrigin!;

        bool is_on_ground = (current_frame.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;

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
        {
            this.CurrentFrameTick = Math.Max(0, this.CurrentFrameTick + this.FrameTickIncrement);
            if (this.Stat_IsRunning)
                this.Stat_RunTick = Math.Max(0, this.Stat_RunTick + this.FrameTickIncrement);
        }

        if(this.CurrentFrameTick >= this.Frames.Count) 
            this.ResetReplay();
    }

    public void LoadReplayData(TimerDatabase DB) 
    {
        if (!this.IsPlayable)
            return;

        Task<MySqlDataReader> dbTask = DB.Query($@"
            SELECT MapTimes.replay_frames, MapTimes.run_time, Player.name
            FROM MapTimes
            JOIN Player ON MapTimes.player_id = Player.id
            WHERE MapTimes.id={this.Stat_MapTimeID}
        ");

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

                this.Stat_RunTime = mapTimeReplay.GetInt32("run_time");
                this.Stat_PlayerName = mapTimeReplay.GetString("name");
            }
            FormatBotName();
        }
        mapTimeReplay.Close();
        dbTask.Dispose();
    }

    private void FormatBotName()
    {
        if (!this.IsPlayable)
            return;

        SchemaString<CBasePlayerController> bot_name = new SchemaString<CBasePlayerController>(this.Controller!, "m_iszPlayerName");

        string replay_name = $"[{this.Stat_Prefix}] {this.Stat_PlayerName} | {PlayerHUD.FormatTime(this.Stat_RunTime)}";
        if(this.Stat_RunTime <= 0)
            replay_name = $"[{this.Stat_Prefix}] {this.Stat_PlayerName}";

        bot_name.Set(replay_name);
        Utilities.SetStateChanged(this.Controller!, "CBasePlayerController", "m_iszPlayerName");
    }
}
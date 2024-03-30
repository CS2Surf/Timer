using System.Dynamic;
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

    public int MapID { get; set; } = -1;
    public int MapTimeID { get; set; } = -1;
    public int Type { get; set; } = -1;
    public int Stage { get; set; } = -1;

    public int RecordRank { get; set; } = -1; // This is used to determine whether replay is for wr or for pb
    public string RecordPlayerName { get; set; } = "N/A";
    public int RecordRunTime { get; set; } = 0;
    public int ReplayCurrentRunTime { get; set; } = 0;
    public bool IsReplayOutsideZone { get; set; } = false;

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
        this.IsReplayOutsideZone = !this.IsReplayOutsideZone;
    }

    public void Tick() 
    {
        if (!this.IsPlaying || !this.IsPlayable || this.Frames.Count == 0)
            return;

        ReplayFrame current_frame = this.Frames[this.CurrentFrameTick];

        // SOME BLASHPEMY FOR YOU
        if (this.FrameTickIncrement >= 0)
        {
            if (current_frame.Situation == (byte)ReplayFrameSituation.START_RUN)
            {
                this.IsReplayOutsideZone = true;
                this.ReplayCurrentRunTime = 0;
            }
            else if (current_frame.Situation == (byte)ReplayFrameSituation.END_RUN)
            {
                this.IsReplayOutsideZone = false;
            }
        }
        else
        {
            if (current_frame.Situation == (byte)ReplayFrameSituation.START_RUN)
            {
                this.IsReplayOutsideZone = false;
            }
            else if (current_frame.Situation == (byte)ReplayFrameSituation.END_RUN)
            {
                this.IsReplayOutsideZone = true;
                this.ReplayCurrentRunTime = this.CurrentFrameTick - (64*2); // (64*2) counts for the 2 seconds before run actually starts
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

        if(this.CurrentFrameTick >= this.Frames.Count) 
            this.ResetReplay();
    }

    public void LoadReplayData(TimerDatabase DB) 
    {
        if (!this.IsPlayable)
            return;

        // Some SQL queries
        string base_query = $@"
            WITH LoadReplayByRank AS (
                SELECT MapTimes.replay_frames, MapTimes.id, MapTimes.run_time, Player.name,
                (SELECT COUNT(*) + 1 
                    FROM MapTimes AS MT 
                    WHERE MT.map_id=MapTimes.map_id
                        AND MT.type=MapTimes.type
                        AND MT.stage=MapTimes.stage
                        AND MT.run_time<MapTimes.run_time) AS run_rank
                FROM MapTimes
                JOIN Player ON MapTimes.player_id=Player.id
                WHERE MapTimes.map_id = {this.MapID}
                    AND MapTimes.type = {this.Type}
                    AND MapTimes.stage = {this.Stage}
            )
        ";

        string query_by_rank = $@"
            {base_query}
            SELECT * FROM `LoadReplayByRank` WHERE `run_rank` = {this.RecordRank};
        ";

        Task<MySqlDataReader> dbTask = DB.Query(query_by_rank);

        System.Console.WriteLine("=================================================");

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

                this.MapTimeID = mapTimeReplay.GetInt32("id");
                this.RecordRunTime = mapTimeReplay.GetInt32("run_time");
                this.RecordPlayerName = mapTimeReplay.GetString("name");
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

        string prefix;
        if (this.RecordRank == 1) {
            prefix = "WR";
        } else {
            prefix = $"Rank #{this.RecordRank}";
        }

        SchemaString<CBasePlayerController> bot_name = new SchemaString<CBasePlayerController>(this.Controller!, "m_iszPlayerName");

        string replay_name = $"[{prefix}] {this.RecordPlayerName} | {PlayerHUD.FormatTime(this.RecordRunTime)}";
        if(this.RecordRunTime <= 0)
            replay_name = $"[{prefix}] {this.RecordPlayerName}";

        bot_name.Set(replay_name);
        Utilities.SetStateChanged(this.Controller!, "CBasePlayerController", "m_iszPlayerName");
    }
}
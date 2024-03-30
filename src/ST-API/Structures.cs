namespace SurfTimer;

// Map Info structure
internal class API_PostResponseData
{
    public int inserted { get; set; }
    public float xtime { get; set; }
    public int? last_id { get; set; }
    public int? trx { get; set; }
}

internal class API_Checkpoint
{
    public int cp { get; set; }
    public int ticks { get; set; }
    public float start_vel_x { get; set; }
    public float start_vel_y { get; set; }
    public float start_vel_z { get; set; }
    public float end_vel_x { get; set; }
    public float end_vel_y { get; set; }
    public float end_vel_z { get; set; }
    public int end_touch { get; set; }
    public int attempts { get; set; }
}

internal class API_CurrentRun
{
    public int player_id { get; set; }
    public int map_id { get; set; }
    public int run_time { get; set; }
    public float start_vel_x { get; set; }
    public float start_vel_y { get; set; }
    public float start_vel_z { get; set; }
    public float end_vel_x { get; set; }
    public float end_vel_y { get; set; }
    public float end_vel_z { get; set; }
    public int style { get; set; } = 0;
    public int type { get; set; } = 0;
    public int stage { get; set; } = 0;
    public List<API_Checkpoint>? checkpoints { get; set; } = null;
    public string replay_frames { get; set; } = ""; // This needs to be checked touroughly
    public int? run_date { get; set; } = null;
}

internal class API_MapInfo
{
    public int id { get; set; } = 0;
    public string name { get; set; } = "N/A";
    public string author { get; set; } = "Unknown";
    public int tier { get; set; } = 0;
    public int stages { get; set; } = 0;
    public int bonuses { get; set; } = 0;
    public int ranked { get; set; } = 0;
    public int? date_added { get; set; } = null;
    public int? last_played { get; set; } = null;
}

internal class API_MapTime
{
    public int id { get; set; }
    public int player_id { get; set; }
    public int map_id { get; set; }
    public int style { get; set; } = 0;
    public int type { get; set; } = 0;
    public int stage { get; set; } = 0;
    public int run_time { get; set; }
    public float start_vel_x { get; set; }
    public float start_vel_y { get; set; }
    public float start_vel_z { get; set; }
    public float end_vel_x { get; set; }
    public float end_vel_y { get; set; }
    public float end_vel_z { get; set; }
    public int? run_date { get; set; } = null;
    public string replay_frames { get; set; } = ""; // This needs to be checked touroughly
    public string name { get; set; } = "N/A";
}

internal class API_PlayerSurfProfile
{
    public string name { get; set; } = "N/A";
    public int steam_id { get; set; }
    public string country { get; set; } = "N/A";
    public int join_date { get; set; }
    public int last_seen { get; set; }
    public int connections { get; set; }
}
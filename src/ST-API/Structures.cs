using System.Text.Json.Serialization;
using SurfTimer.Data;

namespace SurfTimer;

// Map Info structure
public class API_PostResponseData
{
    public int Id { get; set; }
    public int Inserted { get; set; }
    public bool Trx { get; set; }
}

public class API_Checkpoint
{
    public int cp { get; set; }
    public int run_time { get; set; }
    public float start_vel_x { get; set; }
    public float start_vel_y { get; set; }
    public float start_vel_z { get; set; }
    public float end_vel_x { get; set; }
    public float end_vel_y { get; set; }
    public float end_vel_z { get; set; }
    public int end_touch { get; set; }
    public int attempts { get; set; }
}

public class API_CurrentRun
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

public class API_MapInfo
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

    [JsonConstructor]
    /// <summary>
    /// Parameterless constructor for manual assigning of data
    /// </summary>
    public API_MapInfo() { }

    /// <summary>
    /// Assigns values to the needed data model for an API request
    /// </summary>
    public API_MapInfo(MapInfoDataModel data)
    {
        id = data.ID;
        name = data.Name;
        author = data.Author;
        tier = data.Tier;
        stages = data.Stages;
        bonuses = data.Bonuses;
        ranked = data.Ranked ? 1 : 0;
        date_added = data.DateAdded;
        last_played = data.LastPlayed;
    }
}

public class API_MapTime
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
    public int run_date { get; set; }
    public string replay_frames { get; set; } = ""; // This needs to be checked touroughly
    public List<API_Checkpoint>? checkpoints { get; set; } = null;
    public string name { get; set; } = "N/A";
    public int total_count { get; set; }
}

public class API_PlayerSurfProfile
{
    public int id { get; set; }
    public string name { get; set; } = "N/A";
    public ulong steam_id { get; set; }
    public string country { get; set; } = "N/A";
    public int join_date { get; set; }
    public int last_seen { get; set; }
    public int connections { get; set; }

    [JsonConstructor]
    /// <summary>
    /// Parameterless constructor for manual assigning of data
    /// </summary>
    public API_PlayerSurfProfile() { }

    /// <summary>
    /// Assigns values to the needed data model for an API request
    /// </summary>
    public API_PlayerSurfProfile(PlayerProfileDataModel data)
    {
        id = data.ID;
        name = data.Name;
        steam_id = data.SteamID;
        country = data.Country;
        join_date = data.JoinDate;
        last_seen = data.LastSeen;
        connections = data.Connections == 0 ? 1 : data.Connections;
    }
}

public class API_PersonalBest
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
    public int run_date { get; set; }
    public string replay_frames { get; set; } = ""; // This needs to be checked touroughly
    public List<API_Checkpoint>? checkpoints { get; set; } = null;
    public string name { get; set; } = "N/A";
    public int rank { get; set; }

    [JsonConstructor]
    public API_PersonalBest() { } // Parameterless constructor used by GET method
}

public class API_SaveMapTime
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
    public string replay_frames { get; set; } = "";
    public int? run_date { get; set; } = null;

    /// <summary>
    /// Assigns values to the needed data model for an API request
    /// </summary>
    internal API_SaveMapTime(MapTimeDataModel data)
    {
        player_id = data.PlayerId;
        map_id = data.MapId;
        run_time = data.RunTime;
        start_vel_x = data.StartVelX;
        start_vel_y = data.StartVelY;
        start_vel_z = data.StartVelZ;
        end_vel_x = data.EndVelX;
        end_vel_y = data.EndVelY;
        end_vel_z = data.EndVelZ;
        style = data.Style;
        type = data.Type;
        stage = data.Stage;
        replay_frames = data.ReplayFrames;
        run_date = data.RunDate;

        // Convert Checkpoints
        checkpoints = data.Checkpoints.Select(cp => new API_Checkpoint
        {
            cp = cp.Key,
            run_time = cp.Value.RunTime,
            end_touch = cp.Value.EndTouch,
            start_vel_x = cp.Value.StartVelX,
            start_vel_y = cp.Value.StartVelY,
            start_vel_z = cp.Value.StartVelZ,
            end_vel_x = cp.Value.EndVelX,
            end_vel_y = cp.Value.EndVelY,
            end_vel_z = cp.Value.EndVelZ,
            attempts = cp.Value.Attempts
        }).ToList();
    }
}

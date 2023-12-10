using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;

namespace SurfTimer;

public class Map 
{
    // Map information
    public int ID {get; set;} = 0;
    public string Name {get; set;} = "";
    public string Author {get; set;} = "";
    public int Tier {get; set;} = 0;
    public int Stages {get; set;} = 0;
    public bool Ranked {get; set;} = false;
    public int DateAdded {get; set;} = 0;

    // Zone Information
    public Vector StartZoneOrigin {get;} = new Vector(0,0,0);
    public Vector EndZoneOrigin {get;} = new Vector(0,0,0);

    internal Map(string Name, TimerDatabase DB)
    {
        // Gather Zone Information for triggers with name "map_start" and "map_end"
        IEnumerable<CBaseEntity> triggers = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("trigger_multiple");
        foreach (CBaseEntity trigger in triggers)
        {
            if (trigger.Entity!.Name != null)
            {
                if (trigger.Entity!.Name.Contains("map_start") || trigger.Entity!.Name.Contains("stage1_start") || trigger.Entity!.Name.Contains("s1_start"))
                    this.StartZoneOrigin = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                else if (trigger.Entity!.Name.Contains("map_end"))
                    this.EndZoneOrigin = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
            }
        }
        Console.WriteLine($"[CS2 Surf] Identifying start zone: {this.StartZoneOrigin.X},{this.StartZoneOrigin.Y},{this.StartZoneOrigin.Z}\nIdentifying end zone: {this.EndZoneOrigin.X},{this.EndZoneOrigin.Y},{this.EndZoneOrigin.Z}");

        // Gather map information OR create entry
        Task<MySqlDataReader> reader = DB.Query($"SELECT * FROM Maps WHERE name='{MySqlHelper.EscapeString(Name)}'");
        MySqlDataReader mapData = reader.Result;
        if (mapData.HasRows && mapData.Read())
        {
            this.ID = mapData.GetInt32("id");
            this.Name = Name;
            this.Author = mapData.GetString("author") ?? "Unknown";
            this.Tier = mapData.GetInt32("tier");
            this.Stages = mapData.GetInt32("stages");
            this.Ranked = mapData.GetBoolean("ranked");
            this.DateAdded = mapData.GetInt32("date_added");
            mapData.Close();

            // Update last_played data
            Task<int> writer = DB.Write($"UPDATE Maps SET last_played={(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()} WHERE id = {this.ID}");
        }

        else
        {
            mapData.Close();
            Task<int> writer = DB.Write($"INSERT INTO Maps (name, author, tier, stages, ranked, date_added, last_played) VALUES ('{MySqlHelper.EscapeString(Name)}', 'Unknown', 0, 0, 0, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()})");
            int writerRows = writer.Result;
            if (writerRows != 1)
                throw new Exception($"CS2 Surf ERROR >> OnRoundStart -> new Map() -> Failed to write new map to database, this shouldnt happen. Map: {Name}");
            
            Task<MySqlDataReader> postWriteReader = DB.Query($"SELECT * FROM Maps WHERE name='{MySqlHelper.EscapeString(Name)}'");
            MySqlDataReader postWriteMapData = postWriteReader.Result;
            if (postWriteMapData.HasRows && postWriteMapData.Read())
            {
                this.ID = postWriteMapData.GetInt32("id");
            }
            postWriteMapData.Close();
            this.Name = Name;
            this.Author = "Unknown";
            this.Tier = 0;
            this.Stages = 0;
            this.Ranked = false;
            this.DateAdded = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(); 
        }
    }
}
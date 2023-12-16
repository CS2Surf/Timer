using System.Text.RegularExpressions;
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

    // Zone Origin Information
    public Vector StartZone {get;} = new Vector(0,0,0);
    public QAngle StartZoneAngles {get;} = new QAngle(0,0,0);
    public Vector[] StageStartZone {get;} = Enumerable.Repeat(0, 99).Select(x => new Vector(0,0,0)).ToArray();
    public QAngle[] StageStartZoneAngles {get;} = Enumerable.Repeat(0, 99).Select(x => new QAngle(0,0,0)).ToArray();
    // public Vector[] BonusStartZone {get;} = Enumerable.Repeat(0, 99).Select(x => new Vector(0,0,0)).ToArray(); // To-do: Implement bonuses
    public Vector EndZone {get;} = new Vector(0,0,0);
    public QAngle EndZoneAngles {get;} = new QAngle(0,0,0);
    // public Vector[] BonusEndZone {get;} = Enumerable.Repeat(0, 99).Select(x => new Vector(0,0,0)).ToArray(); // To-do: Implement bonuses

    // Constructor
    internal Map(string Name, TimerDatabase DB)
    {
        // Gathering zones from the map
        IEnumerable<CBaseTrigger> triggers = Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple");
        foreach (CBaseTrigger trigger in triggers)
        {
            if (trigger.Entity!.Name != null)
            {
                if (trigger.Entity!.Name.Contains("map_start") || 
                    trigger.Entity!.Name.Contains("stage1_start") || 
                    trigger.Entity!.Name.Contains("s1_start")) // Map start zone
                {
                    this.StartZone = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                    this.StartZoneAngles = new QAngle(trigger.AbsRotation!.X, trigger.AbsRotation!.Y, trigger.AbsRotation!.Z);
                }

                else if (trigger.Entity!.Name.Contains("map_end")) // Map end zone
                {
                    this.EndZone = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                    this.EndZoneAngles = new QAngle(trigger.AbsRotation!.X, trigger.AbsRotation!.Y, trigger.AbsRotation!.Z);
                }

                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success) // Stage start zones
                {
                    this.StageStartZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);

                    // Find an info_destination_teleport inside this zone to grab angles from
                    IEnumerable<CTriggerTeleport> teleports = Utilities.FindAllEntitiesByDesignerName<CTriggerTeleport>("info_teleport_destination");
                    foreach (CBaseEntity teleport in teleports)
                    {
                        if (teleport.Entity!.Name != null && IsInZone(trigger.AbsOrigin!, trigger.Collision.BoundingRadius, teleport.AbsOrigin!))
                        {
                            this.StageStartZoneAngles[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1] = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                        }
                    }
                }
            }
        }
        Console.WriteLine($"[CS2 Surf] Identifying start zone: {this.StartZone.X},{this.StartZone.Y},{this.StartZone.Z}\nIdentifying end zone: {this.EndZone.X},{this.EndZone.Y},{this.EndZone.Z}");

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

            return;
        }

        // Update the map's last played data in the DB
        // Update last_played data
        Task<int> updater = DB.Write($"UPDATE Maps SET last_played={(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()} WHERE id = {this.ID}");
        int lastPlayedUpdateRows = updater.Result;
        if (lastPlayedUpdateRows != 1)
            throw new Exception($"CS2 Surf ERROR >> OnRoundStart -> update Map() -> Failed to update map in database, this shouldnt happen. Map: {Name}");
    }

    public bool IsInZone(Vector zoneOrigin, float zoneCollisionRadius, Vector spawnOrigin)
    {
        if (spawnOrigin.X >= zoneOrigin.X - zoneCollisionRadius && spawnOrigin.X <= zoneOrigin.X + zoneCollisionRadius &&
            spawnOrigin.Y >= zoneOrigin.Y - zoneCollisionRadius && spawnOrigin.Y <= zoneOrigin.Y + zoneCollisionRadius &&
            spawnOrigin.Z >= zoneOrigin.Z - zoneCollisionRadius && spawnOrigin.Z <= zoneOrigin.Z + zoneCollisionRadius)
            return true;
        else
            return false;
    }
}
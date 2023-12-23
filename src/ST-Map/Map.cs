using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;

namespace SurfTimer;

public class Map 
{
    // Map information
    public int ID {get; set;} = -1; // Can we use this to re-trigger retrieving map information from the database?? (all db IDs are auto-incremented)
    public string Name {get; set;} = "";
    public string Author {get; set;} = "";
    public int Tier {get; set;} = 0;
    public int Stages {get; set;} = 0;
    public int Bonuses {get; set;} = 0;
    public bool Ranked {get; set;} = false;
    public int DateAdded {get; set;} = 0;
    public int LastPlayed {get; set;} = 0;
    public int TotalCompletions {get; set;} = 0;
    public int WrRunTime {get; set;} = 0;

    // Zone Origin Information
    // Map start/end zones
    public Vector StartZone {get;} = new Vector(0,0,0);
    public QAngle StartZoneAngles {get;} = new QAngle(0,0,0);
    public Vector EndZone {get;} = new Vector(0,0,0);
    // Map stage zones
    public Vector[] StageStartZone {get;} = Enumerable.Repeat(0, 99).Select(x => new Vector(0,0,0)).ToArray();
    public QAngle[] StageStartZoneAngles {get;} = Enumerable.Repeat(0, 99).Select(x => new QAngle(0,0,0)).ToArray();
    // Map bonus zones
    public Vector[] BonusStartZone {get;} = Enumerable.Repeat(0, 99).Select(x => new Vector(0,0,0)).ToArray(); // To-do: Implement bonuses
    public QAngle[] BonusStartZoneAngles {get;} = Enumerable.Repeat(0, 99).Select(x => new QAngle(0,0,0)).ToArray(); // To-do: Implement bonuses
    public Vector[] BonusEndZone {get;} = Enumerable.Repeat(0, 99).Select(x => new Vector(0,0,0)).ToArray(); // To-do: Implement bonuses

    // Constructor
    internal Map(string Name, TimerDatabase DB)
    {
        // Gathering zones from the map
        IEnumerable<CBaseTrigger> triggers = Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple");
        // Gathering info_teleport_destinations from the map
        IEnumerable<CTriggerTeleport> teleports = Utilities.FindAllEntitiesByDesignerName<CTriggerTeleport>("info_teleport_destination");
        foreach (CBaseTrigger trigger in triggers)
        {
            if (trigger.Entity!.Name != null)
            {
                // Map start zone
                if (trigger.Entity!.Name.Contains("map_start") || 
                    trigger.Entity!.Name.Contains("stage1_start") || 
                    trigger.Entity!.Name.Contains("s1_start")) 
                {
                    this.StartZone = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                    foreach (CBaseEntity teleport in teleports)
                    {
                        if (teleport.Entity!.Name != null && IsInZone(trigger.AbsOrigin!, trigger.Collision.BoundingRadius, teleport.AbsOrigin!))
                        {
                            this.StartZoneAngles = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                        }
                    }
                }

                // Map end zone
                else if (trigger.Entity!.Name.Contains("map_end")) 
                {
                    this.EndZone = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                }

                // Stage start zones
                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success) 
                {
                    this.StageStartZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);

                    // Find an info_destination_teleport inside this zone to grab angles from
                    foreach (CBaseEntity teleport in teleports)
                    {
                        if (teleport.Entity!.Name != null && IsInZone(trigger.AbsOrigin!, trigger.Collision.BoundingRadius, teleport.AbsOrigin!))
                        {
                            this.StageStartZoneAngles[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1] = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                        }
                    }
                }

                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_start$").Success) 
                {
                    this.BonusStartZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);

                    // Find an info_destination_teleport inside this zone to grab angles from
                    foreach (CBaseEntity teleport in teleports)
                    {
                        if (teleport.Entity!.Name != null && IsInZone(trigger.AbsOrigin!, trigger.Collision.BoundingRadius, teleport.AbsOrigin!))
                        {
                            this.BonusStartZoneAngles[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1] = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                        }
                    }
                }

                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_end$").Success) 
                {
                    this.BonusEndZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
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
            this.Bonuses = mapData.GetInt32("bonuses");
            this.Ranked = mapData.GetBoolean("ranked");
            this.DateAdded = mapData.GetInt32("date_added");
            this.LastPlayed = mapData.GetInt32("last_played");
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
            this.Tier = -1;
            this.Stages = -1;
            this.Bonuses = -1;
            this.Ranked = false;
            this.DateAdded = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.LastPlayed = this.DateAdded; 

            return;
        }

        // Update the map's last played data in the DB
        // Update last_played data
        Task<int> updater = DB.Write($"UPDATE Maps SET last_played={(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()} WHERE id = {this.ID}");
        int lastPlayedUpdateRows = updater.Result;
        if (lastPlayedUpdateRows != 1)
            throw new Exception($"CS2 Surf ERROR >> OnRoundStart -> update Map() -> Failed to update map in database, this shouldnt happen. Map: {Name}");
        updater.Dispose();

        // Initiates getting the World Records for the map
        // To-do: Will this check if no records exist for the map? (i.e. no rows returned)
        GetMapRecordAndTotals(DB); // To-do: Implement styles
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

    // Leaving this outside of the constructor for `Map` so we can call it to ONLY update the data when a new world record is set
    internal void GetMapRecordAndTotals(TimerDatabase DB, int style = 0 ) // To-do: Implement styles
    {
        // Get map world records
        Task<MySqlDataReader> reader = DB.Query($"SELECT * FROM `MapTimes` WHERE `map_id` = {this.ID} AND `style` = {style} ORDER BY `run_time` ASC;'");
        MySqlDataReader mapWrData = reader.Result;
        int totalRows = 0;
        
        if (mapWrData.HasRows)
        { 
            // To-do: Implement bonuses WR
            // To-do: Implement stages WR
            // To-do: Implement checkpoints WR
            while (mapWrData.Read())
            {
                if (totalRows == 0)
                    this.WrRunTime = mapWrData.GetInt32("run_time"); // Fastest run time (WR) for the Map and Style combo

                totalRows++;
            }
        }

        this.TotalCompletions = totalRows; // Total completions for the map and style

        mapWrData.Close();
    }
}
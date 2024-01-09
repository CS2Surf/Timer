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
    public int Checkpoints {get; set;} = 0;
    public int Bonuses {get; set;} = 0;
    public bool Ranked {get; set;} = false;
    public int DateAdded {get; set;} = 0;
    public int LastPlayed {get; set;} = 0;
    public int TotalCompletions {get; set;} = 0;
    public int WrRunTime {get; set;} = 0;
    public int WrId {get; set;} = 0;

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
    // Map checkpoint zones
    public Vector[] CheckpointStartZone {get;} = Enumerable.Repeat(0, 99).Select(x => new Vector(0,0,0)).ToArray();

    // Constructor
    internal Map(string Name, TimerDatabase DB)
    {
        // Set map name
        this.Name = Name;
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
                    bool foundPlayerSpawn = false; // Track whether a player spawn is found
                    foreach (CBaseEntity teleport in teleports)
                    {
                        if (teleport.Entity!.Name != null && 
                            (IsInZone(trigger.AbsOrigin!, trigger.Collision.BoundingRadius, teleport.AbsOrigin!) || 
                            teleport.Entity!.Name.Contains("spawn_map_start") || 
                            teleport.Entity!.Name.Contains("spawn_stage1_start") || 
                            teleport.Entity!.Name.Contains("spawn_s1_start")))
                        {
                            this.StartZone = new Vector(teleport.AbsOrigin!.X, teleport.AbsOrigin!.Y, teleport.AbsOrigin!.Z);
                            this.StartZoneAngles = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                            foundPlayerSpawn = true;
                            break;
                        }
                    }

                    if (!foundPlayerSpawn)
                    {
                        this.StartZone = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
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
                    int stage = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);
                    
                    // Find an info_destination_teleport inside this zone to grab angles from
                    bool foundPlayerSpawn = false; // Track whether a player spawn is found
                    foreach (CBaseEntity teleport in teleports)
                    {
                        if (teleport.Entity!.Name != null && 
                            (IsInZone(trigger.AbsOrigin!, trigger.Collision.BoundingRadius, teleport.AbsOrigin!) || (Regex.Match(teleport.Entity.Name, "^spawn_s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success && Int32.Parse(Regex.Match(teleport.Entity.Name, "[0-9][0-9]?").Value) == stage)))
                        {
                            this.StageStartZone[stage - 1] = new Vector(teleport.AbsOrigin!.X, teleport.AbsOrigin!.Y, teleport.AbsOrigin!.Z);
                            this.StageStartZoneAngles[stage - 1] = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                            this.Stages++; // Count stage zones for the map to populate DB
                            foundPlayerSpawn = true;
                            break;
                        }
                    }

                    if (!foundPlayerSpawn)
                    {
                        this.StageStartZone[stage - 1] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                    }
                }

                // Checkpoint start zones (linear maps)
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success) 
                {
                    this.CheckpointStartZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                    this.Checkpoints++; // Might be useful to have this in DB entry
                }
                
                // Bonus start zones
                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_start$").Success) 
                {
                    int bonus = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);

                    // Find an info_destination_teleport inside this zone to grab angles from
                    bool foundPlayerSpawn = false; // Track whether a player spawn is found
                    foreach (CBaseEntity teleport in teleports)
                    {
                        if (teleport.Entity!.Name != null && 
                            (IsInZone(trigger.AbsOrigin!, trigger.Collision.BoundingRadius, teleport.AbsOrigin!) || (Regex.Match(teleport.Entity.Name, "^spawn_b([1-9][0-9]?|onus[1-9][0-9]?)_start$").Success && Int32.Parse(Regex.Match(teleport.Entity.Name, "[0-9][0-9]?").Value) == bonus)))
                        {
                            this.BonusStartZone[bonus - 1] = new Vector(teleport.AbsOrigin!.X, teleport.AbsOrigin!.Y, teleport.AbsOrigin!.Z);
                            this.BonusStartZoneAngles[bonus - 1] = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                            this.Bonuses++; // Count bonus zones for the map to populate DB
                            foundPlayerSpawn = true;
                            break;
                        }
                    }

                    if (!foundPlayerSpawn)
                    {
                        this.BonusStartZone[bonus - 1] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
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
        bool updateData = false;
        if (mapData.HasRows && mapData.Read()) // In here we can check whether MapData in DB is the same as the newly extracted data, if not, update it (as hookzones may have changed on map updates)
        {
            this.ID = mapData.GetInt32("id");
            this.Author = mapData.GetString("author") ?? "Unknown";
            this.Tier = mapData.GetInt32("tier");
            if (this.Stages != mapData.GetInt32("stages") || this.Bonuses != mapData.GetInt32("bonuses"))
                updateData = true;
            // this.Stages = mapData.GetInt32("stages");    // this should now be populated accordingly when looping through hookzones for the map
            // this.Bonuses = mapData.GetInt32("bonuses");  // this should now be populated accordingly when looping through hookzones for the map
            this.Ranked = mapData.GetBoolean("ranked");
            this.DateAdded = mapData.GetInt32("date_added");
            this.LastPlayed = mapData.GetInt32("last_played");
            updateData = true;
            mapData.Close();
        }

        else
        {
            mapData.Close();
            Task<int> writer = DB.Write($"INSERT INTO Maps (name, author, tier, stages, ranked, date_added, last_played) VALUES ('{MySqlHelper.EscapeString(Name)}', 'Unknown', {this.Stages}, {this.Bonuses}, 0, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()})");
            int writerRows = writer.Result;
            if (writerRows != 1)
                throw new Exception($"CS2 Surf ERROR >> OnRoundStart -> new Map() -> Failed to write new map to database, this shouldn't happen. Map: {Name}");
            
            Task<MySqlDataReader> postWriteReader = DB.Query($"SELECT * FROM Maps WHERE name='{MySqlHelper.EscapeString(Name)}'");
            MySqlDataReader postWriteMapData = postWriteReader.Result;
            if (postWriteMapData.HasRows && postWriteMapData.Read())
            {
                this.ID = postWriteMapData.GetInt32("id");
                this.Author = postWriteMapData.GetString("author");
                this.Tier = postWriteMapData.GetInt32("tier");
                // this.Stages = -1;    // this should now be populated accordingly when looping through hookzones for the map
                // this.Bonuses = -1;   // this should now be populated accordingly when looping through hookzones for the map
                this.Ranked = postWriteMapData.GetBoolean("ranked");
                this.DateAdded = postWriteMapData.GetInt32("date_added");
                this.LastPlayed = this.DateAdded; 
            }
            postWriteMapData.Close();

            return;
        }

        // Update the map's last played data in the DB
        // Update last_played data or update last_played, stages, and bonuses data
        string query = $"UPDATE Maps SET last_played={(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()} WHERE id={this.ID}";
        if (updateData) query = $"UPDATE Maps SET last_played={(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, stages={this.Stages}, bonuses={this.Bonuses} WHERE id={this.ID}";
        #if DEBUG
        Console.WriteLine($"CS2 Surf ERROR >> OnRoundStart -> update Map() -> Update MapData: {query}");
        #endif
        
        Task<int> updater = DB.Write(query);
        int lastPlayedUpdateRows = updater.Result;
        if (lastPlayedUpdateRows != 1)
            throw new Exception($"CS2 Surf ERROR >> OnRoundStart -> update Map() -> Failed to update map in database, this shouldnt happen. Map: {Name} | was it 'big' update? {updateData}");
        updater.Dispose();

        // Initiates getting the World Records for the map
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
                    this.WrId = mapWrData.GetInt32("id"); // WR ID for the Map and Style combo

                totalRows++;
            }
        }

        this.TotalCompletions = totalRows; // Total completions for the map and style

        mapWrData.Close();
    }
}
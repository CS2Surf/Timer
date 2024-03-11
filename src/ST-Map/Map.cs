using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;

namespace SurfTimer;

internal class Map 
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
    /// <summary>
    /// Map Completion Count - Refer to as MapCompletions[style]
    /// </summary>
    public Dictionary<int, int> MapCompletions {get; set;} = new Dictionary<int, int>(); 
    /// <summary>
    /// Bonus Completion Count - Refer to as BonusCompletions[bonus#][style]
    /// </summary>
    public Dictionary<int, int>[] BonusCompletions { get; set; } = new Dictionary<int, int>[32];
    /// <summary>
    /// Map World Record - Refer to as WR[style]
    /// </summary>
    public Dictionary<int, PersonalBest> WR { get; set; } = new Dictionary<int, PersonalBest>();
    /// <summary>
    /// Bonus World Record - Refer to as BonusWR[bonus#][style]
    /// </summary>
    public Dictionary<int, PersonalBest>[] BonusWR { get; set; } = new Dictionary<int, PersonalBest>[32];
    public List<int> ConnectedMapTimes { get; set; } = new List<int>();
    public List<ReplayPlayer> ReplayBots { get; set; } = new List<ReplayPlayer> { new ReplayPlayer() };

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
    // To-do: This loops through all the triggers. While that's great and comprehensive, some maps have two triggers with the exact same name, because there are two
    //        for each side of the course (left and right, for example). We should probably work on automatically catching this. 
    //        Maybe even introduce a new naming convention?
    internal Map(string Name, TimerDatabase DB)
    {
        // Set map name
        this.Name = Name;

        // Initialize WR variables
        this.WR[0] = new PersonalBest(); // To-do: Implement styles
        for (int i = 0; i < 32; i++)
        {
            BonusWR[i] = new Dictionary<int, PersonalBest>();
            BonusWR[i][0] = new PersonalBest(); // To-do: Implement styles
        }

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
                            this.StageStartZone[stage] = new Vector(teleport.AbsOrigin!.X, teleport.AbsOrigin!.Y, teleport.AbsOrigin!.Z);
                            this.StageStartZoneAngles[stage] = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                            this.Stages++; // Count stage zones for the map to populate DB
                            foundPlayerSpawn = true;
                            break;
                        }
                    }

                    if (!foundPlayerSpawn)
                    {
                        this.StageStartZone[stage] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                        this.Stages++;
                    }
                }

                // Checkpoint start zones (linear maps)
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success) 
                {
                    this.CheckpointStartZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value)] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
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
                            this.BonusStartZone[bonus] = new Vector(teleport.AbsOrigin!.X, teleport.AbsOrigin!.Y, teleport.AbsOrigin!.Z);
                            this.BonusStartZoneAngles[bonus] = new QAngle(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                            this.Bonuses++; // Count bonus zones for the map to populate DB
                            foundPlayerSpawn = true;
                            break;
                        }
                    }

                    if (!foundPlayerSpawn)
                    {
                        this.BonusStartZone[bonus] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                        this.Bonuses++;
                    }
                }

                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_end$").Success) 
                {
                    this.BonusEndZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value)] = new Vector(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                }
            }
        }

        if (this.Stages > 0) // Account for stage 1, not counted above
            this.Stages += 1; 
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
            writer.Dispose();

            Task<MySqlDataReader> postWriteReader = DB.Query($"SELECT * FROM Maps WHERE name='{MySqlHelper.EscapeString(Name)}'");
            MySqlDataReader postWriteMapData = postWriteReader.Result;
            if (postWriteMapData.HasRows && postWriteMapData.Read())
            {
                this.ID = postWriteMapData.GetInt32("id");
                this.Author = postWriteMapData.GetString("author");
                this.Tier = postWriteMapData.GetInt32("tier");
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

        this.ReplayBots[0].Stat_MapTimeID = this.WR[0].ID; // Sets WrIndex to WR maptime_id
        if(this.Stages > 0) // If stages map adds bot
            this.ReplayBots = this.ReplayBots.Prepend(new ReplayPlayer()).ToList();

        if(this.Bonuses > 0) // If has bonuses adds bot
            this.ReplayBots = this.ReplayBots.Prepend(new ReplayPlayer()).ToList();
    }

    public void KickReplayBot(int index)
    {
        if (!this.ReplayBots[index].IsPlayable)
            return;

        int? id_to_kick = this.ReplayBots[index].Controller!.UserId;
        if(id_to_kick == null)
            return;

        this.ReplayBots.RemoveAt(index);
        Server.ExecuteCommand($"kickid {id_to_kick}; bot_quota {this.ReplayBots.Count}");
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
        Task<MySqlDataReader> reader = DB.Query($@"
            SELECT MapTimes.*, MIN(MapTimes.run_time) AS minimum, Player.name
            FROM MapTimes
            JOIN Player ON MapTimes.player_id = Player.id
            WHERE MapTimes.map_id = {this.ID} AND MapTimes.style = {style}
            GROUP BY MapTimes.type
            ORDER BY MapTimes.run_time ASC;
        ");
        MySqlDataReader mapWrData = reader.Result;
        
        if (mapWrData.HasRows)
        { 
            // To-do: Implement bonuses WR
            // To-do: Implement stages WR
            this.ConnectedMapTimes.Clear();
            while (mapWrData.Read())
            {
                if (mapWrData.GetInt32("type") > 0)
                {
                    this.BonusWR[mapWrData.GetInt32("type")][style].ID = mapWrData.GetInt32("id"); // WR ID for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].Ticks = mapWrData.GetInt32("run_time"); // Fastest run time (WR) for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].Type = mapWrData.GetInt32("type"); // Bonus type (0 = map, 1+ = bonus index)
                    this.BonusWR[mapWrData.GetInt32("type")][style].StartVelX = mapWrData.GetFloat("start_vel_x"); // Fastest run start velocity X for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].StartVelY = mapWrData.GetFloat("start_vel_y"); // Fastest run start velocity Y for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].StartVelZ = mapWrData.GetFloat("start_vel_z"); // Fastest run start velocity Z for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].EndVelX = mapWrData.GetFloat("end_vel_x"); // Fastest run end velocity X for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].EndVelY = mapWrData.GetFloat("end_vel_y"); // Fastest run end velocity Y for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].EndVelZ = mapWrData.GetFloat("end_vel_z"); // Fastest run end velocity Z for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].RunDate = mapWrData.GetInt32("run_date"); // Fastest run date for the Map and Style combo
                    this.BonusWR[mapWrData.GetInt32("type")][style].Name = mapWrData.GetString("name"); // Fastest run player name for the Map and Style combo
                }

                else 
                {
                    this.WR[style].ID = mapWrData.GetInt32("id"); // WR ID for the Map and Style combo
                    this.WR[style].Ticks = mapWrData.GetInt32("run_time"); // Fastest run time (WR) for the Map and Style combo
                    this.WR[style].Type = mapWrData.GetInt32("type"); // Bonus type (0 = map, 1+ = bonus index)
                    this.WR[style].StartVelX = mapWrData.GetFloat("start_vel_x"); // Fastest run start velocity X for the Map and Style combo
                    this.WR[style].StartVelY = mapWrData.GetFloat("start_vel_y"); // Fastest run start velocity Y for the Map and Style combo
                    this.WR[style].StartVelZ = mapWrData.GetFloat("start_vel_z"); // Fastest run start velocity Z for the Map and Style combo
                    this.WR[style].EndVelX = mapWrData.GetFloat("end_vel_x"); // Fastest run end velocity X for the Map and Style combo
                    this.WR[style].EndVelY = mapWrData.GetFloat("end_vel_y"); // Fastest run end velocity Y for the Map and Style combo
                    this.WR[style].EndVelZ = mapWrData.GetFloat("end_vel_z"); // Fastest run end velocity Z for the Map and Style combo
                    this.WR[style].RunDate = mapWrData.GetInt32("run_date"); // Fastest run date for the Map and Style combo
                    this.WR[style].Name = mapWrData.GetString("name"); // Fastest run player name for the Map and Style combo
                
                    this.ConnectedMapTimes.Add(mapWrData.GetInt32("id"));
                }
            }
        }
        mapWrData.Close();

        // Count completions
        Task<MySqlDataReader> completionStats = DB.Query($@"
            SELECT MapTimes.type, COUNT(*) as count
            FROM MapTimes 
            WHERE MapTimes.map_id = {this.ID}
            GROUP BY type;
        ");
        MySqlDataReader completionStatsResult = completionStats.Result;

        if (completionStatsResult.HasRows)
        {
            while (completionStatsResult.Read())
            {
                if (completionStatsResult.GetInt32("type") > 0)
                {
                    // To-do: bonus completion counts
                }

                else
                {
                    // Total completions for the map and style - this should maybe be added to PersonalBest class
                    this.MapCompletions[style] = completionStatsResult.GetInt32("count");
                }
            }
        }
        completionStatsResult.Close();

        // Get map world record checkpoints
        if (this.MapCompletions[style] != 0)
        {
            Task<MySqlDataReader> cpReader = DB.Query($"SELECT * FROM `Checkpoints` WHERE `maptime_id` = {this.WR[style].ID};");
            MySqlDataReader cpWrData = cpReader.Result;
            while (cpWrData.Read())
            {
                #if DEBUG
                Console.WriteLine($"cp {cpWrData.GetInt32("cp")} ");
                Console.WriteLine($"run_time {cpWrData.GetInt32("run_time")} ");
                Console.WriteLine($"sVelX {cpWrData.GetFloat("start_vel_x")} ");
                Console.WriteLine($"sVelY {cpWrData.GetFloat("start_vel_y")} ");
                #endif

                Checkpoint cp = new(cpWrData.GetInt32("cp"),
                                    cpWrData.GetInt32("run_time"),
                                    cpWrData.GetFloat("start_vel_x"),
                                    cpWrData.GetFloat("start_vel_y"),
                                    cpWrData.GetFloat("start_vel_z"),
                                    cpWrData.GetFloat("end_vel_x"),
                                    cpWrData.GetFloat("end_vel_y"),
                                    cpWrData.GetFloat("end_vel_z"),
                                    cpWrData.GetInt32("end_touch"),
                                    cpWrData.GetInt32("attempts"));
                cp.ID = cpWrData.GetInt32("cp");
                // To-do: cp.ID = calculate Rank # from DB

                this.WR[style].Checkpoint[cp.CP] = cp;

                #if DEBUG
                Console.WriteLine($"======= CS2 Surf DEBUG >> internal void GetMapRecordAndTotals : Map -> Loaded WR CP {cp.CP} with RunTime {cp.Ticks} for MapTimeID {WR[0].ID} (MapId = {this.ID}).");
                #endif
            }
            cpWrData.Close();
        }
    }
}
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System;
using System.Net.Http.Json;

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
    public Dictionary<int, int>[] StageCompletions { get; set; } = new Dictionary<int, int>[32];
    /// <summary>
    /// Map World Record - Refer to as WR[style]
    /// </summary>
    public Dictionary<int, PersonalBest> WR { get; set; } = new Dictionary<int, PersonalBest>();
    /// <summary>
    /// Bonus World Record - Refer to as BonusWR[bonus#][style]
    /// </summary>
    public Dictionary<int, PersonalBest>[] BonusWR { get; set; } = new Dictionary<int, PersonalBest>[32];
    public Dictionary<int, PersonalBest>[] StageWR { get; set; } = new Dictionary<int, PersonalBest>[32];
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
            this.BonusWR[i] = new Dictionary<int, PersonalBest>();
            this.BonusWR[i][0] = new PersonalBest(); // To-do: Implement styles
            this.BonusCompletions[i] = new Dictionary<int, int>();

            this.StageWR[i] = new Dictionary<int, PersonalBest>();
            this.StageWR[i][0] = new PersonalBest(); // To-do: Implement styles
            this.StageCompletions[i] = new Dictionary<int, int>();
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
        
        bool updateData = false;
        var mapinfo = APICall.GET<API_MapInfo>($"/surftimer/mapinfo?mapname={Name}").Result;
        if (mapinfo != null)
        {
            this.ID = mapinfo.id;
            this.Author = mapinfo.author;
            this.Tier = mapinfo.tier;
            if (this.Stages != mapinfo.stages || this.Bonuses != mapinfo.bonuses)
                updateData = true;
            this.Ranked = mapinfo.ranked == 1 ? true : false;
            this.DateAdded = (int)mapinfo.date_added!;
            this.LastPlayed = (int)mapinfo.last_played!;
        }
        else
        {
            API_MapInfo inserted = new API_MapInfo
            {
                id = -1, // Shouldn't really use this at all at api side
                name = Name,
                author = "Unknown",
                tier = this.Tier,
                stages = this.Stages,
                bonuses = this.Bonuses,
                ranked = 0,
            };

            _ = APICall.POST($"/surftimer/insertmap", inserted).Result;
            return;
        }

        API_MapInfo updated = new API_MapInfo
        {
                id = this.ID,
                name = Name,
                author = "Unknown",
                tier = this.Tier,
                stages = this.Stages,
                bonuses = this.Bonuses,
                ranked = 0,
                last_played = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (updateData)
        {
            updated.stages = this.Stages;
            updated.bonuses = this.Bonuses;
        }

        _ = APICall.PUT($"/surftimer/updateMap", updated).Result;

        // Initiates getting the World Records for the map
        GetMapRecordAndTotals(DB); // To-do: Implement styles

        // this.ReplayBots[0].Stat_MapTimeID = this.WR[0].ID; // Sets WrIndex to WR maptime_id
        this.ReplayBots[0].RecordRank = 1;
        this.ReplayBots[0].Type = 0;
        this.ReplayBots[0].Stage = 0;
        
        if(this.Stages > 0) // If stages map adds bot
            this.ReplayBots = this.ReplayBots.Prepend(new ReplayPlayer()).ToList();
            this.ReplayBots[0].RecordRank = 1;
            this.ReplayBots[0].Type = 2;
            this.ReplayBots[0].Stage = 0;
            // Add stage MapTimeID

        if(this.Bonuses > 0) // If has bonuses adds bot
            this.ReplayBots = this.ReplayBots.Prepend(new ReplayPlayer()).ToList();
            // int idx = this.Stages > 0 ? 1 : 0;
            this.ReplayBots[0].RecordRank = 1;
            this.ReplayBots[0].Type = 1;
            this.ReplayBots[0].Stage = 0;
    }


    // Leaving this outside of the constructor for `Map` so we can call it to ONLY update the data when a new world record is set
    internal void GetMapRecordAndTotals(TimerDatabase DB, int style = 0 ) // To-do: Implement styles
    {
        var maptimes = APICall.GET<API_MapTime[]>($"/surftimer/maptotals?map_id={this.ID}&style={style}").Result; // TODO: Implement styles
        if (maptimes == null)
            return;

        foreach (API_MapTime mt in maptimes)
        {
            if (mt.type == 1)
            {
                this.BonusWR[mt.stage][style].ID = mt.id;
                this.BonusWR[mt.stage][style].Type = mt.type;
                this.BonusWR[mt.stage][style].Ticks = mt.run_time;
                this.BonusWR[mt.stage][style].StartVelX = mt.start_vel_x;
                this.BonusWR[mt.stage][style].StartVelY = mt.start_vel_y;
                this.BonusWR[mt.stage][style].StartVelZ = mt.start_vel_z;
                this.BonusWR[mt.stage][style].EndVelX = mt.end_vel_x;
                this.BonusWR[mt.stage][style].EndVelY = mt.end_vel_y;
                this.BonusWR[mt.stage][style].EndVelZ = mt.end_vel_z;
                this.BonusWR[mt.stage][style].RunDate = (int)mt.run_date!;
                this.BonusWR[mt.stage][style].Name = mt.name;

                if (!this.BonusCompletions[mt.stage].ContainsKey(style))
                {
                    this.BonusCompletions[mt.stage][style] = 0;
                }
                else
                {
                    this.BonusCompletions[mt.stage][style]++;
                }

            }
            else if (mt.type == 2)
            {
                this.StageWR[mt.stage][style].ID = mt.id;
                this.StageWR[mt.stage][style].Type = mt.type;
                this.StageWR[mt.stage][style].Ticks = mt.run_time;
                this.StageWR[mt.stage][style].StartVelX = mt.start_vel_x;
                this.StageWR[mt.stage][style].StartVelY = mt.start_vel_y;
                this.StageWR[mt.stage][style].StartVelZ = mt.start_vel_z;
                this.StageWR[mt.stage][style].EndVelX = mt.end_vel_x;
                this.StageWR[mt.stage][style].EndVelY = mt.end_vel_y;
                this.StageWR[mt.stage][style].EndVelZ = mt.end_vel_z;
                this.StageWR[mt.stage][style].RunDate = (int)mt.run_date!;
                this.StageWR[mt.stage][style].Name = mt.name;

                if (!this.StageCompletions[mt.stage].ContainsKey(style))
                {
                    this.StageCompletions[mt.stage][style] = 0;
                }
                else
                {
                    this.StageCompletions[mt.stage][style]++;
                }
            }
            else
            {
                this.WR[style].ID = mt.id;
                this.WR[style].Type = mt.type;
                this.WR[style].Ticks = mt.run_time;
                this.WR[style].StartVelX = mt.start_vel_x;
                this.WR[style].StartVelY = mt.start_vel_y;
                this.WR[style].StartVelZ = mt.start_vel_z;
                this.WR[style].EndVelX = mt.end_vel_x;
                this.WR[style].EndVelY = mt.end_vel_y;
                this.WR[style].EndVelZ = mt.end_vel_z;
                this.WR[style].RunDate = (int)mt.run_date!;
                this.WR[style].Name = mt.name;

                this.ConnectedMapTimes.Add(mt.id);

                if (!this.MapCompletions.ContainsKey(style))
                {
                    this.MapCompletions[style] = 0;
                }
                else
                {
                    this.MapCompletions[style]++;
                }
            }
        }

        var checkpoints = APICall.GET<API_Checkpoint[]>($"/surftimer/mapcheckpointsdata?maptime_id={this.WR[style].ID}").Result;
        if (checkpoints == null || checkpoints.Length == 0)
            return;

        foreach (API_Checkpoint checkpoint in checkpoints)
        {
            Checkpoint cp = new Checkpoint
            (
                checkpoint.cp,
                checkpoint.ticks,
                checkpoint.start_vel_x,
                checkpoint.start_vel_y,
                checkpoint.start_vel_z,
                checkpoint.end_vel_x,
                checkpoint.end_vel_y,
                checkpoint.end_vel_z,
                checkpoint.end_touch,
                checkpoint.attempts
            );
            cp.ID = checkpoint.cp;

            this.WR[style].Checkpoint[cp.CP] = cp;
        }
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
}
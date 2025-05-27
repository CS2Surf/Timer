using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace SurfTimer;

internal class Map
{
    // Map information
    public int ID { get; set; } = -1; // Can we use this to re-trigger retrieving map information from the database?? (all db IDs are auto-incremented)
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = "";
    public int Tier { get; set; } = 0;
    public int Stages { get; set; } = 0;
    public int TotalCheckpoints { get; set; } = 0;
    public int Bonuses { get; set; } = 0;
    public bool Ranked { get; set; } = false;
    public int DateAdded { get; set; } = 0;
    public int LastPlayed { get; set; } = 0;
    /// <summary>
    /// Map Completion Count - Refer to as MapCompletions[style]
    /// </summary>
    public Dictionary<int, int> MapCompletions { get; set; } = new Dictionary<int, int>();
    /// <summary>
    /// Bonus Completion Count - Refer to as BonusCompletions[bonus#][style]
    /// </summary>
    public Dictionary<int, int>[] BonusCompletions { get; set; } = new Dictionary<int, int>[32];
    /// <summary>
    /// Stage Completion Count - Refer to as StageCompletions[stage#][style]
    /// </summary>
    public Dictionary<int, int>[] StageCompletions { get; set; } = new Dictionary<int, int>[32];
    /// <summary>
    /// Map World Record - Refer to as WR[style]
    /// </summary>
    public Dictionary<int, PersonalBest> WR { get; set; } = new Dictionary<int, PersonalBest>();
    /// <summary>
    /// Bonus World Record - Refer to as BonusWR[bonus#][style]
    /// </summary>
    public Dictionary<int, PersonalBest>[] BonusWR { get; set; } = new Dictionary<int, PersonalBest>[32];
    /// <summary>
    /// Stage World Record - Refer to as StageWR[stage#][style]
    /// </summary>
    public Dictionary<int, PersonalBest>[] StageWR { get; set; } = new Dictionary<int, PersonalBest>[32];

    /// <summary>
    /// Not sure what this is for.
    /// Guessing it's to do with Replays and the ability to play your PB replay.
    /// 
    /// - T
    /// </summary>
    public List<int> ConnectedMapTimes { get; set; } = new List<int>();

    // Zone Origin Information
    /* Map Start/End zones */
    public Vector StartZone { get; set; } = new Vector(0, 0, 0);
    public QAngle StartZoneAngles { get; set; } = new QAngle(0, 0, 0);
    public Vector EndZone { get; set; } = new Vector(0, 0, 0);
    /* Map Stage zones */
    public Vector[] StageStartZone { get; } = Enumerable.Repeat(0, 99).Select(x => new Vector(0, 0, 0)).ToArray();
    public QAngle[] StageStartZoneAngles { get; } = Enumerable.Repeat(0, 99).Select(x => new QAngle(0, 0, 0)).ToArray();
    /* Map Bonus zones */
    public Vector[] BonusStartZone { get; } = Enumerable.Repeat(0, 99).Select(x => new Vector(0, 0, 0)).ToArray(); // To-do: Implement bonuses
    public QAngle[] BonusStartZoneAngles { get; } = Enumerable.Repeat(0, 99).Select(x => new QAngle(0, 0, 0)).ToArray(); // To-do: Implement bonuses
    public Vector[] BonusEndZone { get; } = Enumerable.Repeat(0, 99).Select(x => new Vector(0, 0, 0)).ToArray(); // To-do: Implement bonuses
    /* Map Checkpoint zones */
    public Vector[] CheckpointStartZone { get; } = Enumerable.Repeat(0, 99).Select(x => new Vector(0, 0, 0)).ToArray();

    public ReplayManager ReplayManager { get; set; } = null!;

    private readonly ILogger<Map> _logger;

    // Constructor
    internal Map(string name)
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<Map>>();

        // Set map name
        this.Name = name;

        // Initialize WR variables
        foreach (int style in Config.Styles)
        {
            this.WR[style] = new PersonalBest();
            this.MapCompletions[style] = -1;
        }

        for (int i = 0; i < 32; i++)
        {
            this.BonusWR[i] = new Dictionary<int, PersonalBest>();
            this.BonusWR[i][0] = new PersonalBest();
            this.BonusWR[i][0].Type = 1;
            this.BonusCompletions[i] = new Dictionary<int, int>();

            this.StageWR[i] = new Dictionary<int, PersonalBest>();
            this.StageWR[i][0] = new PersonalBest();
            this.StageWR[i][0].Type = 2;
            this.StageCompletions[i] = new Dictionary<int, int>();
        }
    }

    public static async Task<Map> CreateAsync(string name)
    {
        var map = new Map(name);
        await map.InitializeAsync();
        return map;
    }

    private async Task InitializeAsync([CallerMemberName] string methodName = "")
    {
        // Load zones
        Map_Load_Zones();
        _logger.LogInformation("[{ClassName}] {MethodName} -> Zones have been loaded. | Bonuses: {Bonuses} | Stages: {Stages} | Checkpoints: {Checkpoints}",
            nameof(Map), methodName, this.Bonuses, this.Stages, this.TotalCheckpoints
        );

        // Initialize ReplayManager with placeholder values
        // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> InitializeAsync -> Initializing ReplayManager(-1, {this.Stages > 0}, false, null!)");
        this.ReplayManager = new ReplayManager(-1, this.Stages > 0, this.Bonuses > 0, null!); // Adjust values as needed

        // Start timing
        var stopwatch = Stopwatch.StartNew();
        await Get_Map_Info();
        stopwatch.Stop();

        _logger.LogInformation("[{ClassName}] {MethodName} -> We got MapID = {ID} ({Name}) in {ElapsedMilliseconds}ms | API = {API}",
            nameof(Map), methodName, ID, Name, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly()
        );
    }

    /// <summary>
    /// Loops through all the hookzones found in the map and loads the respective zones
    /// </summary>
    // To-do: This loops through all the triggers. While that's great and comprehensive, some maps have two triggers with the exact same name, because there are two
    //        for each side of the course (left and right, for example). We should probably work on automatically catching this. 
    //        Maybe even introduce a new naming convention?
    internal void Map_Load_Zones([CallerMemberName] string methodName = "")
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
                    this.TotalCheckpoints++; // Might be useful to have this in DB entry
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

        _logger.LogTrace("[{ClassName}] {MethodName} -> Start zone: {StartZoneX},{StartZoneY},{StartZoneZ} | End zone: {EndZoneX},{EndZoneY},{EndZoneZ}",
            nameof(Map), methodName, this.StartZone.X, this.StartZone.Y, this.StartZone.Z, this.EndZone.X, this.EndZone.Y, this.EndZone.Z
        );

        KillServerCommandEnts();
    }

    /// <summary>
    /// Inserts a new map entry in the database.
    /// Automatically detects whether to use API Calls or MySQL query.
    /// </summary>
    internal async Task Insert_Map_Info([CallerMemberName] string methodName = "")
    {
        if (Config.API.GetApiOnly()) // API Calls
        {
            API_MapInfo inserted = new()
            {
                id = -1, // Shouldn't really use this at all at api side
                name = Name,
                author = "Unknown",
                tier = this.Tier,
                stages = this.Stages,
                bonuses = this.Bonuses,
                ranked = 0,
            };

            var postResponse = await ApiMethod.POST(Config.API.Endpoints.ENDPOINT_MAP_INSERT_INFO, inserted);

            // Check if the response is not null and get the last_id
            if (postResponse != null)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> Insert_Map_Info -> New map '{Name}' inserted, got ID {MapID}",
                    nameof(Map), methodName, Name, postResponse.last_id
                );
                this.ID = postResponse.last_id;
            }

            return;
        }
        else // MySQL Queries
        {
            int writerRows = await SurfTimer.DB.WriteAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_MAP_INSERT_INFO, MySqlHelper.EscapeString(Name), "Unknown", this.Stages, this.Bonuses, 0, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            if (writerRows != 1)
            {
                _logger.LogCritical("[{ClassName}] {MethodName} -> Insert_Map_Info -> Failed to write new map to database, this shouldn't happen. Map: {Name}",
                    nameof(Map), methodName, Name
                );
                Exception exception = new($"CS2 Surf ERROR >> internal class Map -> internal async Task Insert_Map_Info -> Failed to write new map to database, this shouldn't happen. Map: {Name}");
                throw exception;
            }

            await Get_Map_Info(false);
        }
    }

    /// <summary>
    /// Updates last played, stages, bonuses for the map in the database.
    /// Automatically detects whether to use API Calls or MySQL query.
    /// </summary>
    internal async Task Update_Map_Info([CallerMemberName] string methodName = "")
    {
        if (Config.API.GetApiOnly()) // API Calls
        {
            API_MapInfo updated = new()
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

            _ = ApiMethod.PUT(Config.API.Endpoints.ENDPOINT_MAP_UPDATE_INFO, updated).Result;
        }
        else // MySQL Queries
        {
            // Update the map's last played data in the DB
            string updateQuery = string.Format(Config.MySQL.Queries.DB_QUERY_MAP_UPDATE_INFO_FULL,
                (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), this.Stages, this.Bonuses, this.ID);

#if DEBUG
            _logger.LogDebug("[{ClassName}] {MethodName} -> Update MapData: {Query}",
                nameof(Map), methodName, updateQuery
            );
#endif

            int lastPlayedUpdateRows = await SurfTimer.DB.WriteAsync(updateQuery);
            if (lastPlayedUpdateRows != 1)
            {
                _logger.LogCritical("[{ClassName}] {MethodName} -> Update_Map_Info -> Failed to update map in database, this shouldn't happen. Map: {Name}",
                    nameof(Map), methodName, Name
                );
                Exception exception = new($"CS2 Surf ERROR >> internal class Map -> internal async Task Update_Map_Info -> Failed to update map in database, this shouldn't happen. Map: {Name}");
                throw exception;
            }
        }
    }

    /// <summary>
    /// Load map info data using MySQL Queries and update the info as well or create a new entry.
    /// Loads the record runs for the map as well.
    /// Automatically detects whether to use API Calls or MySQL query.
    /// </summary>
    /// <param name="updateData" cref="bool">Should we run UPDATE query for the map</param>
    internal async Task Get_Map_Info(bool updateData = true, [CallerMemberName] string methodName = "")
    {
        bool newMap = false;

        if (Config.API.GetApiOnly()) // API Calls
        {
            // Gather map information OR create entry
            var mapinfo = await ApiMethod.GET<API_MapInfo>(string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_INFO, Name));
            if (mapinfo != null)
            {
                this.ID = mapinfo.id;
                this.Author = mapinfo.author;
                this.Tier = mapinfo.tier;
                this.Ranked = mapinfo.ranked == 1;
                this.DateAdded = (int)mapinfo.date_added!;
                this.LastPlayed = (int)mapinfo.last_played!;
            }
            else
            {
                newMap = true;
            }
        }
        else // MySQL queries
        {
            // Gather map information OR create entry
            using (var mapData = await SurfTimer.DB.QueryAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_MAP_GET_INFO, MySqlHelper.EscapeString(Name))))
            {
                if (mapData.HasRows && mapData.Read()) // In here we can check whether MapData in DB is the same as the newly extracted data, if not, update it (as hookzones may have changed on map updates)
                {
                    this.ID = mapData.GetInt32("id");
                    this.Author = mapData.GetString("author") ?? "Unknown";
                    this.Tier = mapData.GetInt32("tier");
                    this.Ranked = mapData.GetBoolean("ranked");
                    this.DateAdded = mapData.GetInt32("date_added");
                    this.LastPlayed = mapData.GetInt32("last_played");
                }
                else
                {
                    newMap = true;
                }
            }
        }

        // This is a new map
        if (newMap)
        {
            await Insert_Map_Info();
            return;
        }

        // this.ReplayManager = new ReplayManager(this.ID, this.Stages > 0, this.Bonuses > 0);

        // Will skip updating the data in the case where we have just inserted a new map with MySQL Queries and called this method again in order to get the Map ID
        if (updateData)
            await Update_Map_Info();

        var stopwatch = Stopwatch.StartNew();
        await Get_Map_Record_Runs();
        stopwatch.Stop();
#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Finished Get_Map_Record_Runs in {ElapsedMilliseconds}ms | API = {API}",
            nameof(Map), methodName, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly()
        );
#endif
    }

    /// <summary>
    /// Extracts Map, Bonus, Stage record runs and the total completions for each style. 
    /// (NOT TESTED WITH MORE THAN 1 STYLE)
    /// For the Map WR it also gets the Checkpoints data.
    /// Automatically detects whether to use API Calls or MySQL query.
    /// TODO: Re-do the API with the new query and fix the API assign of values
    /// </summary>
    internal async Task Get_Map_Record_Runs([CallerMemberName] string methodName = "")
    {
        int totalMapRuns = 0;
        int totalStageRuns = 0;
        int totalBonusRuns = 0;
        this.ConnectedMapTimes.Clear();

        int qType;
        int qStage;
        int qStyle;

        // Replay Stuff
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() } };

        if (Config.API.GetApiOnly()) // Need to update the query in API and re-do the assigning of data
        {
            // // var maptimes = await ApiMethod.GET<API_MapTime[]>(string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_RUNS, this.ID, style, type));
            // var maptimes = await ApiMethod.GET<API_MapTime[]>(string.Format(Config.API.Endpoints.ENDPOINT_MAP_GET_RUNS, this.ID, 0, 0));
            // if (maptimes == null)
            // {
            //     Console.WriteLine($"======= CS2 Surf DEBUG API >> public async Task Get_Map_Record_Runs -> No map runs found for {this.Name} (MapID {this.ID} | Style {qStyle} | Type {qStyle})");
            //     this.MapCompletions[qStyle] = 0;
            //     return;
            // }

            // Console.WriteLine($"======= CS2 Surf DEBUG API >> public async Task Get_Map_Record_Runs -> Got {maptimes.Length} map runs for MapID {this.ID} (Style {qStyle} | Type {qStyle})");
            // // To-do: Implement bonuses WR
            // // To-do: Implement stages WR
            // foreach (var time in maptimes)
            // {
            //     if (totalMapRuns == 0) // First row is always the fastest run for the map, style, type combo
            //     {
            //         this.WR[qStyle].ID = time.id; // WR ID for the Map and Style combo
            //         this.WR[qStyle].Ticks = time.run_time; // Fastest run time (WR) for the Map and Style combo
            //         this.WR[qStyle].StartVelX = time.start_vel_x; // Fastest run start velocity X for the Map and Style combo
            //         this.WR[qStyle].StartVelY = time.start_vel_y; // Fastest run start velocity Y for the Map and Style combo
            //         this.WR[qStyle].StartVelZ = time.start_vel_z; // Fastest run start velocity Z for the Map and Style combo
            //         this.WR[qStyle].EndVelX = time.end_vel_x; // Fastest run end velocity X for the Map and Style combo
            //         this.WR[qStyle].EndVelY = time.end_vel_y; // Fastest run end velocity Y for the Map and Style combo
            //         this.WR[qStyle].EndVelZ = time.end_vel_z; // Fastest run end velocity Z for the Map and Style combo
            //         this.WR[qStyle].RunDate = time.run_date; // Fastest run date for the Map and Style combo
            //         this.WR[qStyle].Name = time.name; // Fastest run player name for the Map and Style combo
            //     }
            //     this.ConnectedMapTimes.Add(time.id);
            //     totalMapRuns++;
            // }
            // // this.ConnectedMapTimes.Remove(this.WR[style].ID); // ??
            // // this.MapCompletions[style] = maptimes.Length;
        }
        else // MySQL Queries
        {
            // Get map world record
            using (var mapWrData = await SurfTimer.DB.QueryAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_MAP_GET_RECORD_RUNS_AND_COUNT, this.ID)))
            {
                if (mapWrData.HasRows)
                {
                    while (mapWrData.Read())
                    {
                        qType = mapWrData.GetInt32("type");
                        qStage = mapWrData.GetInt32("stage");
                        qStyle = mapWrData.GetInt32("style");

                        // Retrieve replay_frames as string from MySQL
                        string replayFramesBase64;

                        // Option A: Try to get the string directly
                        try
                        {
                            replayFramesBase64 = mapWrData.GetString("replay_frames");
                        }
                        catch (InvalidCastException)
                        {
                            // Option B: Get the data as byte[] and convert to string
                            byte[] replayFramesData = mapWrData.GetFieldValue<byte[]>("replay_frames");
                            replayFramesBase64 = System.Text.Encoding.UTF8.GetString(replayFramesData);
                        }

                        // Populate parameters for all the MapTime rows found
                        switch (qType)
                        {
                            case 0: // Map WR data and total completions
                                this.WR[qStyle].ID = mapWrData.GetInt32("id");
                                this.WR[qStyle].Ticks = mapWrData.GetInt32("run_time");
                                this.WR[qStyle].StartVelX = mapWrData.GetFloat("start_vel_x");
                                this.WR[qStyle].StartVelY = mapWrData.GetFloat("start_vel_y");
                                this.WR[qStyle].StartVelZ = mapWrData.GetFloat("start_vel_z");
                                this.WR[qStyle].EndVelX = mapWrData.GetFloat("end_vel_x");
                                this.WR[qStyle].EndVelY = mapWrData.GetFloat("end_vel_y");
                                this.WR[qStyle].EndVelZ = mapWrData.GetFloat("end_vel_z");
                                this.WR[qStyle].RunDate = mapWrData.GetInt32("run_date");
                                this.WR[qStyle].Name = mapWrData.GetString("name");
                                totalMapRuns = mapWrData.GetInt32("total_count");
                                this.ConnectedMapTimes.Add(mapWrData.GetInt32("id"));
                                this.MapCompletions[qStyle] = totalMapRuns;

                                // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> internal async Task Get_Map_Record_Runs -> [MapWR] Sending style {qStyle} to `ReplayManager`: Map ID {this.ID} | Stages {this.Stages > 0} | Bonuses {this.Bonuses > 0} | Run Time {this.WR[qStyle].Ticks} | Name {this.WR[qStyle].Name} | MapTime ID {this.WR[qStyle].ID}");

                                // Populate the ReplayManager for Map WR only if no replay exists or a new WR was set
                                if (this.ReplayManager.MapWR.MapID == -1 || this.WR[qStyle].Ticks < this.ReplayManager.MapWR.RecordRunTime)
                                {
                                    Set_Replay_Data(qType, qStyle, qStage, replayFramesBase64);
                                }
                                break;
                            case 1: // Bonus WR data and total completions
                                this.BonusWR[qStage][qStyle].ID = mapWrData.GetInt32("id");
                                this.BonusWR[qStage][qStyle].Ticks = mapWrData.GetInt32("run_time");
                                this.BonusWR[qStage][qStyle].StartVelX = mapWrData.GetFloat("start_vel_x");
                                this.BonusWR[qStage][qStyle].StartVelY = mapWrData.GetFloat("start_vel_y");
                                this.BonusWR[qStage][qStyle].StartVelZ = mapWrData.GetFloat("start_vel_z");
                                this.BonusWR[qStage][qStyle].EndVelX = mapWrData.GetFloat("end_vel_x");
                                this.BonusWR[qStage][qStyle].EndVelY = mapWrData.GetFloat("end_vel_y");
                                this.BonusWR[qStage][qStyle].EndVelZ = mapWrData.GetFloat("end_vel_z");
                                this.BonusWR[qStage][qStyle].RunDate = mapWrData.GetInt32("run_date");
                                this.BonusWR[qStage][qStyle].Name = mapWrData.GetString("name");
                                totalBonusRuns = mapWrData.GetInt32("total_count");
                                this.BonusCompletions[qStage][qStyle] = totalBonusRuns;

                                // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> internal async Task Get_Map_Record_Runs -> Got Bonus {qStage}");

                                // Populate the ReplayManager for all bonuses found and set the first bonus to replay
                                if (this.ReplayManager.BonusWR != null)
                                {
                                    Set_Replay_Data(qType, qStyle, qStage, replayFramesBase64);
                                }
                                break;
                            case 2: // Stage WR data and total completions
                                this.StageWR[qStage][qStyle].ID = mapWrData.GetInt32("id");
                                this.StageWR[qStage][qStyle].Ticks = mapWrData.GetInt32("run_time");
                                this.StageWR[qStage][qStyle].StartVelX = mapWrData.GetFloat("start_vel_x");
                                this.StageWR[qStage][qStyle].StartVelY = mapWrData.GetFloat("start_vel_y");
                                this.StageWR[qStage][qStyle].StartVelZ = mapWrData.GetFloat("start_vel_z");
                                this.StageWR[qStage][qStyle].EndVelX = mapWrData.GetFloat("end_vel_x");
                                this.StageWR[qStage][qStyle].EndVelY = mapWrData.GetFloat("end_vel_y");
                                this.StageWR[qStage][qStyle].EndVelZ = mapWrData.GetFloat("end_vel_z");
                                this.StageWR[qStage][qStyle].RunDate = mapWrData.GetInt32("run_date");
                                this.StageWR[qStage][qStyle].Name = mapWrData.GetString("name");
                                totalStageRuns = mapWrData.GetInt32("total_count");
                                this.StageCompletions[qStage][qStyle] = totalStageRuns;

                                // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> internal async Task Get_Map_Record_Runs -> [StageWR] Sending style {qStyle} to `ReplayManager.StageWR`: Map ID {this.ID} | Stages {this.Stages > 0} - {qStage} | Bonuses {this.Bonuses > 0} | Run Time {this.WR[qStyle].Ticks} | Name {this.WR[qStyle].Name} | MapTime ID {this.WR[qStyle].ID}");

                                // Populate the ReplayManager for all stages found and set the first stage to replay
                                if (this.ReplayManager.StageWR != null)
                                {
                                    Set_Replay_Data(qType, qStyle, qStage, replayFramesBase64);
                                }
                                break;
                        }

                        // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> internal async Task Get_Map_Record_Runs -> Map Completions for style {qStyle} {this.MapCompletions[qStyle]}");
                    }
                }
            }
        }

        // Retrieve the checkpoints for each Style if it has been set.
        foreach (int style in Config.Styles)
        {
            // if (this.MapCompletions[style] > 0 && this.WR[style].ID != -1 && this.WR[style].Ticks < this.ReplayManager.MapWR.RecordRunTime) // This should also reload Checkpoints if a new MapWR is set
            if (
                this.MapCompletions[style] > 0 && this.WR[style].ID != -1 ||
                this.WR[style].ID != -1 && this.WR[style].Ticks < this.ReplayManager.MapWR.RecordRunTime
            ) // This should also reload Checkpoints if a new MapWR is set
            {
#if DEBUG
                _logger.LogDebug("[{ClassName}] {MethodName} -> Get_Map_Record_Runs : Map -> [{DBorAPI}] Loaded {MapCompletions} runs (MapID {MapID} | Style {Style}). WR by {PlayerName} - {Time}",
                    nameof(Map), methodName, (Config.API.GetApiOnly() ? "API" : "DB"), this.MapCompletions[style], this.ID, style, this.WR[style].Name, PlayerHUD.FormatTime(this.WR[style].Ticks)
                );
#endif

                var stopwatch = Stopwatch.StartNew();
                await Get_Record_Run_Checkpoints(style);
                stopwatch.Stop();

                _logger.LogInformation("[{ClassName}] {MethodName} -> Finished Get_Record_Run_Checkpoints({Style}) in {ElapsedMilliseconds}ms | API = {API}",
                    nameof(Map), methodName, style, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly()
                );
            }
        }
    }

    /// <summary>
    /// Redirects to `PersonalBest.PB_LoadCheckpointsData()`.
    /// Extracts all entries from Checkpoints table of the World Record for the given `style` 
    /// </summary>
    /// <param name="style">Style to load</param>
    internal async Task Get_Record_Run_Checkpoints(int style = 0)
    {
        await this.WR[style].PB_LoadCheckpointsData();
    }

    /// <summary>
    /// Sets the data for a replay that has been retrieved from MapTimes data.
    /// Also sets the first Stage replay if no replays existed for stages until now.
    /// </summary>
    /// <param name="type">Type - 0 = Map, 1 = Bonus, 2 = Stage</param>
    /// <param name="style">Style to add</param>
    /// <param name="stage">Stage to add</param>
    /// <param name="replayFramesBase64">Base64 encoded string for the replay_frames</param>
    internal void Set_Replay_Data(int type, int style, int stage, string replayFramesBase64, [CallerMemberName] string methodName = "")
    {
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false, Converters = { new VectorConverter(), new QAngleConverter() } };

        // Decompress the Base64 string
        string json = Compressor.Decompress(replayFramesBase64);

        // Deserialize to List<ReplayFrame>
        List<ReplayFrame> frames = JsonSerializer.Deserialize<List<ReplayFrame>>(json, options)!;

        switch (type)
        {
            case 0: // Map Replays
                // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> internal void Set_Replay_Data -> [MapWR] Setting run {this.WR[style].ID} {PlayerHUD.FormatTime(this.WR[style].Ticks)} (Ticks = {this.WR[style].Ticks}; Frames = {frames.Count}) to `ReplayManager.MapWR`");
                if (this.ReplayManager.MapWR.IsPlaying)
                    this.ReplayManager.MapWR.Stop();

                this.ReplayManager.MapWR.RecordPlayerName = this.WR[style].Name;
                this.ReplayManager.MapWR.RecordRunTime = this.WR[style].Ticks;
                this.ReplayManager.MapWR.Frames = frames;
                this.ReplayManager.MapWR.MapTimeID = this.WR[style].ID;
                this.ReplayManager.MapWR.MapID = this.ID;
                this.ReplayManager.MapWR.Type = 0;
                for (int i = 0; i < frames.Count; i++) // Load the situations for the replay
                {
                    ReplayFrame f = frames[i];
                    switch (f.Situation)
                    {
                        case ReplayFrameSituation.START_ZONE_ENTER:
                            this.ReplayManager.MapWR.MapSituations.Add(i);
                            // Console.WriteLine($"START_ZONE_ENTER: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.START_ZONE_EXIT:
                            this.ReplayManager.MapWR.MapSituations.Add(i);
                            // Console.WriteLine($"START_ZONE_EXIT: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.STAGE_ZONE_ENTER:
                            this.ReplayManager.MapWR.StageEnterSituations.Add(i);
                            // Console.WriteLine($"STAGE_ZONE_ENTER: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.STAGE_ZONE_EXIT:
                            this.ReplayManager.MapWR.StageExitSituations.Add(i);
                            // Console.WriteLine($"STAGE_ZONE_EXIT: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.CHECKPOINT_ZONE_ENTER:
                            this.ReplayManager.MapWR.CheckpointEnterSituations.Add(i);
                            // Console.WriteLine($"CHECKPOINT_ZONE_ENTER: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.CHECKPOINT_ZONE_EXIT:
                            this.ReplayManager.MapWR.CheckpointExitSituations.Add(i);
                            // Console.WriteLine($"CHECKPOINT_ZONE_EXIT: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.END_ZONE_ENTER:
                            // Console.WriteLine($"END_ZONE_ENTER: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.END_ZONE_EXIT:
                            // Console.WriteLine($"END_ZONE_EXIT: {i} | Situation {f.Situation}");
                            break;
                    }
                }
                break;
            case 1: // Bonus Replays
                // Skip if the same bonus run already exists
                if (this.ReplayManager.AllBonusWR[stage][style].RecordRunTime == this.BonusWR[stage][style].Ticks)
                    break;
#if DEBUG
                _logger.LogDebug("[{ClassName}] {MethodName} -> Set_Replay_Data -> [BonusWR] Adding run {ID} {Time} (Ticks = {Ticks}; Frames = {Frames}) to `ReplayManager.AllBonusWR`",
                    nameof(Map), methodName, this.BonusWR[stage][style].ID, PlayerHUD.FormatTime(this.BonusWR[stage][style].Ticks), this.BonusWR[stage][style].Ticks, frames.Count
                );
#endif

                // Add all stages found to a dictionary with their data
                this.ReplayManager.AllBonusWR[stage][style].MapID = this.ID;
                this.ReplayManager.AllBonusWR[stage][style].Frames = frames;
                this.ReplayManager.AllBonusWR[stage][style].RecordRunTime = this.BonusWR[stage][style].Ticks;
                this.ReplayManager.AllBonusWR[stage][style].RecordPlayerName = this.BonusWR[stage][style].Name;
                this.ReplayManager.AllBonusWR[stage][style].MapTimeID = this.BonusWR[stage][style].ID;
                this.ReplayManager.AllBonusWR[stage][style].Stage = stage;
                this.ReplayManager.AllBonusWR[stage][style].Type = 1;
                this.ReplayManager.AllBonusWR[stage][style].RecordRank = 1;
                this.ReplayManager.AllBonusWR[stage][style].IsPlayable = true; // We set this to `true` else we overwrite it and need to call SetController method again
                for (int i = 0; i < frames.Count; i++)
                {
                    ReplayFrame f = frames[i];
                    switch (f.Situation)
                    {
                        case ReplayFrameSituation.START_ZONE_ENTER:
                            this.ReplayManager.AllBonusWR[stage][style].BonusSituations.Add(i);
                            break;
                        case ReplayFrameSituation.END_ZONE_EXIT:
                            this.ReplayManager.AllBonusWR[stage][style].BonusSituations.Add(i);
                            break;
                    }
                }
                // Set the bonus to replay first
                if (this.ReplayManager.BonusWR != null && this.ReplayManager.BonusWR.MapID == -1)
                {
#if DEBUG
                    _logger.LogDebug("[{ClassName}] {MethodName} -> [BonusWR] Setting first `ReplayManager.BonusWR` to bonus {stage}",
                        nameof(Map), methodName, stage
                    );
#endif
                    if (this.ReplayManager.BonusWR.IsPlaying) // Maybe only stop the replay if we are overwriting the current bonus being played?
                        this.ReplayManager.BonusWR.Stop();
                    this.ReplayManager.BonusWR.MapID = this.ID;
                    this.ReplayManager.BonusWR.Frames = frames;
                    this.ReplayManager.BonusWR.RecordRunTime = this.BonusWR[stage][style].Ticks;
                    this.ReplayManager.BonusWR.RecordPlayerName = this.BonusWR[stage][style].Name;
                    this.ReplayManager.BonusWR.MapTimeID = this.BonusWR[stage][style].ID;
                    this.ReplayManager.BonusWR.Stage = stage;
                    this.ReplayManager.BonusWR.Type = 1;
                    this.ReplayManager.BonusWR.RecordRank = 1;
                }
                break;
            case 2: // Stage Replays
                // Skip if the same stage run already exists
                if (this.ReplayManager.AllStageWR[stage][style].RecordRunTime == this.StageWR[stage][style].Ticks)
                    break;
#if DEBUG
                _logger.LogDebug("[{ClassName}] {MethodName} -> Set_Replay_Data -> [StageWR] Adding run {ID} {Time} (Ticks = {Ticks}; Frames = {Frames}) to `ReplayManager.AllStageWR`",
                    nameof(Map), methodName, this.StageWR[stage][style].ID, PlayerHUD.FormatTime(this.StageWR[stage][style].Ticks), this.StageWR[stage][style].Ticks, frames.Count
                );
#endif

                // Add all stages found to a dictionary with their data
                this.ReplayManager.AllStageWR[stage][style].MapID = this.ID;
                this.ReplayManager.AllStageWR[stage][style].Frames = frames;
                this.ReplayManager.AllStageWR[stage][style].RecordRunTime = this.StageWR[stage][style].Ticks;
                this.ReplayManager.AllStageWR[stage][style].RecordPlayerName = this.StageWR[stage][style].Name;
                this.ReplayManager.AllStageWR[stage][style].MapTimeID = this.StageWR[stage][style].ID;
                this.ReplayManager.AllStageWR[stage][style].Stage = stage;
                this.ReplayManager.AllStageWR[stage][style].Type = 2;
                this.ReplayManager.AllStageWR[stage][style].RecordRank = 1;
                this.ReplayManager.AllStageWR[stage][style].IsPlayable = true; // We set this to `true` else we overwrite it and need to call SetController method again
                for (int i = 0; i < frames.Count; i++)
                {
                    ReplayFrame f = frames[i];
                    switch (f.Situation)
                    {
                        case ReplayFrameSituation.STAGE_ZONE_ENTER:
                            this.ReplayManager.AllStageWR[stage][style].StageEnterSituations.Add(i);
                            break;
                        case ReplayFrameSituation.STAGE_ZONE_EXIT:
                            this.ReplayManager.AllStageWR[stage][style].StageExitSituations.Add(i);
                            break;
                    }
                }
                // Set the stage to replay first
                if (this.ReplayManager.StageWR != null && this.ReplayManager.StageWR.MapID == -1)
                {
#if DEBUG
                    _logger.LogDebug("[{ClassName}] {MethodName} -> [StageWR] Setting first `ReplayManager.StageWR` to stage {stage}",
                        nameof(Map), methodName, stage
                    );
#endif

                    if (this.ReplayManager.StageWR.IsPlaying) // Maybe only stop the replay if we are overwriting the current stage being played?
                        this.ReplayManager.StageWR.Stop();
                    this.ReplayManager.StageWR.MapID = this.ID;
                    this.ReplayManager.StageWR.Frames = frames;
                    this.ReplayManager.StageWR.RecordRunTime = this.StageWR[stage][style].Ticks;
                    this.ReplayManager.StageWR.RecordPlayerName = this.StageWR[stage][style].Name;
                    this.ReplayManager.StageWR.MapTimeID = this.StageWR[stage][style].ID;
                    this.ReplayManager.StageWR.Stage = stage;
                    this.ReplayManager.StageWR.Type = 2;
                    this.ReplayManager.StageWR.RecordRank = 1;
                }
                break;
        }

        // Start the new map replay if none existed until now
        if (type == 0 && this.ReplayManager.MapWR != null && !this.ReplayManager.MapWR.IsPlaying)
        {
            // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> internal void Set_Replay_Data -> [MapWR] ResetReplay() and Start()");
            this.ReplayManager.MapWR.ResetReplay();
            this.ReplayManager.MapWR.Start();
        }
        else if (type == 1 && this.ReplayManager.BonusWR != null && !this.ReplayManager.BonusWR.IsPlaying)
        {
            // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> internal void Set_Replay_Data -> [BonusWR] ResetReplay() and Start() {stage}");
            this.ReplayManager.BonusWR.ResetReplay();
            this.ReplayManager.BonusWR.Start();
        }
        else if (type == 2 && this.ReplayManager.StageWR != null && !this.ReplayManager.StageWR.IsPlaying)
        {
            // Console.WriteLine($"CS2 Surf DEBUG >> internal class Map -> internal void Set_Replay_Data -> [StageWR] ResetReplay() and Start() {stage}");
            this.ReplayManager.StageWR.ResetReplay();
            this.ReplayManager.StageWR.Start();
        }
    }

    public void KickReplayBot(int index)
    {
        if (!this.ReplayManager.CustomReplays[index].IsPlayable)
            return;

        int? id_to_kick = this.ReplayManager.CustomReplays[index].Controller!.UserId;
        if (id_to_kick == null)
            return;

        this.ReplayManager.CustomReplays.RemoveAt(index);
        Server.ExecuteCommand($"kickid {id_to_kick}; bot_quota {this.ReplayManager.CustomReplays.Count}");
    }

    public static bool IsInZone(Vector zoneOrigin, float zoneCollisionRadius, Vector spawnOrigin)
    {
        if (spawnOrigin.X >= zoneOrigin.X - zoneCollisionRadius && spawnOrigin.X <= zoneOrigin.X + zoneCollisionRadius &&
            spawnOrigin.Y >= zoneOrigin.Y - zoneCollisionRadius && spawnOrigin.Y <= zoneOrigin.Y + zoneCollisionRadius &&
            spawnOrigin.Z >= zoneOrigin.Z - zoneCollisionRadius && spawnOrigin.Z <= zoneOrigin.Z + zoneCollisionRadius)
            return true;
        else
            return false;
    }

    private void KillServerCommandEnts([CallerMemberName] string methodName = "")
    {
        var pointServerCommands = Utilities.FindAllEntitiesByDesignerName<CPointServerCommand>("point_servercommand");

        foreach (var servercmd in pointServerCommands)
        {
            if (servercmd == null) continue;
            _logger.LogTrace("[{ClassName}] {MethodName} -> Killed point_servercommand ent: {ServerCMD}", nameof(Map), methodName, servercmd.Handle);
            servercmd.Remove();
        }
    }
}
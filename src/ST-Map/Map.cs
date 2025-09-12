using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Data;
using SurfTimer.Shared.DTO;
using SurfTimer.Shared.Entities;
using SurfTimer.Shared.Types;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SurfTimer;

public class Map : MapEntity
{
    public int TotalCheckpoints { get; set; } = 0;
    /// <summary>
    /// Map Completion Count - Refer to as MapCompletions[style]
    /// </summary>
    public Dictionary<int, int> MapCompletions { get; set; } = new Dictionary<int, int>();
    /// <summary>
    /// Bonus Completion Count - Refer to as BonusCompletions[bonus#][style]
    /// </summary>
    public Dictionary<int, int>[] BonusCompletions { get; set; } = Array.Empty<Dictionary<int, int>>();
    /// <summary>
    /// Stage Completion Count - Refer to as StageCompletions[stage#][style]
    /// </summary>
    public Dictionary<int, int>[] StageCompletions { get; set; } = Array.Empty<Dictionary<int, int>>();
    /// <summary>
    /// Map World Record - Refer to as WR[style]
    /// </summary>
    public Dictionary<int, PersonalBest> WR { get; set; } = new Dictionary<int, PersonalBest>();
    /// <summary>
    /// Bonus World Record - Refer to as BonusWR[bonus#][style]
    /// </summary>
    public Dictionary<int, PersonalBest>[] BonusWR { get; set; } = Array.Empty<Dictionary<int, PersonalBest>>();
    /// <summary>
    /// Stage World Record - Refer to as StageWR[stage#][style]
    /// </summary>
    public Dictionary<int, PersonalBest>[] StageWR { get; set; } = Array.Empty<Dictionary<int, PersonalBest>>();

    /// <summary>
    /// Not sure what this is for.
    /// Guessing it's to do with Replays and the ability to play your PB replay.
    /// 
    /// - T
    /// </summary>
    public List<int> ConnectedMapTimes { get; set; } = new List<int>();

    // Zone Origin Information
    /* Map Start/End zones */
    public VectorT StartZone { get; set; } = new VectorT(0, 0, 0);
    public QAngleT StartZoneAngles { get; set; } = new QAngleT(0, 0, 0);
    public VectorT EndZone { get; set; } = new VectorT(0, 0, 0);
    /* Map Stage zones */
    public VectorT[] StageStartZone { get; } = Enumerable.Repeat(0, 99).Select(x => new VectorT(0, 0, 0)).ToArray();
    public QAngleT[] StageStartZoneAngles { get; } = Enumerable.Repeat(0, 99).Select(x => new QAngleT(0, 0, 0)).ToArray();
    /* Map Bonus zones */
    public VectorT[] BonusStartZone { get; } = Enumerable.Repeat(0, 99).Select(x => new VectorT(0, 0, 0)).ToArray(); // To-do: Implement bonuses
    public QAngleT[] BonusStartZoneAngles { get; } = Enumerable.Repeat(0, 99).Select(x => new QAngleT(0, 0, 0)).ToArray(); // To-do: Implement bonuses
    public VectorT[] BonusEndZone { get; } = Enumerable.Repeat(0, 99).Select(x => new VectorT(0, 0, 0)).ToArray(); // To-do: Implement bonuses
    /* Map Checkpoint zones */
    public VectorT[] CheckpointStartZone { get; } = Enumerable.Repeat(0, 99).Select(x => new VectorT(0, 0, 0)).ToArray();

    public ReplayManager ReplayManager { get; set; } = null!;

    private readonly ILogger<Map> _logger;
    private readonly IDataAccessService _dataService;

    // Constructor
    internal Map(string name)
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<Map>>();
        _dataService = SurfTimer.ServiceProvider.GetRequiredService<IDataAccessService>();

        // Set map name
        this.Name = name;

        // Load zones
        MapLoadZones();
        _logger.LogInformation("[{ClassName}] -> Zones have been loaded. | Bonuses: {Bonuses} | Stages: {Stages} | Checkpoints: {Checkpoints}",
            nameof(Map), this.Bonuses, this.Stages, this.TotalCheckpoints
        );
    }

    internal async Task InitializeAsync([CallerMemberName] string methodName = "")
    {
        // Initialize ReplayManager with placeholder values
        this.ReplayManager = new ReplayManager(-1, this.Stages > 0, this.Bonuses > 0, null!);

        // Initialize WR variables
        this.StageWR = new Dictionary<int, PersonalBest>[this.Stages + 1]; // We do + 1 cause stages and bonuses start from 1, not from 0
        this.StageCompletions = new Dictionary<int, int>[this.Stages + 1];
        this.BonusWR = new Dictionary<int, PersonalBest>[this.Bonuses + 1];
        this.BonusCompletions = new Dictionary<int, int>[this.Bonuses + 1];
        int initStages = 0;
        int initBonuses = 0;

        foreach (int style in Config.Styles)
        {
            this.WR[style] = new PersonalBest { Type = 0 };
            this.MapCompletions[style] = 0;

            for (int i = 1; i <= this.Stages; i++)
            {
                this.StageWR[i] = new Dictionary<int, PersonalBest>();
                this.StageWR[i][style] = new PersonalBest { Type = 2 };
                this.StageCompletions[i] = new Dictionary<int, int>();
                this.StageCompletions[i][style] = 0;
                initStages++;
            }

            for (int i = 1; i <= this.Bonuses; i++)
            {
                this.BonusWR[i] = new Dictionary<int, PersonalBest>();
                this.BonusWR[i][style] = new PersonalBest { Type = 1 };
                this.BonusCompletions[i] = new Dictionary<int, int>();
                this.BonusCompletions[i][style] = 0;
                initBonuses++;
            }
        }

        _logger.LogInformation("[{ClassName}] {MethodName} -> Initialized WR variables. | Bonuses: {Bonuses} | Stages: {Stages}",
            nameof(Map), methodName, initBonuses, initStages
        );

        await LoadMapInfo();
    }

    /// <summary>
    /// Loops through all the hookzones found in the map and loads the respective zones
    /// </summary>
    // To-do: This loops through all the triggers. While that's great and comprehensive, some maps have two triggers with the exact same name, because there are two
    //        for each side of the course (left and right, for example). We should probably work on automatically catching this. 
    //        Maybe even introduce a new naming convention?
    internal void MapLoadZones([CallerMemberName] string methodName = "")
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
                            this.StartZone = new VectorT(teleport.AbsOrigin!.X, teleport.AbsOrigin!.Y, teleport.AbsOrigin!.Z);
                            this.StartZoneAngles = new QAngleT(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                            foundPlayerSpawn = true;
                            break;
                        }
                    }

                    if (!foundPlayerSpawn)
                    {
                        this.StartZone = new VectorT(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                    }
                }

                // Map end zone
                else if (trigger.Entity!.Name.Contains("map_end"))
                {
                    this.EndZone = new VectorT(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
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
                            this.StageStartZone[stage] = new VectorT(teleport.AbsOrigin!.X, teleport.AbsOrigin!.Y, teleport.AbsOrigin!.Z);
                            this.StageStartZoneAngles[stage] = new QAngleT(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                            this.Stages++; // Count stage zones for the map to populate DB
                            foundPlayerSpawn = true;
                            break;
                        }
                    }

                    if (!foundPlayerSpawn)
                    {
                        this.StageStartZone[stage] = new VectorT(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                        this.Stages++;
                    }
                }

                // Checkpoint start zones (linear maps)
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
                {
                    this.CheckpointStartZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value)] = new VectorT(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
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
                            this.BonusStartZone[bonus] = new VectorT(teleport.AbsOrigin!.X, teleport.AbsOrigin!.Y, teleport.AbsOrigin!.Z);
                            this.BonusStartZoneAngles[bonus] = new QAngleT(teleport.AbsRotation!.X, teleport.AbsRotation!.Y, teleport.AbsRotation!.Z);
                            this.Bonuses++; // Count bonus zones for the map to populate DB
                            foundPlayerSpawn = true;
                            break;
                        }
                    }

                    if (!foundPlayerSpawn)
                    {
                        this.BonusStartZone[bonus] = new VectorT(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                        this.Bonuses++;
                    }
                }

                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_end$").Success)
                {
                    this.BonusEndZone[Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value)] = new VectorT(trigger.AbsOrigin!.X, trigger.AbsOrigin!.Y, trigger.AbsOrigin!.Z);
                }
            }
        }

        if (this.Stages > 0) // Account for stage 1, not counted above
        {
            this.TotalCheckpoints = this.Stages; // Stages are counted as Checkpoints on Staged maps during MAP runs
            this.Stages += 1;
        }

        _logger.LogTrace("[{ClassName}] {MethodName} -> Start zone: {StartZoneX}, {StartZoneY}, {StartZoneZ} | End zone: {EndZoneX}, {EndZoneY}, {EndZoneZ}",
            nameof(Map), methodName, this.StartZone.X, this.StartZone.Y, this.StartZone.Z, this.EndZone.X, this.EndZone.Y, this.EndZone.Z
        );

        KillServerCommandEnts();
    }

    /// <summary>
    /// Inserts a new map entry in the database.
    /// </summary>
    internal async Task InsertMapInfo([CallerMemberName] string methodName = "")
    {
        var mapInfo = new MapDto
        {
            Name = this.Name!,
            Author = "Unknown", // Or set appropriately
            Tier = this.Tier,
            Stages = this.Stages,
            Bonuses = this.Bonuses,
            Ranked = false
        };

        try
        {
            this.ID = await _dataService.InsertMapInfoAsync(mapInfo);

            _logger.LogInformation("[{ClassName}] {MethodName} -> Map '{Map}' inserted successfully with ID {ID}.",
                nameof(Map), methodName, this.Name, this.ID
            );
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[{ClassName}] {MethodName} -> Failed to insert map '{Map}'. Exception: {ExceptionMessage}",
                nameof(Map), methodName, this.Name, ex.Message
            );
            throw new InvalidOperationException($"Failed to insert map '{Name}'. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Updates last played, stages, bonuses for the map in the database.
    /// </summary>
    internal async Task UpdateMapInfo([CallerMemberName] string methodName = "")
    {
        var mapInfo = new MapDto
        {
            Name = this.Name!,
            Author = this.Author!,
            Tier = this.Tier,
            Stages = this.Stages,
            Bonuses = this.Bonuses,
            Ranked = this.Ranked,
            LastPlayed = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        try
        {
            await _dataService.UpdateMapInfoAsync(mapInfo, this.ID);

#if DEBUG
            _logger.LogDebug("[{ClassName}] {MethodName} -> Updated map '{Map}' (ID: {ID}).",
                nameof(Map), methodName, this.Name, this.ID
            );
#endif
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[{ClassName}] {MethodName} -> Failed to update map '{Map}'. Exception Message: {ExceptionMessage}",
                nameof(Map), methodName, this.Name, ex.Message
            );
            throw new InvalidOperationException($"Failed to update map '{Name}'. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Load/update/create Map table entry.
    /// Loads the record runs for the map as well.
    /// </summary>
    /// <param name="updateData">Should we run UPDATE query for the map</param>
    internal async Task LoadMapInfo(bool updateData = true, [CallerMemberName] string methodName = "")
    {
        bool newMap = false;

        var mapInfo = await _dataService.GetMapInfoAsync(this.Name!);

        if (mapInfo != null)
        {
            ID = mapInfo.ID;
            Author = mapInfo.Author;
            Tier = mapInfo.Tier;
            Ranked = mapInfo.Ranked;
            DateAdded = mapInfo.DateAdded;
            LastPlayed = mapInfo.LastPlayed;
        }
        else
        {
            newMap = true;
        }

        if (newMap)
        {
            await InsertMapInfo();
            return;
        }

        if (updateData)
            await UpdateMapInfo();

        var stopwatch = Stopwatch.StartNew();
        await LoadMapRecordRuns();
        stopwatch.Stop();

#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Finished LoadMapRecordRuns in {Elapsed}ms | API = {API}",
            nameof(Map), methodName, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly());
#endif
    }

    /// <summary>
    /// Extracts Map, Bonus, Stage record runs and the total completions for each style. 
    /// (NOT TESTED WITH MORE THAN 1 STYLE)
    /// For the Map WR it also gets the Checkpoints data.
    /// </summary>
    internal async Task LoadMapRecordRuns([CallerMemberName] string methodName = "")
    {
        //this.ConnectedMapTimes.Clear(); // This is for Custom Replays (PB replays?) - T

        var runs = await _dataService.GetMapRecordRunsAsync(this.ID);

        _logger.LogInformation("[{ClassName}] {MethodName} -> Received {Length} runs from `GetMapRecordRunsAsync`",
            nameof(Map), methodName, runs.Count
        );

        foreach (var run in runs)
        {
            switch (run.Type)
            {
                case 0: // Map WR data and total completions
                    WR[run.Style].ID = run.ID;
                    WR[run.Style].RunTime = run.RunTime;
                    WR[run.Style].StartVelX = run.StartVelX;
                    WR[run.Style].StartVelY = run.StartVelY;
                    WR[run.Style].StartVelZ = run.StartVelZ;
                    WR[run.Style].EndVelX = run.EndVelX;
                    WR[run.Style].EndVelY = run.EndVelY;
                    WR[run.Style].EndVelZ = run.EndVelZ;
                    WR[run.Style].RunDate = run.RunDate;
                    WR[run.Style].Name = run.Name;
                    /// ConnectedMapTimes.Add(run.ID);
                    MapCompletions[run.Style] = run.TotalCount;

                    SetReplayData(run.Type, run.Style, run.Stage, run.ReplayFrames!);
                    break;

                case 1: // Bonus WR data and total completions
                    BonusWR[run.Stage][run.Style].ID = run.ID;
                    BonusWR[run.Stage][run.Style].RunTime = run.RunTime;
                    BonusWR[run.Stage][run.Style].StartVelX = run.StartVelX;
                    BonusWR[run.Stage][run.Style].StartVelY = run.StartVelY;
                    BonusWR[run.Stage][run.Style].StartVelZ = run.StartVelZ;
                    BonusWR[run.Stage][run.Style].EndVelX = run.EndVelX;
                    BonusWR[run.Stage][run.Style].EndVelY = run.EndVelY;
                    BonusWR[run.Stage][run.Style].EndVelZ = run.EndVelZ;
                    BonusWR[run.Stage][run.Style].RunDate = run.RunDate;
                    BonusWR[run.Stage][run.Style].Name = run.Name;
                    BonusCompletions[run.Stage][run.Style] = run.TotalCount;

                    SetReplayData(run.Type, run.Style, run.Stage, run.ReplayFrames!);
                    break;

                case 2: // Stage WR data and total completions
                    StageWR[run.Stage][run.Style].ID = run.ID;
                    StageWR[run.Stage][run.Style].RunTime = run.RunTime;
                    StageWR[run.Stage][run.Style].StartVelX = run.StartVelX;
                    StageWR[run.Stage][run.Style].StartVelY = run.StartVelY;
                    StageWR[run.Stage][run.Style].StartVelZ = run.StartVelZ;
                    StageWR[run.Stage][run.Style].EndVelX = run.EndVelX;
                    StageWR[run.Stage][run.Style].EndVelY = run.EndVelY;
                    StageWR[run.Stage][run.Style].EndVelZ = run.EndVelZ;
                    StageWR[run.Stage][run.Style].RunDate = run.RunDate;
                    StageWR[run.Stage][run.Style].Name = run.Name;
                    StageCompletions[run.Stage][run.Style] = run.TotalCount;

                    SetReplayData(run.Type, run.Style, run.Stage, run.ReplayFrames!);
                    break;
            }
        }

        foreach (int style in Config.Styles)
        {
            if (MapCompletions[style] > 0 && WR[style].ID != -1)
            {
#if DEBUG
                _logger.LogDebug("[{ClassName}] {MethodName} -> LoadMapRecordRuns : Map -> [{DBorAPI}] Loaded {MapCompletions} runs (MapID {MapID} | Style {Style}). WR by {PlayerName} - {Time}",
                    nameof(Map), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.MapCompletions[style], this.ID, style, this.WR[style].Name, PlayerHUD.FormatTime(this.WR[style].RunTime)
                );
#endif

                var stopwatch = Stopwatch.StartNew();
                await this.WR[style].LoadCheckpoints(); // Load the checkpoints for the WR and Style combo
                stopwatch.Stop();

                _logger.LogInformation("[{ClassName}] {MethodName} -> Finished WR.[{Style}].LoadCheckpoints() in {ElapsedMilliseconds}ms | API = {API}",
                    nameof(Map), methodName, style, stopwatch.ElapsedMilliseconds, Config.Api.GetApiOnly()
                );
            }
        }
    }

    /// <summary>
    /// Sets the data for a replay that has been retrieved from MapTimes data.
    /// Also sets the first Stage replay if no replays existed for stages until now.
    /// </summary>
    /// <param name="type">Type - 0 = Map, 1 = Bonus, 2 = Stage</param>
    /// <param name="style">Style to add</param>
    /// <param name="stage">Stage to add</param>
    /// <param name="replayFramesBase64">Base64 encoded string for the replay_frames</param>
    internal void SetReplayData(int type, int style, int stage, ReplayFramesString replayFramesBase64, [CallerMemberName] string methodName = "")
    {
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false, Converters = { new VectorTConverter(), new QAngleTConverter() } };

        // Decompress the Base64 string
        string json = Compressor.Decompress(replayFramesBase64.ToString());

        // Deserialize to List<ReplayFrame>
        List<ReplayFrame> frames = JsonSerializer.Deserialize<List<ReplayFrame>>(json, options)!;

        switch (type)
        {
            case 0: // Map Replays
                _logger.LogTrace("[{ClassName}] {MethodName} -> SetReplayData -> [MapWR] Setting run {RunID} {RunTime} (Ticks = {RunTicks}; Frames = {TotalFrames})",
                    nameof(Map), methodName, this.WR[style].ID, PlayerHud.FormatTime(this.WR[style].RunTime), this.WR[style].RunTime, frames.Count
                );
                if (this.ReplayManager.MapWR.IsPlaying)
                    this.ReplayManager.MapWR.Stop();

                this.ReplayManager.MapWR.RecordPlayerName = this.WR[style].Name!;
                this.ReplayManager.MapWR.RecordRunTime = this.WR[style].RunTime;
                this.ReplayManager.MapWR.Frames = frames;
                this.ReplayManager.MapWR.MapTimeID = this.WR[style].ID;
                this.ReplayManager.MapWR.MapID = this.ID;
                this.ReplayManager.MapWR.Type = 0;
                for (int i = 0; i < frames.Count; i++) // Load the situations for the replay
                {
                    ReplayFrame f = frames[i];
                    switch (f.Situation)
                    {
                        case ReplayFrameSituation.START_ZONE_ENTER or ReplayFrameSituation.START_ZONE_EXIT:
                            this.ReplayManager.MapWR.MapSituations.Add(i);
                            /// Console.WriteLine($"START_ZONE_ENTER: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.STAGE_ZONE_ENTER or ReplayFrameSituation.STAGE_ZONE_EXIT:
                            this.ReplayManager.MapWR.StageEnterSituations.Add(i);
                            /// Console.WriteLine($"STAGE_ZONE_ENTER: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.CHECKPOINT_ZONE_ENTER or ReplayFrameSituation.CHECKPOINT_ZONE_EXIT:
                            this.ReplayManager.MapWR.CheckpointEnterSituations.Add(i);
                            /// Console.WriteLine($"CHECKPOINT_ZONE_ENTER: {i} | Situation {f.Situation}");
                            break;
                        case ReplayFrameSituation.END_ZONE_ENTER or ReplayFrameSituation.END_ZONE_EXIT:
                            /// Console.WriteLine($"END_ZONE_ENTER: {i} | Situation {f.Situation}");
                            break;
                    }
                }
                break;
            case 1: // Bonus Replays
                // Skip if the same bonus run already exists
                if (this.ReplayManager.AllBonusWR[stage][style].RecordRunTime == this.BonusWR[stage][style].RunTime)
                    break;
#if DEBUG
                _logger.LogDebug("[{ClassName}] {MethodName} -> SetReplayData -> [BonusWR] Adding run {ID} {Time} (Ticks = {Ticks}; Frames = {Frames}) to `ReplayManager.AllBonusWR`",
                    nameof(Map), methodName, this.BonusWR[stage][style].ID, PlayerHUD.FormatTime(this.BonusWR[stage][style].RunTime), this.BonusWR[stage][style].RunTime, frames.Count
                );
#endif

                // Add all stages found to a dictionary with their data
                this.ReplayManager.AllBonusWR[stage][style].MapID = this.ID;
                this.ReplayManager.AllBonusWR[stage][style].Frames = frames;
                this.ReplayManager.AllBonusWR[stage][style].RecordRunTime = this.BonusWR[stage][style].RunTime;
                this.ReplayManager.AllBonusWR[stage][style].RecordPlayerName = this.BonusWR[stage][style].Name!;
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
                        case ReplayFrameSituation.START_ZONE_ENTER or ReplayFrameSituation.END_ZONE_EXIT:
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
                    this.ReplayManager.BonusWR.RecordRunTime = this.BonusWR[stage][style].RunTime;
                    this.ReplayManager.BonusWR.RecordPlayerName = this.BonusWR[stage][style].Name!;
                    this.ReplayManager.BonusWR.MapTimeID = this.BonusWR[stage][style].ID;
                    this.ReplayManager.BonusWR.Stage = stage;
                    this.ReplayManager.BonusWR.Type = 1;
                    this.ReplayManager.BonusWR.RecordRank = 1;
                }
                break;
            case 2: // Stage Replays
                // Skip if the same stage run already exists
                if (this.ReplayManager.AllStageWR[stage][style].RecordRunTime == this.StageWR[stage][style].RunTime)
                    break;
#if DEBUG
                _logger.LogDebug("[{ClassName}] {MethodName} -> SetReplayData -> [StageWR] Adding run {ID} {Time} (Ticks = {Ticks}; Frames = {Frames}) to `ReplayManager.AllStageWR`",
                    nameof(Map), methodName, this.StageWR[stage][style].ID, PlayerHUD.FormatTime(this.StageWR[stage][style].RunTime), this.StageWR[stage][style].RunTime, frames.Count
                );
#endif

                // Add all stages found to a dictionary with their data
                this.ReplayManager.AllStageWR[stage][style].MapID = this.ID;
                this.ReplayManager.AllStageWR[stage][style].Frames = frames;
                this.ReplayManager.AllStageWR[stage][style].RecordRunTime = this.StageWR[stage][style].RunTime;
                this.ReplayManager.AllStageWR[stage][style].RecordPlayerName = this.StageWR[stage][style].Name!;
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
                    this.ReplayManager.StageWR.RecordRunTime = this.StageWR[stage][style].RunTime;
                    this.ReplayManager.StageWR.RecordPlayerName = this.StageWR[stage][style].Name!;
                    this.ReplayManager.StageWR.MapTimeID = this.StageWR[stage][style].ID;
                    this.ReplayManager.StageWR.Stage = stage;
                    this.ReplayManager.StageWR.Type = 2;
                    this.ReplayManager.StageWR.RecordRank = 1;
                }
                break;
        }

        // Start the new map replay if none existed until now
        Server.NextFrame(() =>
        {
            if (type == 0 && this.ReplayManager.MapWR != null && !this.ReplayManager.MapWR.IsPlaying)
            {
                this.ReplayManager.MapWR.ResetReplay();
                this.ReplayManager.MapWR.Start();
            }
            else if (type == 1 && this.ReplayManager.BonusWR != null && !this.ReplayManager.BonusWR.IsPlaying)
            {
                this.ReplayManager.BonusWR.ResetReplay();
                this.ReplayManager.BonusWR.Start();
            }
            else if (type == 2 && this.ReplayManager.StageWR != null && !this.ReplayManager.StageWR.IsPlaying)
            {
                this.ReplayManager.StageWR.ResetReplay();
                this.ReplayManager.StageWR.Start();
            }
        }
        );
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
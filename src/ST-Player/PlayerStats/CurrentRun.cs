using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Data;

namespace SurfTimer;

/// <summary>
/// This class stores data for the current run.
/// </summary>
internal class CurrentRun : RunStats
{
    private readonly ILogger<CurrentRun> _logger;
    private readonly IDataAccessService _dataService;


    public CurrentRun() : base()
    {
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<CurrentRun>>();
        _dataService = SurfTimer.ServiceProvider.GetRequiredService<IDataAccessService>();
    }

    public override void Reset()
    {
        base.Reset();
        // Reset other properties as needed (specific for this class)
    }

    /// <summary>
    /// Saves the player's run to the database. 
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="bonus">Bonus number</param>
    /// <param name="stage">Stage number</param>
    /// <param name="run_ticks">Ticks for the run - used for Stage and Bonus entries</param>
    public async Task SaveMapTime(Player player, int bonus = 0, int stage = 0, int run_ticks = -1, [CallerMemberName] string methodName = "")
    {
        string replay_frames = "";

        /* Test Time Saving */
        if (methodName != "TestSetPb")
            replay_frames = player.ReplayRecorder.TrimReplay(player, stage != 0 ? 2 : bonus != 0 ? 1 : 0, stage == SurfTimer.CurrentMap.Stages);


        _logger.LogTrace("[{ClassName}] {MethodName} -> Sending total of {Frames} replay frames.",
            nameof(CurrentRun), methodName, replay_frames.Length);

        var stopwatch = Stopwatch.StartNew();
        int recType = stage != 0 ? 2 : bonus != 0 ? 1 : 0;

        var mapTime = new MapTimeDataModel
        {
            PlayerId = player.Profile.ID,
            MapId = player.CurrMap.ID,
            Style = player.Timer.Style,
            Type = recType,
            Stage = stage != 0 ? stage : bonus,
            Ticks = run_ticks == -1 ? this.Ticks : run_ticks,
            StartVelX = this.StartVelX,
            StartVelY = this.StartVelY,
            StartVelZ = this.StartVelZ,
            EndVelX = this.EndVelX,
            EndVelY = this.EndVelY,
            EndVelZ = this.EndVelZ,
            ReplayFramesBase64 = replay_frames,
            Checkpoints = this.Checkpoints // Test out 
        };

        /*
        _logger.LogDebug(
            "[{ClassName}] {MethodName} -> Sending data:\n" +
            " PlayerId: {PlayerId}\n" +
            " MapId: {MapId}\n" +
            " Style: {Style}\n" +
            " Type: {Type}\n" +
            " Stage: {Stage}\n" +
            " Ticks: {Ticks}\n" +
            " StartVel: ({StartVelX}, {StartVelY}, {StartVelZ})\n" +
            " EndVel: ({EndVelX}, {EndVelY}, {EndVelZ})\n" +
            " ReplayFramesBase64: {ReplayFrames}\n" +
            " Checkpoints: {CheckpointsCount}",
            nameof(CurrentRun), methodName,
            mapTime.PlayerId,
            mapTime.MapId,
            mapTime.Style,
            mapTime.Type,
            mapTime.Stage,
            mapTime.Ticks,
            mapTime.StartVelX, mapTime.StartVelY, mapTime.StartVelZ,
            mapTime.EndVelX, mapTime.EndVelY, mapTime.EndVelZ,
            mapTime.ReplayFramesBase64?.Length ?? 0, // log length to avoid dumping huge string
            mapTime.Checkpoints?.Count ?? 0
        );
        */

        await _dataService.InsertMapTimeAsync(mapTime);

        if (recType == 0 && !Config.API.GetApiOnly())
            await SaveCurrentRunCheckpoints(player, true);

        await player.CurrMap.LoadMapRecordRuns();
        await player.Stats.LoadPlayerMapTimesData(player);

        stopwatch.Stop();
        _logger.LogInformation("[{Class}] {Method} -> Finished SaveMapTime for '{Name}' in {Elapsed}ms | API = {API}",
            nameof(CurrentRun), methodName, player.Profile.Name, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly()
        );
    }

    /*
        public async Task SaveMapTime(Player player, int bonus = 0, int stage = 0, int run_ticks = -1, [CallerMemberName] string methodName = "")
        {
            string replay_frames = player.ReplayRecorder.TrimReplay(player, stage != 0 ? 2 : bonus != 0 ? 1 : 0, stage == SurfTimer.CurrentMap.Stages);

            _logger.LogTrace("[{ClassName}] {MethodName} -> SaveMapTime -> Sending total of {ReplayFramesTotal} replay frames.",
                nameof(CurrentRun), methodName, replay_frames.Length
            );

            var stopwatch = Stopwatch.StartNew();

            if (Config.API.GetApiOnly())
            {
                return;
            }
            else
            {
                await InsertMapTime(player, bonus, stage, run_ticks, replay_frames, true);

                if (stage != 0 || bonus != 0)
                {
                    _logger.LogTrace("[{ClassName}] {MethodName} -> Inserted an entry for {Type} {Number} - {Ticks}",
                        nameof(CurrentRun), methodName, (stage != 0 ? "Stage" : "Bonus"), (stage != 0 ? stage : bonus), run_ticks
                    );
                }
                else
                {
                    await SaveCurrentRunCheckpoints(player, true); // Save this run's checkpoints
                }

                await player.CurrMap.LoadMapRecordRuns(); // Reload the times for the Map
            }

            stopwatch.Stop();
            _logger.LogInformation("[{ClassName}] {MethodName} -> Finished SaveMapTime for player '{Name}' in {ElapsedMilliseconds}ms | API = {API}",
                nameof(CurrentRun), methodName, player.Profile.Name, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly()
            );
        }
    */

    /// <summary>
    /// Saves the CurrentRun of the player to the database. Does NOT support Bonus entries yet.
    /// </summary>
    public async Task InsertMapTime(Player player, int bonus = 0, int stage = 0, int run_ticks = -1, string replay_frames = "", bool reloadData = false, [CallerMemberName] string methodName = "")
    {
        int playerId = player.Profile.ID;
        int mapId = player.CurrMap.ID;
        int style = player.Timer.Style;
        int ticks = run_ticks == -1 ? this.Ticks : run_ticks;
        int type = stage != 0 ? 2 : bonus != 0 ? 1 : 0;
        float startVelX = this.StartVelX;
        float startVelY = this.StartVelY;
        float startVelZ = this.StartVelZ;
        float endVelX = this.EndVelX;
        float endVelY = this.EndVelY;
        float endVelZ = this.EndVelZ;

        var stopwatch = Stopwatch.StartNew();

        if (Config.API.GetApiOnly())
        {
            // API Insert map goes here
        }
        else
        {
            // int updatePlayerRunTask = await SurfTimer.DB.WriteAsync(
            var (updatePlayerRunTask, lastId) = await SurfTimer.DB.WriteAsync(
                string.Format(Config.MySQL.Queries.DB_QUERY_CR_INSERT_TIME, playerId, mapId, style, type, type == 2 ? stage : type == 1 ? bonus : 0, ticks, startVelX, startVelY, startVelZ, endVelX, endVelY, endVelZ, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), replay_frames));
            if (updatePlayerRunTask <= 0)
            {
                _logger.LogError("[{ClassName}] {MethodName} -> InsertMapTime -> Failed to insert/update player run in database. Player: {Name} ({SteamID})",
                    nameof(CurrentRun), methodName, player.Profile.Name, player.Profile.SteamID
                );
                Exception ex = new($"CS2 Surf ERROR >> internal class CurrentRun -> public async Task InsertMapTime -> Failed to insert/update player run in database. Player: {player.Profile.Name} ({player.Profile.SteamID})");
                throw ex;
            }

            if (reloadData && type == 0)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> InsertMapTime -> Will reload MapTime (Type {type}) data for '{Name}' (ID {MapTimeID}))",
                    nameof(CurrentRun), methodName, type, player.Profile.Name, player.Stats.PB[player.Timer.Style].ID
                );
                await player.Stats.PB[style].LoadPlayerSpecificMapTimeData(player); // Load the Map MapTime PB data again (will refresh the MapTime ID for the Checkpoints query)
            }
            else if (reloadData && type == 1)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> InsertMapTime -> Will reload Bonus MapTime (Type {type}) data for '{Name}' (ID {MapTimeID}))",
                    nameof(CurrentRun), methodName, type, player.Profile.Name, player.Stats.BonusPB[bonus][style].ID
                );
                await player.Stats.BonusPB[bonus][style].LoadPlayerSpecificMapTimeData(player); // Load the Bonus MapTime PB data again (will refresh the MapTime ID)
            }
            else if (reloadData && type == 2)
            {
                _logger.LogInformation("[{ClassName}] {MethodName} -> InsertMapTime -> Will reload Stage MapTime (Type {type}) data for '{Name}' (ID {MapTimeID}))",
                    nameof(CurrentRun), methodName, type, player.Profile.Name, player.Stats.StagePB[stage][style].ID
                );
                await player.Stats.StagePB[stage][style].LoadPlayerSpecificMapTimeData(player); // Load the Stage MapTime PB data again (will refresh the MapTime ID)
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("[{ClassName}] {MethodName} -> Finished InsertMapTime for player '{Name}' in {ElapsedMilliseconds}ms | API = {API}",
            nameof(CurrentRun), methodName, player.Profile.Name, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly()
        );
    }

    /// <summary>
    /// Saves the `CurrentRunCheckpoints` dictionary to the database
    /// API deals with this when sending a SaveMapTime of type 0, so we do not have an endpoint for it
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="reloadData">Whether to reload the PersonalBest Checkpoints data for the Player.</param>
    public async Task SaveCurrentRunCheckpoints(Player player, bool reloadData = false, [CallerMemberName] string methodName = "")
    {
        _logger.LogInformation("[{ClassName}] {MethodName} -> Saving {Count} checkpoints...",
            nameof(CurrentRun), methodName, this.Checkpoints.Count);

        var stopwatch = Stopwatch.StartNew();

        var checkpoints = this.Checkpoints.Select(cp => new Checkpoint
        {
            CP = cp.Key,
            Ticks = cp.Value.Ticks,
            EndTouch = cp.Value.EndTouch,
            StartVelX = cp.Value.StartVelX,
            StartVelY = cp.Value.StartVelY,
            StartVelZ = cp.Value.StartVelZ,
            EndVelX = cp.Value.EndVelX,
            EndVelY = cp.Value.EndVelY,
            EndVelZ = cp.Value.EndVelZ,
            Attempts = cp.Value.Attempts
        });

        int mapTimeId = player.Stats.PB[player.Timer.Style].ID;

        // await _dataService.SaveRunCheckpointsAsync(mapTimeId, checkpoints);

        this.Checkpoints.Clear();

        if (reloadData)
        {
            _logger.LogInformation("[{ClassName}] {MethodName} -> Reloading Checkpoints data for '{Name}' (ID {MapTimeID})",
                nameof(CurrentRun), methodName, player.Profile.Name, mapTimeId);
            await player.Stats.PB[player.Timer.Style].LoadCheckpoints();
        }

        stopwatch.Stop();
        _logger.LogInformation("[{ClassName}] {MethodName} -> Finished saving checkpoints for '{Name}' in {Elapsed}ms | API = {API}",
            nameof(CurrentRun), methodName, player.Profile.Name, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly());
    }

    /* Delete after testing it using the ApiDataAccessService
    public async Task SaveCurrentRunCheckpoints(Player player, bool reloadData = false, [CallerMemberName] string methodName = "")
    {
        _logger.LogInformation("[{ClassName}] {MethodName} -> SaveCurrentRunCheckpoints -> Will send {ThisRunCheckpoints} ({CheckpointsCount}) checkpoints to DB....",
            nameof(CurrentRun), methodName, this.Checkpoints.Count, this.Checkpoints.Count
        );
        var stopwatch = Stopwatch.StartNew();

        int style = player.Timer.Style;
        int mapTimeId = player.Stats.PB[style].ID;
        List<string> commands = new List<string>();
        // Loop through the checkpoints and insert/update them in the database for the run
        foreach (var item in this.Checkpoints)
        {
            int cp = item.Key;
            int ticks = item.Value!.Ticks;
            int endTouch = item.Value!.EndTouch;
            double startVelX = item.Value!.StartVelX;
            double startVelY = item.Value!.StartVelY;
            double startVelZ = item.Value!.StartVelZ;
            double endVelX = item.Value!.EndVelX;
            double endVelY = item.Value!.EndVelY;
            double endVelZ = item.Value!.EndVelZ;
            int attempts = item.Value!.Attempts;

#if DEBUG
            _logger.LogDebug("[{ClassName}] {MethodName} -> SaveCurrentRunCheckpoints -> CP: {Checkpoint} | MapTime ID: {MapTimeID} | Time: {Time} | Ticks: {Ticks} | SVX {StartVelX} | SVY {StartVelY} | SVZ {StartVelZ} | EVX {EndVelX} | EVY {EndVelY} | EVZ {EndVelZ}",
                nameof(CurrentRun), methodName, cp, mapTimeId, endTouch, ticks, startVelX, startVelY, startVelZ, endVelX, endVelY, endVelZ
            );
            _logger.LogDebug("Query to send:\n{Query}",
                string.Format(Config.MySQL.Queries.DB_QUERY_CR_INSERT_CP,
                    mapTimeId, cp, ticks, startVelX, startVelY, startVelZ, endVelX, endVelY, endVelZ, attempts, endTouch)
            );
#endif

            if (item.Value != null && player.Controller.PlayerPawn.Value != null)
            {
                string command = string.Format(
                    Config.MySQL.Queries.DB_QUERY_CR_INSERT_CP,
                    mapTimeId, cp, ticks, startVelX, startVelY, startVelZ, endVelX, endVelY, endVelZ, attempts, endTouch
                );
                commands.Add(command);
            }
        }
        await SurfTimer.DB.TransactionAsync(commands);
        this.Checkpoints.Clear();

        if (reloadData)
        {
            _logger.LogInformation("[{ClassName}] {MethodName} -> SaveCurrentRunCheckpoints -> Will reload Checkpoints data for '{Name}' (ID {MapTimeID})",
                nameof(CurrentRun), methodName, player.Profile.Name, player.Stats.PB[player.Timer.Style].ID
            );
            await player.Stats.PB[player.Timer.Style].LoadCheckpoints(); // Load the Checkpoints data again
        }

        stopwatch.Stop();
        _logger.LogInformation("[{ClassName}] {MethodName} -> Finished SaveCurrentRunCheckpoints(reloadData = {reloadData}) for player '{Name}' in {ElapsedMilliseconds}ms | API = {API}",
            nameof(CurrentRun), methodName, reloadData, player.Profile.Name, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly()
        );
    }

*/
    public void PrintSituations(Player player)
    {
        Console.WriteLine($"========================== FOUND SITUATIONS ==========================");
        for (int i = 0; i < player.ReplayRecorder.Frames.Count; i++)
        {
            ReplayFrame x = player.ReplayRecorder.Frames[i];
            switch (x.Situation)
            {
                case ReplayFrameSituation.START_ZONE_ENTER:
                    Console.WriteLine($"START_ZONE_ENTER: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.START_ZONE_EXIT:
                    Console.WriteLine($"START_ZONE_EXIT: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.STAGE_ZONE_ENTER:
                    Console.WriteLine($"STAGE_ZONE_ENTER: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.STAGE_ZONE_EXIT:
                    Console.WriteLine($"STAGE_ZONE_EXIT: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.CHECKPOINT_ZONE_ENTER:
                    Console.WriteLine($"CHECKPOINT_ZONE_ENTER: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.CHECKPOINT_ZONE_EXIT:
                    Console.WriteLine($"CHECKPOINT_ZONE_EXIT: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.END_ZONE_ENTER:
                    Console.WriteLine($"END_ZONE_ENTER: {i} | Situation {x.Situation}");
                    break;
                case ReplayFrameSituation.END_ZONE_EXIT:
                    Console.WriteLine($"END_ZONE_EXIT: {i} | Situation {x.Situation}");
                    break;
            }
        }
        Console.WriteLine("========================== MapSituations: {0} | START_ZONE_ENTER = {1} | START_ZONE_EXIT = {2} | END_ZONE_ENTER = {3} ==========================",
            player.ReplayRecorder.MapSituations.Count,
            player.ReplayRecorder.MapSituations[0], player.ReplayRecorder.MapSituations[1], player.ReplayRecorder.MapSituations[2]);
        Console.WriteLine("========================== Total Frames: {0} ==========================", player.ReplayRecorder.Frames.Count);
    }

}


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
    /// Supports all types of runs Map/Bonus/Stage. 
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="bonus">Bonus number</param>
    /// <param name="stage">Stage number</param>
    /// <param name="run_ticks">Ticks for the run - used for Stage and Bonus entries</param>
    public async Task SaveMapTime(Player player, int bonus = 0, int stage = 0, int run_ticks = -1, [CallerMemberName] string methodName = "")
    {
        string replay_frames = "";

        // /* Test Time Saving */
        // if (methodName != "TestSetPb")
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

        int mapTimeId = await _dataService.InsertMapTimeAsync(mapTime);

        // Reload the times for the map
        await player.CurrMap.LoadMapRecordRuns();

        _logger.LogTrace("[{ClassName}] {MethodName} -> Loading data for run {ID} with type {Type}.",
            nameof(CurrentRun), methodName, mapTimeId, recType
        );

        // Reload the player PB time (could possibly be skipped as we have mapTimeId after inserting)
        switch (recType)
        {
            case 0:
                player.Stats.PB[player.Timer.Style].ID = mapTimeId;
                await player.Stats.PB[player.Timer.Style].LoadPlayerSpecificMapTimeData(player);
                break;
            case 1:
                player.Stats.BonusPB[bonus][player.Timer.Style].ID = mapTimeId;
                await player.Stats.BonusPB[bonus][player.Timer.Style].LoadPlayerSpecificMapTimeData(player);
                break;
            case 2:
                player.Stats.StagePB[stage][player.Timer.Style].ID = mapTimeId;
                await player.Stats.StagePB[stage][player.Timer.Style].LoadPlayerSpecificMapTimeData(player);
                break;
        }

        stopwatch.Stop();
        _logger.LogInformation("[{Class}] {Method} -> Finished SaveMapTime for '{Name}' (ID {ID}) in {Elapsed}ms | API = {API}",
            nameof(CurrentRun), methodName, player.Profile.Name, mapTimeId, stopwatch.ElapsedMilliseconds, Config.API.GetApiOnly()
        );
    }

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


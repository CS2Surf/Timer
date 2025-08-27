using CounterStrikeSharp.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Data;
using SurfTimer.Shared.DTO;
using SurfTimer.Shared.Entities;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SurfTimer;

/// <summary>
/// This class stores data for the current run.
/// </summary>
public class CurrentRun : RunStatsEntity
{
    private readonly ILogger<CurrentRun> _logger;
    private readonly IDataAccessService _dataService;

    public Dictionary<int, CheckpointEntity> Checkpoints { get; set; }

    internal CurrentRun()
    {
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<CurrentRun>>();
        _dataService = SurfTimer.ServiceProvider.GetRequiredService<IDataAccessService>();

        Checkpoints = new Dictionary<int, CheckpointEntity>();
    }

    /// <summary>
    /// Saves the player's run to the database.
    /// Supports all types of runs Map/Bonus/Stage. 
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="bonus">Bonus number</param>
    /// <param name="stage">Stage number</param>
    /// <param name="run_ticks">Ticks for the run - used for Stage and Bonus entries</param>
    internal async Task SaveMapTime(Player player, short bonus = 0, short stage = 0, int run_ticks = -1, [CallerMemberName] string methodName = "")
    {
        string replay_frames = "";
        int style = player.Timer.Style;
        int mapTimeId = 0;
        short recType;

        if (stage != 0)
        {
            recType = 2; // Stage run
        }
        else if (bonus != 0)
        {
            recType = 1; // Bonus run
        }
        else
        {
            recType = 0; // Map run
        }

        /// Test Time Saving: if (methodName != "TestSetPb")
        replay_frames = player.ReplayRecorder.TrimReplay(player, recType, stage == SurfTimer.CurrentMap.Stages);

        _logger.LogTrace("[{ClassName}] {MethodName} -> Sending total of {Frames} serialized and compressed replay frames.",
            nameof(CurrentRun), methodName, replay_frames.Length
        );

        var stopwatch = Stopwatch.StartNew();
        var mapTime = new MapTimeRunDataDto
        {
            PlayerID = player.Profile.ID,
            MapID = SurfTimer.CurrentMap.ID,
            Style = player.Timer.Style,
            Type = recType,
            Stage = stage != 0 ? stage : bonus,
            RunTime = run_ticks == -1 ? this.RunTime : run_ticks,
            StartVelX = this.StartVelX,
            StartVelY = this.StartVelY,
            StartVelZ = this.StartVelZ,
            EndVelX = this.EndVelX,
            EndVelY = this.EndVelY,
            EndVelZ = this.EndVelZ,
            ReplayFrames = replay_frames,
            Checkpoints = this.Checkpoints
        };

        switch (recType)
        {
            case 0:
                mapTimeId = player.Stats.PB[style].ID;
                break;
            case 1:
                mapTimeId = player.Stats.BonusPB[bonus][style].ID;
                break;
            case 2:
                mapTimeId = player.Stats.StagePB[stage][style].ID;
                break;
        }

        if (mapTimeId <= 0)
            mapTimeId = await _dataService.InsertMapTimeAsync(mapTime);
        else
            mapTimeId = await _dataService.UpdateMapTimeAsync(mapTime, mapTimeId);


        // Reload the times for the map
        await SurfTimer.CurrentMap.LoadMapRecordRuns();

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
            nameof(CurrentRun), methodName, player.Profile.Name, mapTimeId, stopwatch.ElapsedMilliseconds, Config.Api.GetApiOnly()
        );
    }

    /// <summary>
    /// Deals with saving a Stage MapTime (Type 2) in the Database.
    /// Should deal with `IsStageMode` runs, Stages during Map Runs and also Last Stage.
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="stage">Stage to save</param>
    /// <param name="saveLastStage">Is it the last stage?</param>
    /// <param name="stage_run_time">Run Time (Ticks) for the stage run</param>
    internal static async Task SaveStageTime(Player player, short stage = -1, int stage_run_time = -1, bool saveLastStage = false)
    {
#if DEBUG
        _logger.LogTrace("[{Class}] -> SaveStageTime received: Name = {Name} | Stage = {Stage} | RunTime = {RunTime} | IsLastStage = {IsLastStage}",
            nameof(CurrentRun), player.Profile.Name, stage, stage_run_time, saveLastStage
        );
#endif
        int pStyle = player.Timer.Style;
        if (
            stage_run_time < SurfTimer.CurrentMap.StageWR[stage][pStyle].RunTime ||
            SurfTimer.CurrentMap.StageWR[stage][pStyle].ID == -1 ||
            player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].RunTime > stage_run_time ||
            player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].ID == -1
        )
        {
            if (stage_run_time < SurfTimer.CurrentMap.StageWR[stage][pStyle].RunTime) // Player beat the Stage WR
            {
                int timeImprove = SurfTimer.CurrentMap.StageWR[stage][pStyle].RunTime - stage_run_time;
                Server.PrintToChatAll($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagewr_improved",
                    player.Controller.PlayerName, stage, PlayerHud.FormatTime(stage_run_time), PlayerHud.FormatTime(timeImprove), PlayerHud.FormatTime(SurfTimer.CurrentMap.StageWR[stage][pStyle].RunTime)]}"
                );
            }
            else if (SurfTimer.CurrentMap.StageWR[stage][pStyle].ID == -1) // No Stage record was set on the map
            {
                Server.PrintToChatAll($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagewr_set",
                    player.Controller.PlayerName, stage, PlayerHud.FormatTime(stage_run_time)]}"
                );
            }
            else if (player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].ID == -1) // Player first Stage personal best
            {
                player.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagepb_set",
                    stage, PlayerHud.FormatTime(stage_run_time)]}"
                );
            }
            else if (player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].RunTime > stage_run_time) // Player beating their existing Stage personal best
            {
                int timeImprove = player.Stats.StagePB[stage][pStyle].RunTime - stage_run_time;
                Server.PrintToChatAll($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagepb_improved",
                    player.Controller.PlayerName, stage, PlayerHud.FormatTime(stage_run_time), PlayerHud.FormatTime(timeImprove), PlayerHud.FormatTime(player.Stats.StagePB[stage][pStyle].RunTime)]}"
                );
            }

            player.ReplayRecorder.IsSaving = true;

            // Save stage run
            await player.Stats.ThisRun.SaveMapTime(player, stage: stage, run_ticks: stage_run_time); // Save the Stage MapTime PB data
        }
        else if (stage_run_time > SurfTimer.CurrentMap.StageWR[stage][pStyle].RunTime && player.Timer.IsStageMode) // Player is behind the Stage WR for the map
        {
            int timeImprove = stage_run_time - SurfTimer.CurrentMap.StageWR[stage][pStyle].RunTime;
            player.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagewr_missed",
                stage, PlayerHud.FormatTime(stage_run_time), PlayerHud.FormatTime(timeImprove), PlayerHud.FormatTime(SurfTimer.CurrentMap.StageWR[stage][pStyle].RunTime)]}"
            );
        }
    }


    public static void PrintSituations(Player player)
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


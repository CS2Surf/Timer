using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;

namespace SurfTimer;

public partial class SurfTimer
{
    /// <summary>
    /// Handler for trigger start touch hook - CBaseTrigger_StartTouchFunc
    /// </summary>
    /// <returns>CounterStrikeSharp.API.Core.HookResult</returns>
    /// <exception cref="Exception"></exception>
    internal HookResult OnTriggerStartTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        CBaseTrigger trigger = new CBaseTrigger(caller.Handle);
        CBaseEntity entity = new CBaseEntity(activator.Handle);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        if (!client.IsValid || !client.PawnIsAlive || !playerList.ContainsKey((int)client.UserId!)) // !playerList.ContainsKey((int)client.UserId!) make sure to not check for user_id that doesnt exists
        {
            return HookResult.Continue;
        }
        // To-do: Sometimes this triggers before `OnPlayerConnect` and `playerList` does not contain the player how is this possible :thonk:
        if (!playerList.ContainsKey(client.UserId ?? 0))
        {
            _logger.LogCritical("[{ClassName}] OnTriggerStartTouch -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {PlayerName} ({UserId})",
                nameof(SurfTimer), client.PlayerName, client.UserId
            );

            Exception exception = new($"[{nameof(SurfTimer)}] OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
            throw exception;
        }
        // Implement Trigger Start Touch Here
        Player player = playerList[client.UserId ?? 0];

#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
#endif

        if (DB == null)
        {
            _logger.LogCritical("[{ClassName}] OnTriggerStartTouch -> DB object is null, this shouldn't happen.",
                nameof(SurfTimer)
            );

            Exception exception = new Exception($"[{nameof(SurfTimer)}] OnTriggerStartTouch -> DB object is null, this shouldn't happen.");
            throw exception;
        }

        if (trigger.Entity!.Name != null)
        {
            ZoneType currentZone = GetZoneType(trigger.Entity.Name);

            switch (currentZone)
            {
                // Map end zones -- hook into map_end
                case ZoneType.MapEnd:
                    StartTouchHandleMapEndZone(player);
                    break;
                // Map start zones -- hook into map_start, (s)tage1_start
                case ZoneType.MapStart:
                    StartTouchHandleMapStartZone(player, trigger);
                    break;
                // Stage start zones -- hook into (s)tage#_start
                case ZoneType.StageStart:
                    StartTouchHandleStageStartZone(player, trigger);
                    break;
                // Map checkpoint zones -- hook into map_(c)heck(p)oint#
                case ZoneType.Checkpoint:
                    StartTouchHandleCheckpointZone(player, trigger);
                    break;
                // Bonus start zones -- hook into (b)onus#_start
                case ZoneType.BonusStart:
                    StartTouchHandleBonusStartZone(player, trigger);
                    break;
                // Bonus end zones -- hook into (b)onus#_end
                case ZoneType.BonusEnd:
                    StartTouchHandleBonusEndZone(player, trigger);
                    break;

                default:
                    _logger.LogError("[{ClassName}] OnTriggerStartTouch -> Unknown MapZone detected in OnTriggerStartTouch. Name: {ZoneName}",
                        nameof(SurfTimer), trigger.Entity.Name
                    );
                    break;
            }
        }
        return HookResult.Continue;
    }

    /// <summary>
    /// Deals with saving a Stage MapTime (Type 2) in the Database.
    /// Should deal with `IsStageMode` runs, Stages during Map Runs and also Last Stage.
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="stage">Stage to save</param>
    /// <param name="saveLastStage">Is it the last stage?</param>
    /// <param name="stage_run_time">Run Time (Ticks) for the stage run</param>
    void SaveStageTime(Player player, int stage = -1, int stage_run_time = -1, bool saveLastStage = false)
    {
        // player.Controller.PrintToChat($"{Config.PluginPrefix} SaveStageTime received: {player.Profile.Name}, {stage}, {stage_run_time}, {saveLastStage}");
        int pStyle = player.Timer.Style;
        if (
            stage_run_time < CurrentMap.StageWR[stage][pStyle].Ticks ||
            CurrentMap.StageWR[stage][pStyle].ID == -1 ||
            player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].Ticks > stage_run_time ||
            player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].ID == -1
        )
        {
            if (stage_run_time < CurrentMap.StageWR[stage][pStyle].Ticks) // Player beat the Stage WR
            {
                int timeImprove = CurrentMap.StageWR[stage][pStyle].Ticks - stage_run_time;
                Server.PrintToChatAll($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagewr_improved",
                    player.Controller.PlayerName, stage, PlayerHUD.FormatTime(stage_run_time), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(CurrentMap.StageWR[stage][pStyle].Ticks)]}"
                );
            }
            else if (CurrentMap.StageWR[stage][pStyle].ID == -1) // No Stage record was set on the map
            {
                Server.PrintToChatAll($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagewr_set",
                    player.Controller.PlayerName, stage, PlayerHUD.FormatTime(stage_run_time)]}"
                );
            }
            else if (player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].ID == -1) // Player first Stage personal best
            {
                player.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagepb_set",
                    stage, PlayerHUD.FormatTime(stage_run_time)]}"
                );
            }
            else if (player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].Ticks > stage_run_time) // Player beating their existing Stage personal best
            {
                int timeImprove = player.Stats.StagePB[stage][pStyle].Ticks - stage_run_time;
                Server.PrintToChatAll($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagepb_improved",
                    player.Controller.PlayerName, stage, PlayerHUD.FormatTime(stage_run_time), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(player.Stats.StagePB[stage][pStyle].Ticks)]}"
                );
            }

            player.ReplayRecorder.IsSaving = true;
            AddTimer(1.0f, async () =>
            {
                // Save stage run
                Console.WriteLine($"==== OnTriggerStartTouch -> SaveStageTime -> [StageWR (IsStageMode? {player.Timer.IsStageMode} | Last? {saveLastStage})] Saving Stage {stage} ({stage}) time of {PlayerHUD.FormatTime(stage_run_time)} ({stage_run_time})");
                await player.Stats.ThisRun.SaveMapTime(player, stage: stage, run_ticks: stage_run_time); // Save the Stage MapTime PB data
            });
        }
        else if (stage_run_time > CurrentMap.StageWR[stage][pStyle].Ticks && player.Timer.IsStageMode) // Player is behind the Stage WR for the map
        {
            int timeImprove = stage_run_time - CurrentMap.StageWR[stage][pStyle].Ticks;
            player.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["stagewr_missed",
                stage, PlayerHUD.FormatTime(stage_run_time), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(CurrentMap.StageWR[stage][pStyle].Ticks)]}"
            );
        }
    }
}


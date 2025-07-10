using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace SurfTimer;

public partial class SurfTimer
{
    internal enum ZoneType
    {
        MapEnd,
        MapStart,
        StageStart,
        Checkpoint,
        BonusStart,
        BonusEnd,
        Unknown
    }

    /// <summary>
    /// Determines the zone type based on the entity name.
    /// </summary>
    /// <param name="entityName">Name of the entity.</param>
    /// <returns>ZoneType data</returns>
    private ZoneType GetZoneType(string entityName)
    {
        if (entityName == "map_end")
            return ZoneType.MapEnd;
        else if (entityName.Contains("map_start") || entityName.Contains("s1_start") || entityName.Contains("stage1_start"))
            return ZoneType.MapStart;
        else if (Regex.IsMatch(entityName, @"^s([1-9][0-9]?|tage[1-9][0-9]?)_start$"))
            return ZoneType.StageStart;
        else if (Regex.IsMatch(entityName, @"^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$"))
            return ZoneType.Checkpoint;
        else if (Regex.IsMatch(entityName, @"^b([1-9][0-9]?|onus[1-9][0-9]?)_start$"))
            return ZoneType.BonusStart;
        else if (Regex.IsMatch(entityName, @"^b([1-9][0-9]?|onus[1-9][0-9]?)_end$"))
            return ZoneType.BonusEnd;

        return ZoneType.Unknown;
    }

    /// <summary>
    /// Handler for trigger start touch hook - CBaseTrigger_StartTouchFunc
    /// </summary>
    /// <returns>CounterStrikeSharp.API.Core.HookResult</returns>
    /// <exception cref="Exception"></exception>
    internal HookResult OnTriggerStartTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay, [CallerMemberName] string methodName = "")
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
            _logger.LogCritical("[{ClassName}] {MethodName} -> OnTriggerStartTouch -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {PlayerName} ({UserId})",
                nameof(SurfTimer), methodName, client.PlayerName, client.UserId
            );

            Exception exception = new($"[{nameof(SurfTimer)}] {methodName} -> OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
            throw exception;
        }
        // Implement Trigger Start Touch Here
        Player player = playerList[client.UserId ?? 0];

#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
#endif

        if (DB == null)
        {
            _logger.LogCritical("[{ClassName}] {MethodName} -> OnTriggerStartTouch -> DB object is null, this shouldn't happen.",
                nameof(SurfTimer), methodName
            );

            Exception exception = new Exception($"[{nameof(SurfTimer)}] {methodName} -> OnTriggerStartTouch -> DB object is null, this shouldn't happen.");
            throw exception;
        }

        if (trigger.Entity!.Name != null)
        {
            ZoneType currentZone = GetZoneType(trigger.Entity.Name);

            switch (currentZone)
            {
                // Map end zones -- hook into map_end
                case ZoneType.MapEnd:
                    HandleMapEndZone(player);
                    break;
                // Map start zones -- hook into map_start, (s)tage1_start
                case ZoneType.MapStart:
                    HandleMapStartZone(player, trigger);
                    break;
                // Stage start zones -- hook into (s)tage#_start
                case ZoneType.StageStart:
                    HandleStageStartZone(player, trigger);
                    break;
                // Map checkpoint zones -- hook into map_(c)heck(p)oint#
                case ZoneType.Checkpoint:
                    HandleCheckpointZone(player, trigger);
                    break;
                // Bonus start zones -- hook into (b)onus#_start
                case ZoneType.BonusStart:
                    HandleBonusStartZone(player, trigger);
                    break;
                // Bonus end zones -- hook into (b)onus#_end
                case ZoneType.BonusEnd:
                    HandleBonusEndZone(player, trigger);
                    break;

                default:
                    _logger.LogError("[{ClassName}] {MethodName} -> OnTriggerStartTouch -> Unknown MapZone detected in OnTriggerStartTouch. Name: {ZoneName}",
                        nameof(SurfTimer), methodName, trigger.Entity.Name
                    );
                    break;
            }
        }
        return HookResult.Continue;
    }

    private void HandleMapEndZone(Player player, [CallerMemberName] string methodName = "")
    {
        // Get velocities for DB queries
        // Get the velocity of the player - we will be using this values to compare and write to DB
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();
        int pStyle = player.Timer.Style;

        player.Controller.PrintToCenter($"Map End");

        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_ZONE_ENTER;
        player.ReplayRecorder.MapSituations.Add(player.Timer.Ticks);

        player.Stats.ThisRun.Ticks = player.Timer.Ticks; // End time for the Map run
        player.Stats.ThisRun.EndVelX = velocity.X; // End speed for the Map run
        player.Stats.ThisRun.EndVelY = velocity.Y; // End speed for the Map run
        player.Stats.ThisRun.EndVelZ = velocity.Z; // End speed for the Map run


        // MAP END ZONE - Map RUN
        if (player.Timer.IsRunning && !player.Timer.IsStageMode)
        {
            player.Timer.Stop();
            bool saveMapTime = false;
            string PracticeString = "";
            if (player.Timer.IsPracticeMode)
                PracticeString = $"({ChatColors.Grey}Practice{ChatColors.Default}) ";

            if (player.Timer.Ticks < CurrentMap.WR[pStyle].Ticks) // Player beat the Map WR
            {
                saveMapTime = true;
                int timeImprove = CurrentMap.WR[pStyle].Ticks - player.Timer.Ticks;
                Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mapwr_improved",
                    player.Controller.PlayerName, PlayerHUD.FormatTime(player.Timer.Ticks), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(CurrentMap.WR[pStyle].Ticks)]}"
                );
            }
            else if (CurrentMap.WR[pStyle].ID == -1) // No record was set on the map
            {
                saveMapTime = true;
                Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mapwr_set",
                    player.Controller.PlayerName, PlayerHUD.FormatTime(player.Timer.Ticks)]}"
                );
            }
            else if (player.Stats.PB[pStyle].Ticks <= 0) // Player first ever PersonalBest for the map
            {
                saveMapTime = true;
                player.Controller.PrintToChat($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mappb_set",
                    PlayerHUD.FormatTime(player.Timer.Ticks)]}"
                );
            }
            else if (player.Timer.Ticks < player.Stats.PB[pStyle].Ticks) // Player beating their existing PersonalBest for the map
            {
                saveMapTime = true;
                int timeImprove = player.Stats.PB[pStyle].Ticks - player.Timer.Ticks;
                Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mappb_improved",
                    player.Controller.PlayerName, PlayerHUD.FormatTime(player.Timer.Ticks), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(player.Stats.PB[pStyle].Ticks)]}"
                );
            }
            else // Player did not beat their existing PersonalBest for the map nor the map record
            {
                player.Controller.PrintToChat($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mappb_missed",
                    PlayerHUD.FormatTime(player.Timer.Ticks)]}"
                );
            }

            if (saveMapTime)
            {
                player.ReplayRecorder.IsSaving = true;
                AddTimer(1.0f, async () =>
                {
                    await player.Stats.ThisRun.SaveMapTime(player); // Save the MapTime PB data
                });
            }

            // Add entry in DB for the run
            if (!player.Timer.IsPracticeMode)
            {
                // Should we also save a last stage run?
                if (CurrentMap.Stages > 0)
                {
                    AddTimer(0.1f, () =>
                    {
                        // This calculation is wrong unless we wait for a bit in order for the `END_ZONE_ENTER` to be available in the `Frames` object
                        int stage_run_time = player.ReplayRecorder.Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.END_ZONE_ENTER) - player.ReplayRecorder.Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.STAGE_ZONE_EXIT);

                        // player.Controller.PrintToChat($"{Config.PluginPrefix} [LAST StageWR (Map RUN)] Sending to SaveStageTime: {player.Profile.Name}, {CurrentMap.Stages}, {stage_run_time}");
                        SaveStageTime(player, CurrentMap.Stages, stage_run_time, true);
                    });
                }

                // This section checks if the PB is better than WR
                if (player.Timer.Ticks < CurrentMap.WR[pStyle].Ticks || CurrentMap.WR[pStyle].ID == -1)
                {
                    AddTimer(2f, () =>
                    {
                        Console.WriteLine("CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> WR/PB");
                        CurrentMap.ReplayManager.MapWR.Start(); // Start the replay again
                        CurrentMap.ReplayManager.MapWR.FormatBotName();
                    });
                }

            }
        }
        // MAP END ZONE - Stage RUN
        else if (player.Timer.IsStageMode)
        {
            player.Timer.Stop();

            if (!player.Timer.IsPracticeMode)
            {
                AddTimer(0.1f, () =>
                {
                    // This calculation is wrong unless we wait for a bit in order for the `END_ZONE_ENTER` to be available in the `Frames` object
                    int stage_run_time = player.ReplayRecorder.Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.END_ZONE_ENTER) - player.ReplayRecorder.Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.STAGE_ZONE_EXIT);

                    // player.Controller.PrintToChat($"{Config.PluginPrefix} [LAST StageWR (IsStageMode)] Sending to SaveStageTime: {player.Profile.Name}, {CurrentMap.Stages}, {stage_run_time}");
                    SaveStageTime(player, CurrentMap.Stages, stage_run_time, true);
                });
            }
        }

#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Red}Map Stop Zone");
#endif
    }

    private static void HandleMapStartZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
    {
        // We shouldn't start timer and reset data until MapTime has been saved - mostly concerns the Replays and trimming the correct parts
        if (!player.ReplayRecorder.IsSaving)
        {
            player.ReplayRecorder.Reset(); // Start replay recording
            player.ReplayRecorder.Start(); // Start replay recording
            player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.START_ZONE_ENTER;

            player.ReplayRecorder.MapSituations.Add(player.ReplayRecorder.Frames.Count);
            // player.Controller.PrintToChat($"{ChatColors.Green}START_ZONE_ENTER: player.ReplayRecorder.MapSituations.Add({player.ReplayRecorder.Frames.Count})");
            // Console.WriteLine($"START_ZONE_ENTER: player.ReplayRecorder.MapSituations.Add({player.ReplayRecorder.Frames.Count})");
            player.Timer.Reset();
            player.Stats.ThisRun.Checkpoints.Clear();
            player.Controller.PrintToCenter($"Map Start ({trigger.Entity!.Name})");

#if DEBUG
            player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
#endif
        }
        else
        {
            player.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["reset_delay"]}");
        }
    }

    private void HandleStageStartZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
    {
        // Get velocities for DB queries
        // Get the velocity of the player - we will be using this values to compare and write to DB
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();
        int pStyle = player.Timer.Style;
        int stage = Int32.Parse(Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value);

        if (!player.ReplayRecorder.IsRecording)
            player.ReplayRecorder.Start();

        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.STAGE_ZONE_ENTER;
        player.ReplayRecorder.StageEnterSituations.Add(player.ReplayRecorder.Frames.Count);
        Console.WriteLine($"STAGE_ZONE_ENTER: player.ReplayRecorder.StageEnterSituations.Add({player.ReplayRecorder.Frames.Count})");

        bool failed_stage = false;
        if (player.Timer.Stage == stage)
            failed_stage = true;

        // Reset/Stop the Stage timer
        // Save a Stage run when `IsStageMode` is active - (`stage - 1` to get the previous stage data)
        if (player.Timer.IsStageMode)
        {
            // player.Controller.PrintToChat($"{Config.PluginPrefix} Player ticks higher than 0? {ChatColors.Yellow}{player.Timer.Ticks > 0}");
            // player.Controller.PrintToChat($"{Config.PluginPrefix} Player time is faster than StageWR time? {ChatColors.Yellow}{player.Timer.Ticks < CurrentMap.StageWR[stage - 1][style].Ticks}");
            // player.Controller.PrintToChat($"{Config.PluginPrefix} No StageWR Exists? {ChatColors.Yellow}{CurrentMap.StageWR[stage - 1][style].ID == -1}");
            // player.Controller.PrintToChat($"{Config.PluginPrefix} Not null? {ChatColors.Yellow}{player.Stats.StagePB[stage - 1][style] != null}");
            // player.Controller.PrintToChat($"{Config.PluginPrefix} Time faster than existing stage PB? {ChatColors.Yellow}{player.Stats.StagePB[stage - 1][style].Ticks > player.Timer.Ticks}");
            if (stage > 1 && !failed_stage && !player.Timer.IsPracticeMode)
            {
                int stage_run_time = player.Timer.Ticks;
                // player.Controller.PrintToChat($"{Config.PluginPrefix} [StageWR (IsStageMode)] Sending to SaveStageTime: {player.Profile.Name}, {stage - 1}, {stage_run_time}");
                SaveStageTime(player, stage - 1, stage_run_time);
            }
            player.Timer.Reset();
            player.Timer.IsStageMode = true;
            // player.Controller.PrintToChat($"{ChatColors.Red}Resetted{ChatColors.Default} Stage timer for stage {ChatColors.Green}{stage}");
        }

        player.Timer.Stage = stage;

#if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Timer.IsRunning: {player.Timer.IsRunning}");
        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> !player.Timer.IsStageMode: {!player.Timer.IsStageMode}");
        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.ThisRun.Checkpoint.Count <= stage: {player.Stats.ThisRun.Checkpoints.Count <= stage}");
#endif

        // This should patch up re-triggering *player.Stats.ThisRun.Checkpoint.Count < stage*
        if (player.Timer.IsRunning && !player.Timer.IsStageMode && player.Stats.ThisRun.Checkpoints.Count < stage)
        {
            // Save Stage MapTime during a Map run
            if (stage > 1 && !failed_stage && !player.Timer.IsPracticeMode)
            {
                int stage_run_time = player.Timer.Ticks - player.Stats.ThisRun.Ticks; // player.Stats.ThisRun.Ticks should be the Tick we left the previous Stage zone
                // player.Controller.PrintToChat($"{Config.PluginPrefix} [StageWR (Map RUN)] Sending to SaveStageTime: {player.Profile.Name}, {stage - 1}, {stage_run_time}");
                SaveStageTime(player, stage - 1, stage_run_time);
            }

            player.Timer.Checkpoint = stage - 1; // Stage = Checkpoint when in a run on a Staged map

#if DEBUG
            Console.WriteLine($"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `stage`: {stage} | player.Timer.Checkpoint: {stage - 1}");
            Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.PB[{pStyle}].Checkpoint.Count = {player.Stats.PB[pStyle].Checkpoints.Count}");
#endif

            // Print checkpoint message
            player.HUD.DisplayCheckpointMessages();

            // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
            if (!player.Stats.ThisRun.Checkpoints.ContainsKey(player.Timer.Checkpoint))
            {
                Checkpoint cp2 = new Checkpoint(player.Timer.Checkpoint,
                                                player.Timer.Ticks,
                                                velocity.X,
                                                velocity.Y,
                                                velocity.Z,
                                                -1.0f,
                                                -1.0f,
                                                -1.0f,
                                                0,
                                                1);
                player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint] = cp2;
            }
            else
            {
                player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].Attempts++;
            }
        }

#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
#endif
    }

    private static void HandleCheckpointZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
    {
        // Get velocities for DB queries
        // Get the velocity of the player - we will be using this values to compare and write to DB
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();
        int pStyle = player.Timer.Style;
        int checkpoint = Int32.Parse(Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value);
        player.Timer.Checkpoint = checkpoint;

        // This should patch up re-triggering *player.Stats.ThisRun.Checkpoint.Count < checkpoint*
        if (player.Timer.IsRunning && !player.Timer.IsStageMode && player.Stats.ThisRun.Checkpoints.Count < checkpoint)
        {
#if DEBUG
            Console.WriteLine($"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `checkpoint`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value)}");
            Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Checkpoint zones) -> player.Stats.PB[{pStyle}].Checkpoint.Count = {player.Stats.PB[pStyle].Checkpoints.Count}");
#endif

            if (player.Timer.IsRunning && player.ReplayRecorder.IsRecording)
            {
                player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.CHECKPOINT_ZONE_ENTER;
                player.ReplayRecorder.CheckpointEnterSituations.Add(player.Timer.Ticks);
            }

            // Print checkpoint message
            player.HUD.DisplayCheckpointMessages();

            if (!player.Stats.ThisRun.Checkpoints.ContainsKey(checkpoint))
            {
                // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                Checkpoint cp2 = new Checkpoint(checkpoint,
                                                player.Timer.Ticks,
                                                velocity.X,
                                                velocity.Y,
                                                velocity.Z,
                                                -1.0f,
                                                -1.0f,
                                                -1.0f,
                                                0,
                                                1);
                player.Stats.ThisRun.Checkpoints[checkpoint] = cp2;
            }
            else
            {
                player.Stats.ThisRun.Checkpoints[checkpoint].Attempts++;
            }
        }

#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.LightBlue}Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Zone");
#endif
    }

    private static void HandleBonusStartZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
    {
        int bonus = Int32.Parse(Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value);
        player.Timer.Bonus = bonus;

        player.Timer.Reset();
        player.Timer.IsBonusMode = true;


        player.ReplayRecorder.Reset();
        player.ReplayRecorder.Start(); // Start replay recording
        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.START_ZONE_ENTER;
        player.ReplayRecorder.BonusSituations.Add(player.ReplayRecorder.Frames.Count);
        Console.WriteLine($"START_ZONE_ENTER: player.ReplayRecorder.BonusSituations.Add({player.ReplayRecorder.Frames.Count})");

        player.Controller.PrintToCenter($"Bonus Start ({trigger.Entity.Name})");

#if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Bonus start zones) -> player.Timer.IsRunning: {player.Timer.IsRunning}");
        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Bonus start zones) -> !player.Timer.IsBonusMode: {!player.Timer.IsBonusMode}");
#endif
    }

    private void HandleBonusEndZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
    {
        // Get velocities for DB queries
        // Get the velocity of the player - we will be using this values to compare and write to DB
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();
        int pStyle = player.Timer.Style;
        // To-do: verify the bonus trigger being hit!
        int bonus_idx = int.Parse(Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value);

        player.Timer.Stop();
        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_ZONE_ENTER;
        player.ReplayRecorder.BonusSituations.Add(player.Timer.Ticks);

        player.Stats.ThisRun.Ticks = player.Timer.Ticks; // End time for the run
        player.Stats.ThisRun.EndVelX = velocity.X; // End pre speed for the run
        player.Stats.ThisRun.EndVelY = velocity.Y; // End pre speed for the run
        player.Stats.ThisRun.EndVelZ = velocity.Z; // End pre speed for the run

        bool saveBonusTime = false;
        string PracticeString = "";
        if (player.Timer.IsPracticeMode)
            PracticeString = $"({ChatColors.Grey}Practice{ChatColors.Default}) ";

        if (player.Timer.Ticks < CurrentMap.BonusWR[bonus_idx][pStyle].Ticks) // Player beat the Bonus WR
        {
            saveBonusTime = true;
            int timeImprove = CurrentMap.BonusWR[bonus_idx][pStyle].Ticks - player.Timer.Ticks;
            Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuswr_improved",
                player.Controller.PlayerName, bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(CurrentMap.BonusWR[bonus_idx][pStyle].Ticks)]}"
            );
        }
        else if (CurrentMap.BonusWR[bonus_idx][pStyle].ID == -1) // No Bonus record was set on the map
        {
            saveBonusTime = true;
            Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuswr_set",
                player.Controller.PlayerName, bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks)]}"
            );
        }
        else if (player.Stats.BonusPB[bonus_idx][pStyle].Ticks <= 0) // Player first ever PersonalBest for the bonus
        {
            saveBonusTime = true;
            player.Controller.PrintToChat($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuspb_set",
                bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks)]}"
            );
        }
        else if (player.Timer.Ticks < player.Stats.BonusPB[bonus_idx][pStyle].Ticks) // Player beating their existing PersonalBest for the bonus
        {
            saveBonusTime = true;
            int timeImprove = player.Stats.BonusPB[bonus_idx][pStyle].Ticks - player.Timer.Ticks;
            Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuspb_improved",
                player.Controller.PlayerName, bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(player.Stats.PB[pStyle].Ticks)]}"
            );
        }
        else // Player did not beat their existing personal best for the bonus
        {
            player.Controller.PrintToChat($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuspb_missed",
                bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks)]}"
            );
        }

        if (!player.Timer.IsPracticeMode)
        {
            if (saveBonusTime)
            {
                player.ReplayRecorder.IsSaving = true;
                AddTimer(1.0f, async () =>
                {
                    await player.Stats.ThisRun.SaveMapTime(player, bonus: bonus_idx); // Save the Bonus MapTime data
                });
            }
        }
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


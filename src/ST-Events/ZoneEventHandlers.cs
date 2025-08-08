using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
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
    private static ZoneType GetZoneType(string entityName)
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

    /* StartTouch */
    private void StartTouchHandleMapEndZone(Player player, [CallerMemberName] string methodName = "")
    {
        // Get velocities for DB queries
        // Get the velocity of the player - we will be using this values to compare and write to DB
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();
        int pStyle = player.Timer.Style;

        player.Controller.PrintToCenter($"Map End");

        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_ZONE_ENTER;
        player.ReplayRecorder.MapSituations.Add(player.Timer.Ticks);

        player.Stats.ThisRun.RunTime = player.Timer.Ticks; // End time for the Map run
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

            if (player.Timer.Ticks < CurrentMap.WR[pStyle].RunTime) // Player beat the Map WR
            {
                saveMapTime = true;
                int timeImprove = CurrentMap.WR[pStyle].RunTime - player.Timer.Ticks;
                Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mapwr_improved",
                    player.Controller.PlayerName, PlayerHUD.FormatTime(player.Timer.Ticks), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(CurrentMap.WR[pStyle].RunTime)]}"
                );
            }
            else if (CurrentMap.WR[pStyle].ID == -1) // No record was set on the map
            {
                saveMapTime = true;
                Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mapwr_set",
                    player.Controller.PlayerName, PlayerHUD.FormatTime(player.Timer.Ticks)]}"
                );
            }
            else if (player.Stats.PB[pStyle].RunTime <= 0) // Player first ever PersonalBest for the map
            {
                saveMapTime = true;
                player.Controller.PrintToChat($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mappb_set",
                    PlayerHUD.FormatTime(player.Timer.Ticks)]}"
                );
            }
            else if (player.Timer.Ticks < player.Stats.PB[pStyle].RunTime) // Player beating their existing PersonalBest for the map
            {
                saveMapTime = true;
                int timeImprove = player.Stats.PB[pStyle].RunTime - player.Timer.Ticks;
                Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["mappb_improved",
                    player.Controller.PlayerName, PlayerHUD.FormatTime(player.Timer.Ticks), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(player.Stats.PB[pStyle].RunTime)]}"
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
                AddTimer(1.0f, async () => // This determines whether we will have frames for AFTER touch the endZone 
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
                    AddTimer(0.5f, async () => // This determines whether we will have frames for AFTER touch the endZone 
                    {
                        // This calculation is wrong unless we wait for a bit in order for the `END_ZONE_ENTER` to be available in the `Frames` object
                        int stage_run_time = player.ReplayRecorder.Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.END_ZONE_ENTER) - player.ReplayRecorder.Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.STAGE_ZONE_EXIT);

                        // player.Controller.PrintToChat($"{Config.PluginPrefix} [LAST StageWR (Map RUN)] Sending to SaveStageTime: {player.Profile.Name}, {CurrentMap.Stages}, {stage_run_time}");
                        await player.Stats.ThisRun.SaveStageTime(player, CurrentMap.Stages, stage_run_time, true);
                    });
                }

                // This section checks if the PB is better than WR
                if (player.Timer.Ticks < CurrentMap.WR[pStyle].RunTime || CurrentMap.WR[pStyle].ID == -1)
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
                AddTimer(0.5f, async () => // This determines whether we will have frames for AFTER touch the endZone 
                {
                    // This calculation is wrong unless we wait for a bit in order for the `END_ZONE_ENTER` to be available in the `Frames` object
                    int stage_run_time = player.ReplayRecorder.Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.END_ZONE_ENTER) - player.ReplayRecorder.Frames.FindLastIndex(f => f.Situation == ReplayFrameSituation.STAGE_ZONE_EXIT);

                    // player.Controller.PrintToChat($"{Config.PluginPrefix} [LAST StageWR (IsStageMode)] Sending to SaveStageTime: {player.Profile.Name}, {CurrentMap.Stages}, {stage_run_time}");
                    await player.Stats.ThisRun.SaveStageTime(player, CurrentMap.Stages, stage_run_time, true);
                });
            }
        }

#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Red}Map Stop Zone");
#endif
    }

    private static void StartTouchHandleMapStartZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
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

    private void StartTouchHandleStageStartZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
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
                AddTimer(0.5f, async () => // This determines whether we will have frames for AFTER touch the endZone 
                {
                    await player.Stats.ThisRun.SaveStageTime(player, stage - 1, stage_run_time);
                });
                // player.Controller.PrintToChat($"{Config.PluginPrefix} [StageWR (IsStageMode)] Sending to SaveStageTime: {player.Profile.Name}, {stage - 1}, {stage_run_time}");
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
                int stage_run_time = player.Timer.Ticks - player.Stats.ThisRun.RunTime; // player.Stats.ThisRun.RunTime should be the Tick we left the previous Stage zone
                                                                                        // player.Controller.PrintToChat($"{Config.PluginPrefix} [StageWR (Map RUN)] Sending to SaveStageTime: {player.Profile.Name}, {stage - 1}, {stage_run_time}");
                AddTimer(0.5f, async () => // This determines whether we will have frames for AFTER touch the endZone 
                {
                    await player.Stats.ThisRun.SaveStageTime(player, stage - 1, stage_run_time);
                });

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

    private static void StartTouchHandleCheckpointZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
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

    private static void StartTouchHandleBonusStartZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
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

    private void StartTouchHandleBonusEndZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
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

        player.Stats.ThisRun.RunTime = player.Timer.Ticks; // End time for the run
        player.Stats.ThisRun.EndVelX = velocity.X; // End pre speed for the run
        player.Stats.ThisRun.EndVelY = velocity.Y; // End pre speed for the run
        player.Stats.ThisRun.EndVelZ = velocity.Z; // End pre speed for the run

        bool saveBonusTime = false;
        string PracticeString = "";
        if (player.Timer.IsPracticeMode)
            PracticeString = $"({ChatColors.Grey}Practice{ChatColors.Default}) ";

        if (player.Timer.Ticks < CurrentMap.BonusWR[bonus_idx][pStyle].RunTime) // Player beat the Bonus WR
        {
            saveBonusTime = true;
            int timeImprove = CurrentMap.BonusWR[bonus_idx][pStyle].RunTime - player.Timer.Ticks;
            Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuswr_improved",
                player.Controller.PlayerName, bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(CurrentMap.BonusWR[bonus_idx][pStyle].RunTime)]}"
            );
        }
        else if (CurrentMap.BonusWR[bonus_idx][pStyle].ID == -1) // No Bonus record was set on the map
        {
            saveBonusTime = true;
            Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuswr_set",
                player.Controller.PlayerName, bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks)]}"
            );
        }
        else if (player.Stats.BonusPB[bonus_idx][pStyle].RunTime <= 0) // Player first ever PersonalBest for the bonus
        {
            saveBonusTime = true;
            player.Controller.PrintToChat($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuspb_set",
                bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks)]}"
            );
        }
        else if (player.Timer.Ticks < player.Stats.BonusPB[bonus_idx][pStyle].RunTime) // Player beating their existing PersonalBest for the bonus
        {
            saveBonusTime = true;
            int timeImprove = player.Stats.BonusPB[bonus_idx][pStyle].RunTime - player.Timer.Ticks;
            Server.PrintToChatAll($"{Config.PluginPrefix} {PracticeString}{LocalizationService.LocalizerNonNull["bonuspb_improved",
                player.Controller.PlayerName, bonus_idx, PlayerHUD.FormatTime(player.Timer.Ticks), PlayerHUD.FormatTime(timeImprove), PlayerHUD.FormatTime(player.Stats.PB[pStyle].RunTime)]}"
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
                AddTimer(1.0f, async () => // This determines whether we will have frames for AFTER touch the endZone 
                {
                    await player.Stats.ThisRun.SaveMapTime(player, bonus: bonus_idx); // Save the Bonus MapTime data
                });
            }
        }
    }


    /* EndTouch */
    private void EndTouchHandleMapEndZone(Player player, [CallerMemberName] string methodName = "")
    {
        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_ZONE_EXIT;
    }

    private void EndTouchHandleMapStartZone(Player player, [CallerMemberName] string methodName = "")
    {
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();

        // MAP START ZONE
        if (!player.Timer.IsStageMode && !player.Timer.IsBonusMode)
        {
            player.Timer.Start();
            player.Stats.ThisRun.RunTime = player.Timer.Ticks;
            player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.START_ZONE_EXIT;
            player.ReplayRecorder.MapSituations.Add(player.ReplayRecorder.Frames.Count);
            // player.Controller.PrintToChat($"{ChatColors.Red}START_ZONE_EXIT: player.ReplayRecorder.MapSituations.Add({player.ReplayRecorder.Frames.Count})");
            // Console.WriteLine($"START_ZONE_EXIT: player.ReplayRecorder.MapSituations.Add({player.ReplayRecorder.Frames.Count})");
        }

        // Prespeed display
        player.Controller.PrintToCenter($"Prespeed: {velocity.velMag():0} u/s");
        player.Stats.ThisRun.StartVelX = velocity.X; // Start pre speed for the Map run
        player.Stats.ThisRun.StartVelY = velocity.Y; // Start pre speed for the Map run
        player.Stats.ThisRun.StartVelZ = velocity.Z; // Start pre speed for the Map run

#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
#endif
    }

    private void EndTouchHandleStageStartZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
    {
#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value} Start Zone");
        Console.WriteLine($"===================== player.Timer.Checkpoint {player.Timer.Checkpoint} - player.Stats.ThisRun.Checkpoint.Count {player.Stats.ThisRun.Checkpoints.Count}");
#endif
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();
        int stage = Int32.Parse(Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value);

        // Set replay situation
        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.STAGE_ZONE_EXIT;
        player.ReplayRecorder.StageExitSituations.Add(player.ReplayRecorder.Frames.Count);
        player.Stats.ThisRun.RunTime = player.Timer.Ticks;
        // Console.WriteLine($"STAGE_ZONE_EXIT: player.ReplayRecorder.StageExitSituations.Add({player.ReplayRecorder.Frames.Count})");

        // Start the Stage timer
        if (player.Timer.IsStageMode && player.Timer.Stage == stage)
        {
            player.Timer.Start();
            // player.Controller.PrintToChat($"{ChatColors.Green}Started{ChatColors.Default} Stage timer for stage {ChatColors.Green}{stage}{ChatColors.Default}");

            // Show Prespeed for Stages - will be enabled/disabled by the user?
            player.Controller.PrintToCenter($"Stage {stage} - Prespeed: {velocity.velMag().ToString("0")} u/s");
        }
        else if (player.Timer.IsRunning)
        {
#if DEBUG
            Console.WriteLine($"currentCheckpoint.EndVelX {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelX} - velocity.X {velocity.X}");
            Console.WriteLine($"currentCheckpoint.EndVelY {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelY} - velocity.Y {velocity.Y}");
            Console.WriteLine($"currentCheckpoint.EndVelZ {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelZ} - velocity.Z {velocity.Z}");
            Console.WriteLine($"currentCheckpoint.Attempts {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].Attempts}");
#endif

            // Update the Checkpoint object values
            player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelX = velocity.X;
            player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelY = velocity.Y;
            player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelZ = velocity.Z;
            player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndTouch = player.Timer.Ticks;

            // Show Prespeed for Checkpoints - will be enabled/disabled by the user?
            player.Controller.PrintToCenter($"Checkpoint {player.Timer.Checkpoint} - Prespeed: {velocity.velMag():0} u/s");
        }
    }

    private void EndTouchHandleCheckpointZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
    {
#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Checkpoint {Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value} Start Zone");
        Console.WriteLine($"===================== player.Timer.Checkpoint {player.Timer.Checkpoint} - player.Stats.ThisRun.Checkpoint.Count {player.Stats.ThisRun.Checkpoints.Count}");
#endif
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();

        // This will populate the End velocities for the given Checkpoint zone (Stage = Checkpoint when in a Map Run)
        if (player.Timer.Checkpoint != 0 && player.Timer.Checkpoint <= player.Stats.ThisRun.Checkpoints.Count)
        {
#if DEBUG
            Console.WriteLine($"currentCheckpoint.EndVelX {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelX} - velocity.X {velocity.X}");
            Console.WriteLine($"currentCheckpoint.EndVelY {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelY} - velocity.Y {velocity.Y}");
            Console.WriteLine($"currentCheckpoint.EndVelZ {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelZ} - velocity.Z {velocity.Z}");
#endif

            if (player.Timer.IsRunning && player.ReplayRecorder.IsRecording)
            {
                player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.CHECKPOINT_ZONE_EXIT;
                player.ReplayRecorder.CheckpointExitSituations.Add(player.Timer.Ticks);
            }

            // Update the Checkpoint object values
            player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelX = velocity.X;
            player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelY = velocity.Y;
            player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelZ = velocity.Z;
            player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndTouch = player.Timer.Ticks;

            // Show Prespeed for stages - will be enabled/disabled by the user?
            player.Controller.PrintToCenter($"Checkpoint {Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value} - Prespeed: {velocity.velMag():0} u/s");
        }
    }

    private void EndTouchHandleBonusStartZone(Player player, CBaseTrigger trigger, [CallerMemberName] string methodName = "")
    {
#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Bonus {Regex.Match(trigger.Entity!.Name, "[0-9][0-9]?").Value} Start Zone");
#endif
        Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();

        // Replay
        if (player.ReplayRecorder.IsRecording)
        {
            // Saveing 2 seconds before leaving the start zone
            player.ReplayRecorder.Frames.RemoveRange(0, Math.Max(0, player.ReplayRecorder.Frames.Count - (64 * 2))); // Todo make a plugin convar for the time saved before start of run 
        }

        // BONUS START ZONE
        if (!player.Timer.IsStageMode && player.Timer.IsBonusMode)
        {
            player.Timer.Start();
            // Set the CurrentRunData values
            player.Stats.ThisRun.RunTime = player.Timer.Ticks;

            player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.START_ZONE_EXIT;
            player.ReplayRecorder.BonusSituations.Add(player.ReplayRecorder.Frames.Count);
            Console.WriteLine($"START_ZONE_EXIT: player.ReplayRecorder.BonusSituations.Add({player.ReplayRecorder.Frames.Count})");
        }

        // Prespeed display
        player.Controller.PrintToCenter($"Prespeed: {velocity.velMag():0)} u/s");
        player.Stats.ThisRun.StartVelX = velocity.X; // Start pre speed for the Bonus run
        player.Stats.ThisRun.StartVelY = velocity.Y; // Start pre speed for the Bonus run
        player.Stats.ThisRun.StartVelZ = velocity.Z; // Start pre speed for the Bonus run
    }

    private void EndTouchHandleBonusEndZone(Player player, [CallerMemberName] string methodName = "")
    {

    }
}
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;

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
            Console.WriteLine($"CS2 Surf ERROR >> OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
            Exception exception = new($"CS2 Surf ERROR >> OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
            throw exception;
        }
        // Implement Trigger Start Touch Here
        Player player = playerList[client.UserId ?? 0];
#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
#endif

        if (DB == null)
        {
            Exception exception = new Exception("CS2 Surf ERROR >> OnTriggerStartTouch (Map end zone) -> DB object is null, this shouldn't happen.");
            throw exception;
        }

        if (trigger.Entity!.Name != null)
        {
            // Get velocities for DB queries
            // Get the velocity of the player - we will be using this values to compare and write to DB
            Vector_t velocity = player.Controller.PlayerPawn.Value!.AbsVelocity.ToVector_t();
            int pStyle = player.Timer.Style;

            // Map end zones -- hook into map_end
            if (trigger.Entity.Name == "map_end")
            {
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

#if DEBUG
                    Console.WriteLine($@"CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> 
                            ============== INSERT INTO `MapTimes` 
                            (`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`) 
                            VALUES ({player.Profile.ID}, {CurrentMap.ID}, {pStyle}, 0, 0, {player.Stats.ThisRun.Ticks}, 
                            {player.Stats.ThisRun.StartVelX}, {player.Stats.ThisRun.StartVelY}, {player.Stats.ThisRun.StartVelZ}, {velocity.X}, {velocity.Y}, {velocity.Z}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()})
                            ON DUPLICATE KEY UPDATE run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), 
                            start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date);
                        ");
#endif

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

                    // API
                    /*
                    // Add entry in DB for the run
                    if (!player.Timer.IsPracticeMode) {
                        API_CurrentRun? last_stage_time = null;
                        if (CurrentMap.Stages > 0)
                        {
                            int last_exit_tick = player.ReplayRecorder.LastExitTick();
                            int last_enter_tick = player.ReplayRecorder.LastEnterTick();

                            int stage_run_time = player.ReplayRecorder.Frames.Count - 1 - last_exit_tick; // Would like some check on this
                            int time_since_last_enter = player.ReplayRecorder.Frames.Count - 1 - last_enter_tick;

                            int tt = -1;
                            if (last_exit_tick - last_enter_tick > 2*64)
                                tt = last_exit_tick - 2*64;
                            else
                                tt = last_enter_tick;

                            last_stage_time = new API_CurrentRun
                            {
                                    player_id = player.Profile.ID,
                                    map_id = player.CurrMap.ID,
                                    style = style,
                                    type = 2,
                                    stage = CurrentMap.Stages,
                                    run_time = stage_run_time,
                                    run_date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                    replay_frames = player.ReplayRecorder.SerializeReplayPortion(tt, time_since_last_enter)
                            };
                        }
                        AddTimer(1.5f, () => {
                            List<API_Checkpoint> checkpoints = new List<API_Checkpoint>();
                            foreach (var cp in player.Stats.ThisRun.Checkpoint)
                            {
                                checkpoints.Add(new API_Checkpoint
                                {
                                    cp = cp.Key,
                                    run_time = cp.Value.Ticks,
                                    start_vel_x = cp.Value.StartVelX,
                                    start_vel_y = cp.Value.StartVelY,
                                    start_vel_z = cp.Value.StartVelZ,
                                    end_vel_x = cp.Value.EndVelX,
                                    end_vel_y = cp.Value.EndVelY,
                                    end_vel_z = cp.Value.EndVelZ,
                                    end_touch = 0, // ?????
                                    attempts = cp.Value.Attempts
                                });
                            }

                            API_CurrentRun map_time = new API_CurrentRun
                            {
                                player_id = player.Profile.ID,
                                map_id = player.CurrMap.ID,
                                style = style,
                                type = 0,
                                stage = 0,
                                run_time = player.Stats.ThisRun.Ticks,
                                run_date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                checkpoints = checkpoints,
                                replay_frames = player.ReplayRecorder.SerializeReplay()
                            };                                    

                            Task.Run(async () => {
                                System.Console.WriteLine("CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> Saved map time");
                                await ApiCall.POST("/surftimer/savemaptime", map_time);

                                if (last_stage_time != null)
                                {
                                    await ApiCall.POST("/surftimer/savestagetime", last_stage_time);
                                    System.Console.WriteLine("CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> Saved last stage time");
                                    player.Stats.LoadStageTime(player);
                                }

                                player.Stats.LoadMapTime(player);
                                await CurrentMap.ApiGetMapRecordAndTotals(); // Reload the Map record and totals for the HUD
                            });
                        });

                        // This section checks if the PB is better than WR
                        if(player.Timer.Ticks < CurrentMap.WR[pStyle].Ticks || CurrentMap.WR[pStyle].ID == -1)
                        {
                            AddTimer(2f, () => {
                                System.Console.WriteLine("CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> WR PB");
                                CurrentMap.ReplayManager.MapWR.LoadReplayData();

                                AddTimer(1.5f, () => {
                                    CurrentMap.ReplayManager.MapWR.FormatBotName();
                                });
                            });
                        }
                    }
                    */
                }
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

            // Map start zones -- hook into map_start, (s)tage1_start
            else if (trigger.Entity.Name.Contains("map_start") ||
                    trigger.Entity.Name.Contains("s1_start") ||
                    trigger.Entity.Name.Contains("stage1_start")
            )
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
                    player.Controller.PrintToCenter($"Map Start ({trigger.Entity.Name})");

#if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
#endif
                }
                else
                {
                    player.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["reset_delay"]}");
                }
            }

            // Stage start zones -- hook into (s)tage#_start
            else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
            {
                int stage = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);

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

            // Map checkpoint zones -- hook into map_(c)heck(p)oint#
            else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
            {
                int checkpoint = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);
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

            // Bonus start zones -- hook into (b)onus#_start
            else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_start$").Success)
            {
                int bonus = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);
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

            // Bonus end zones -- hook into (b)onus#_end
            else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_end$").Success && player.Timer.IsBonusMode && player.Timer.IsRunning)
            {
                // To-do: verify the bonus trigger being hit!
                int bonus_idx = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);

                player.Timer.Stop();
                player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_ZONE_ENTER;
                player.ReplayRecorder.BonusSituations.Add(player.Timer.Ticks);

                player.Stats.ThisRun.Ticks = player.Timer.Ticks; // End time for the run
                player.Stats.ThisRun.EndVelX = velocity.X; // End pre speed for the run
                player.Stats.ThisRun.EndVelY = velocity.Z; // End pre speed for the run
                player.Stats.ThisRun.EndVelZ = velocity.Y; // End pre speed for the run

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

                // To-do: save to DB
                if (!player.Timer.IsPracticeMode)
                {
                    /*
                    AddTimer(1.5f, () =>
                    {
                        API_CurrentRun bonus_time = new API_CurrentRun
                        {
                            player_id = player.Profile.ID,
                            map_id = player.CurrMap.ID,
                            style = pStyle,
                            type = 1,
                            stage = bonus_idx,
                            run_time = player.Stats.ThisRun.Ticks,
                            run_date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            replay_frames = player.ReplayRecorder.SerializeReplay()
                        };

                        Task.Run(async () =>
                        {
                            await ApiMethod.POST("/surftimer/savebonustime", bonus_time);
                            player.Stats.LoadBonusTime(player);
                            await CurrentMap.Get_Map_Record_Runs(); // Reload the Map record and totals for the HUD
                                                                    // await CurrentMap.ApiGetMapRecordAndTotals(); // Reload the Map record and totals for the HUD
                        });
                    });
                    */
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
            player.Stats.StagePB[stage][pStyle] != null && player.Stats.StagePB[stage][pStyle].Ticks > stage_run_time
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


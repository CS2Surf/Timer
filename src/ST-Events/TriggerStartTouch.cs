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
        // CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
        CBaseTrigger trigger = new CBaseTrigger(caller.Handle);
        // CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
        CBaseEntity entity = new CBaseEntity(activator.Handle);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        if (!client.IsValid || !client.PawnIsAlive || !playerList.ContainsKey((int)client.UserId!)) // !playerList.ContainsKey((int)client.UserId!) make sure to not check for user_id that doesnt exists
        {
            return HookResult.Continue;
        }
        else
        {
            // To-do: Sometimes this triggers before `OnPlayerConnect` and `playerList` does not contain the player how is this possible :thonk:
            if (!playerList.ContainsKey(client.UserId ?? 0))
            {
                Console.WriteLine($"CS2 Surf ERROR >> OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
                throw new Exception($"CS2 Surf ERROR >> OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
                // return HookResult.Continue;
            }
            // Implement Trigger Start Touch Here
            Player player = playerList[client.UserId ?? 0];
            #if DEBUG
            player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
            #endif

            if (trigger.Entity!.Name != null)
            {
                // Get velocities for DB queries
                // Get the velocity of the player - we will be using this values to compare and write to DB
                float velocity_x = player.Controller.PlayerPawn.Value!.AbsVelocity.X;
                float velocity_y = player.Controller.PlayerPawn.Value!.AbsVelocity.Y;
                float velocity_z = player.Controller.PlayerPawn.Value!.AbsVelocity.Z;
                float velocity = (float)Math.Sqrt(velocity_x * velocity_x + velocity_y * velocity_y + velocity_z + velocity_z);
                int style = player.Timer.Style;

                // Map end zones -- hook into map_end
                if (trigger.Entity.Name == "map_end")
                {
                    player.Controller.PrintToCenter($"Map End");
                    // MAP END ZONE
                    if (player.Timer.IsRunning)
                    {
                        player.Timer.Stop();
                        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_RUN;

                        player.Stats.ThisRun.Ticks = player.Timer.Ticks; // End time for the run
                        player.Stats.ThisRun.EndVelX = velocity_x; // End pre speed for the run
                        player.Stats.ThisRun.EndVelY = velocity_y; // End pre speed for the run
                        player.Stats.ThisRun.EndVelZ = velocity_z; // End pre speed for the run

                        string PracticeString = "";
                        if (player.Timer.IsPracticeMode)
                            PracticeString = $"({ChatColors.Grey}Practice{ChatColors.Default}) ";

                        // To-do: make Style (currently 0) be dynamic
                        if (player.Stats.PB[style].Ticks <= 0) // Player first ever PersonalBest for the map
                        {
                            Server.PrintToChatAll($"{PluginPrefix} {PracticeString}{player.Controller.PlayerName} finished the map in {ChatColors.Gold}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} ({player.Timer.Ticks})!");
                        }
                        else if (player.Timer.Ticks < player.Stats.PB[style].Ticks) // Player beating their existing PersonalBest for the map
                        {
                            Server.PrintToChatAll($"{PluginPrefix} {PracticeString}{ChatColors.Lime}{player.Profile.Name}{ChatColors.Default} beat their PB in {ChatColors.Gold}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} (Old: {ChatColors.BlueGrey}{PlayerHUD.FormatTime(player.Stats.PB[style].Ticks)}{ChatColors.Default})!");
                        }
                        else // Player did not beat their existing PersonalBest for the map
                        {
                            player.Controller.PrintToChat($"{PluginPrefix} {PracticeString}You finished the map in {ChatColors.Yellow}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default}!");
                            return HookResult.Continue; // Exit here so we don't write to DB
                        }

                        if (DB == null)
                            throw new Exception("CS2 Surf ERROR >> OnTriggerStartTouch (Map end zone) -> DB object is null, this shouldn't happen.");

                        
                        player.Stats.PB[style].Ticks = player.Timer.Ticks; // Reload the run_time for the HUD and also assign for the DB query

                        #if DEBUG
                        Console.WriteLine($@"CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> 
                            ============== INSERT INTO `MapTimes` 
                            (`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`) 
                            VALUES ({player.Profile.ID}, {CurrentMap.ID}, {style}, 0, 0, {player.Stats.ThisRun.Ticks}, 
                            {player.Stats.ThisRun.StartVelX}, {player.Stats.ThisRun.StartVelY}, {player.Stats.ThisRun.StartVelZ}, {velocity_x}, {velocity_y}, {velocity_z}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()})
                            ON DUPLICATE KEY UPDATE run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), 
                            start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date);
                        ");
                        #endif

                        // Add entry in DB for the run
                        if (!player.Timer.IsPracticeMode) {
                            AddTimer(1.5f, async () => {
                                player.Stats.ThisRun.SaveMapTime(player, DB); // Save the MapTime PB data
                                player.Stats.LoadMapTimesData(player, DB); // Load the MapTime PB data again (will refresh the MapTime ID for the Checkpoints query)
                                await player.Stats.ThisRun.SaveCurrentRunCheckpoints(player, DB); // Save this run's checkpoints
                                player.Stats.LoadCheckpointsData(DB); // Reload checkpoints for the run - we should really have this in `SaveMapTime` as well but we don't re-load PB data inside there so we need to do it here
                                CurrentMap.GetMapRecordAndTotals(DB); // Reload the Map record and totals for the HUD
                            });

                            // This section checks if the PB is better than WR
                            if(player.Timer.Ticks < CurrentMap.WR[player.Timer.Style].Ticks || CurrentMap.WR[player.Timer.Style].ID == -1)
                            {
                                int WrIndex = CurrentMap.ReplayBots.Count-1; // As the ReplaysBot is set, WR Index will always be at the end of the List
                                AddTimer(2f, () => {
                                    CurrentMap.ReplayBots[WrIndex].Stat_MapTimeID = CurrentMap.WR[player.Timer.Style].ID;
                                    CurrentMap.ReplayBots[WrIndex].LoadReplayData(DB!);
                                    CurrentMap.ReplayBots[WrIndex].ResetReplay();
                                });
                            }
                        }
                    }

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Red}Map Stop Zone");
                    #endif
                }

                // Map start zones -- hook into map_start, (s)tage1_start
                else if (trigger.Entity.Name.Contains("map_start") ||
                        trigger.Entity.Name.Contains("s1_start") ||
                        trigger.Entity.Name.Contains("stage1_start"))
                {
                    player.ReplayRecorder.Start(); // Start replay recording

                    player.Timer.Reset();
                    player.Stats.ThisRun.Checkpoint.Clear(); // I have the suspicion that the `Timer.Reset()` does not properly reset this object :thonk:
                    player.Controller.PrintToCenter($"Map Start ({trigger.Entity.Name})");

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
                    // player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> KeyValues: {trigger.Entity.KeyValues3}");
                    #endif
                }

                // Stage start zones -- hook into (s)tage#_start
                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
                {
                    int stage = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);
                    player.Timer.Stage = stage;

                    #if DEBUG
                    Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Timer.IsRunning: {player.Timer.IsRunning}");
                    Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> !player.Timer.IsStageMode: {!player.Timer.IsStageMode}");
                    Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.ThisRun.Checkpoint.Count <= stage: {player.Stats.ThisRun.Checkpoint.Count <= stage}");
                    #endif

                    // This should patch up re-triggering *player.Stats.ThisRun.Checkpoint.Count < stage*
                    if (player.Timer.IsRunning && !player.Timer.IsStageMode && player.Stats.ThisRun.Checkpoint.Count < stage)
                    {
                        player.Timer.Checkpoint = stage - 1; // Stage = Checkpoint when in a run on a Staged map

                        #if DEBUG
                        Console.WriteLine($"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `stage`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value)}");
                        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.PB[{style}].Checkpoint.Count = {player.Stats.PB[style].Checkpoint.Count}");
                        #endif

                        // Print checkpoint message
                        player.HUD.DisplayCheckpointMessages(PluginPrefix);

                        // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                        Checkpoint cp2 = new Checkpoint(player.Timer.Checkpoint,
                                                        player.Timer.Ticks,
                                                        velocity_x,
                                                        velocity_y,
                                                        velocity_z,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        0);
                        player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint] = cp2;
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
                    if (player.Timer.IsRunning && !player.Timer.IsStageMode && player.Stats.ThisRun.Checkpoint.Count < checkpoint)
                    {
                        #if DEBUG
                        Console.WriteLine($"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `checkpoint`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value)}");
                        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Checkpoint zones) -> player.Stats.PB[{style}].Checkpoint.Count = {player.Stats.PB[style].Checkpoint.Count}");
                        #endif
                        
                        // Print checkpoint message
                        player.HUD.DisplayCheckpointMessages(PluginPrefix);

                        // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                        Checkpoint cp2 = new Checkpoint(checkpoint,
                                                        player.Timer.Ticks,
                                                        velocity_x,
                                                        velocity_y,
                                                        velocity_z,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        0);
                        player.Stats.ThisRun.Checkpoint[checkpoint] = cp2;
                    }

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.LightBlue}Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Zone");
                    #endif
                }
            
                // Bonus start zones -- hook into (b)onus#_start
                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_start$").Success)
                {
                    // We only want this working if they're in bonus mode, ignore otherwise.
                    if (player.Timer.IsBonusMode) 
                    {
                        player.ReplayRecorder.Start(); // Start replay recording

                        player.Timer.Reset();
                        player.Timer.IsBonusMode = true;
                        int bonus = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);
                        player.Timer.Bonus = bonus;

                        player.Controller.PrintToCenter($"Bonus Start ({trigger.Entity.Name})");

                        #if DEBUG
                        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Bonus start zones) -> player.Timer.IsRunning: {player.Timer.IsRunning}");
                        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Bonus start zones) -> !player.Timer.IsBonusMode: {!player.Timer.IsBonusMode}");
                        #endif
                    }
                }

                // Bonus end zones -- hook into (b)onus#_end
                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_end$").Success)
                {
                    // We only want this working if they're in bonus mode, ignore otherwise.
                    if (player.Timer.IsBonusMode && player.Timer.IsRunning) 
                    {
                        // To-do: verify the bonus trigger being hit!
                        int bonus = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);
                        if (bonus != player.Timer.Bonus)
                        {
                            // Exit hook as this end zone is not relevant to the player's current bonus
                            return HookResult.Continue;
                        }

                        player.Timer.Stop();
                        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_RUN;

                        // To-do: Bonus prespeeds

                        string PracticeString = "";
                        if (player.Timer.IsPracticeMode)
                            PracticeString = $"({ChatColors.Grey}Practice{ChatColors.Default}) ";
                    
                        // To-do: make Style (currently 0) be dynamic
                        if (player.Stats.BonusPB[bonus][style].Ticks <= 0) // Player first ever PB for the bonus
                        {
                            Server.PrintToChatAll($"{PluginPrefix} {PracticeString}{player.Controller.PlayerName} finished bonus {bonus} in {ChatColors.Gold}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} ({player.Timer.Ticks})!");
                        }
                        else if (player.Timer.Ticks < player.Stats.BonusPB[bonus][style].Ticks) // Player beating their existing PB for the bonus
                        {
                            Server.PrintToChatAll($"{PluginPrefix} {PracticeString}{ChatColors.Lime}{player.Profile.Name}{ChatColors.Default} beat their bonus {bonus} PB in {ChatColors.Gold}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} (Old: {ChatColors.BlueGrey}{PlayerHUD.FormatTime(player.Stats.BonusPB[bonus][style].Ticks)}{ChatColors.Default})!");
                        }
                        else // Player did not beat their existing personal best for the bonus
                        {
                            player.Controller.PrintToChat($"{PluginPrefix} {PracticeString}You finished bonus {bonus} in {ChatColors.Yellow}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default}!");
                            return HookResult.Continue; // Exit here so we don't write to DB
                        }

                        if (DB == null)
                            throw new Exception("CS2 Surf ERROR >> OnTriggerStartTouch (Bonus end zone) -> DB object is null, this shouldn't happen.");
                    
                        player.Stats.BonusPB[bonus][style].Ticks = player.Timer.Ticks; // Reload the run_time for the HUD and also assign for the DB query
                        
                        // To-do: save to DB
                        if (!player.Timer.IsPracticeMode)
                        {
                            AddTimer(1.5f, () => {
                                player.Stats.ThisRun.SaveMapTime(player, DB, bonus); // Save the bonus time PB data
                                player.Stats.LoadMapTimesData(player, DB); // Load the MapTime PB data again (will refresh the MapTime ID for the Checkpoints query)
                                CurrentMap.GetMapRecordAndTotals(DB); // Reload the Map record and totals for the HUD
                            });
                        }
                    }
                }
            }

            return HookResult.Continue;
        }
    }
}
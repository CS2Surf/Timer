using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API;

namespace SurfTimer;

public partial class SurfTimer
{
    // Trigger start touch handler - CBaseTrigger_StartTouchFunc
    internal HookResult OnTriggerStartTouch(DynamicHook handler)
    {
        CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
        CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        if (client.IsBot || !client.IsValid || !client.PawnIsAlive)
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
                float velocity = (float)Math.Sqrt(player.Controller.PlayerPawn.Value!.AbsVelocity.X * player.Controller.PlayerPawn.Value!.AbsVelocity.X
                                            + player.Controller.PlayerPawn.Value!.AbsVelocity.Y * player.Controller.PlayerPawn.Value!.AbsVelocity.Y
                                            + player.Controller.PlayerPawn.Value!.AbsVelocity.Z * player.Controller.PlayerPawn.Value!.AbsVelocity.Z);
                float velocity_x = player.Controller.PlayerPawn.Value!.AbsVelocity.X;
                float velocity_y = player.Controller.PlayerPawn.Value!.AbsVelocity.Y;
                float velocity_z = player.Controller.PlayerPawn.Value!.AbsVelocity.Z;

                // Map end zones -- hook into map_end
                if (trigger.Entity.Name == "map_end")
                {
                    player.Controller.PrintToCenter($"Map End");
                    // MAP END ZONE
                    if (player.Timer.IsRunning)
                    {
                        player.Timer.Stop();
                        player.Stats.ThisRun.EndVelX = velocity_x; // End pre speed for the run
                        player.Stats.ThisRun.EndVelY = velocity_y; // End pre speed for the run
                        player.Stats.ThisRun.EndVelZ = velocity_z; // End pre speed for the run

                        // To-do: make Style (currently 0) be dynamic
                        if (player.Stats.PB[0].RunTime <= 0) // Player first ever PersonalBest for the map
                        {
                            Server.PrintToChatAll($"{PluginPrefix} {player.Controller.PlayerName} finished the map in {ChatColors.Gold}{player.HUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} ({player.Timer.Ticks})!");
                        }
                        else if (player.Timer.Ticks < player.Stats.PB[0].RunTime) // Player beating their existing PersonalBest for the map
                        {
                            Server.PrintToChatAll($"{PluginPrefix} {ChatColors.Lime}{player.Profile.Name}{ChatColors.Default} beat their PB in {ChatColors.Gold}{player.HUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} (Old: {ChatColors.BlueGrey}{player.HUD.FormatTime(player.Stats.PB[0].RunTime)}{ChatColors.Default})!");
                        }
                        else // Player did not beat their existing PersonalBest for the map
                        {
                            player.Controller.PrintToChat($"{PluginPrefix} You finished the map in {ChatColors.Yellow}{player.HUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default}!");
                            return HookResult.Continue; // Exit here so we don't write to DB
                        }

                        if (DB == null)
                            throw new Exception("CS2 Surf ERROR >> OnTriggerStartTouch (Map end zone) -> DB object is null, this shouldn't happen.");

                        
                        player.Stats.PB[0].RunTime = player.Timer.Ticks; // Reload the run_time for the HUD and also assign for the DB query

                        #if DEBUG
                        Console.WriteLine($"CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> " +
                                                                    $"============== INSERT INTO `MapTimes` " +
                                                                    $"(`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`) " +
                                                                    $"VALUES ({player.Profile.ID}, {CurrentMap.ID}, 0, 0, 0, {player.Stats.PB[0].RunTime}, " +
                                                                    $"{player.Stats.ThisRun.StartVelX}, {player.Stats.ThisRun.StartVelY}, {player.Stats.ThisRun.StartVelZ}, {velocity_x}, {velocity_y}, {velocity_z}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}) " + // To-do: get the `start_vel` values for the run from CP implementation
                                                                    $"ON DUPLICATE KEY UPDATE run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), " +
                                                                    $"start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date);");
                        #endif

                        // Add entry in DB for the run
                        player.Stats.PB[0].SaveMapTime(player, DB); // Save the MapTime PB data
                        player.Stats.LoadMapTimesData(player, DB); // Load the MapTime PB data again (will refresh the MapTime ID for the Checkpoints query)
                        player.Stats.PB[0].SaveCurrentRunCheckpoints(player, DB); // Save the Checkpoints PB data
                        player.Stats.PB[0].LoadCheckpointsForRun(DB); // Reload checkpoints for the run - we should really have this in `SaveMapTime` as well but we don't re-load PB data inside there so we need to do it here
                        CurrentMap.GetMapRecordAndTotals(DB); // Reload the Map record and totals for the HUD
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
                    player.Timer.Reset();
                    player.Controller.PrintToCenter($"Map Start ({trigger.Entity.Name})");

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
                    // player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> KeyValues: {trigger.Entity.KeyValues3}");
                    #endif
                }

                // Stage start zones -- hook into (s)tage#_start
                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
                {
                    int stage = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1;
                    player.Timer.Stage = stage;

                    #if DEBUG
                    Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Timer.IsRunning: {player.Timer.IsRunning}");
                    Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> !player.Timer.IsStageMode: {!player.Timer.IsStageMode}");
                    Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.ThisRun.Checkpoint.Count <= stage: {player.Stats.ThisRun.Checkpoint.Count <= stage}");
                    #endif

                    // This should patch up re-triggering *player.Stats.ThisRun.Checkpoint.Count < stage*
                    if (player.Timer.IsRunning && !player.Timer.IsStageMode && player.Stats.ThisRun.Checkpoint.Count <= stage)
                    {
                        player.Timer.Checkpoint = stage; // Stage = Checkpoint when in a run on a Staged map

                        #if DEBUG
                        Console.WriteLine($"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `stage`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1}");
                        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.PB[0].Checkpoint.Count = {player.Stats.PB[0].Checkpoint.Count}");
                        #endif

                        // Print checkpoint message
                        player.HUD.DisplayCheckpointMessages(PluginPrefix);

                        // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                        PersonalBest.CheckpointObject cp2 = new PersonalBest.CheckpointObject(stage,
                                                        player.Timer.Ticks, // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
                                                        player.Timer.Ticks, // To-do: this was supposed to be the ticks but that is used for run_time for HUD
                                                        velocity_x,
                                                        velocity_y,
                                                        velocity_z,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        0);
                        player.Stats.ThisRun.Checkpoint[stage] = cp2;
                    }

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    #endif
                }

                // Map checkpoint zones -- hook into map_(c)heck(p)oint#
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
                {
                    int checkpoint = Int32.Parse(Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Value) - 1;
                    player.Timer.Checkpoint = checkpoint;

                    // This should patch up re-triggering *player.Stats.ThisRun.Checkpoint.Count < checkpoint*
                    if (player.Timer.IsRunning && !player.Timer.IsStageMode && player.Stats.ThisRun.Checkpoint.Count < checkpoint)
                    {
                        #if DEBUG
                        Console.WriteLine($"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `checkpoint`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1}");
                        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Checkpoint zones) -> player.Stats.PB[0].Checkpoint.Count = {player.Stats.PB[0].Checkpoint.Count}");
                        #endif
                        
                        // Print checkpoint message
                        player.HUD.DisplayCheckpointMessages(PluginPrefix);

                        // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                        PersonalBest.CheckpointObject cp2 = new PersonalBest.CheckpointObject(checkpoint,
                                                        player.Timer.Ticks, // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
                                                        player.Timer.Ticks, // To-do: this was supposed to be the ticks but that is used for run_time for HUD
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
            }

            return HookResult.Continue;
        }
    }
}
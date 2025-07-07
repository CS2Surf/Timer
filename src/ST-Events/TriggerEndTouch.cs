using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

public partial class SurfTimer
{
    /// <summary>
    /// Handler for trigger end touch hook - CBaseTrigger_EndTouchFunc.
    /// 
    /// Sometimes this gets triggered when a player joins the server (for the 2nd time) so we assign `client` to `null` to bypass the error.
    /// - T
    /// </summary>
    internal HookResult OnTriggerEndTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        CBaseTrigger trigger = new CBaseTrigger(caller.Handle);
        CBaseEntity entity = new CBaseEntity(activator.Handle);
        CCSPlayerController client = null!;

        try
        {
            client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        }
        catch (System.Exception)
        {
            Console.WriteLine($"===================== [ERROR] OnTriggerEndTouch -> Could not assign `client` (name: {name})");
        }

        if (client == null || !client.IsValid || client.UserId == -1 || !client.PawnIsAlive || !playerList.ContainsKey((int)client.UserId!)) // `client.IsBot` throws error in server console when going to spectator? + !playerList.ContainsKey((int)client.UserId!) make sure to not check for user_id that doesnt exists
        {
            return HookResult.Continue;
        }
        else
        {
            // Implement Trigger End Touch Here
            Player player = playerList[client.UserId ?? 0];
#if DEBUG
            player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_EndTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
#endif

            if (trigger.Entity!.Name != null)
            {
                // Get velocities for DB queries
                // Get the velocity of the player - we will be using this values to compare and write to DB
                float velocity_x = player.Controller.PlayerPawn.Value!.AbsVelocity.X;
                float velocity_y = player.Controller.PlayerPawn.Value!.AbsVelocity.Y;
                float velocity_z = player.Controller.PlayerPawn.Value!.AbsVelocity.Z;
                float velocity = (float)Math.Sqrt(velocity_x * velocity_x + velocity_y * velocity_y + velocity_z + velocity_z);

                // Map start zones -- hook into map_start, (s)tage1_start
                if (trigger.Entity.Name.Contains("map_start") ||
                    trigger.Entity.Name.Contains("s1_start") ||
                    trigger.Entity.Name.Contains("stage1_start"))
                {
                    // MAP START ZONE
                    if (!player.Timer.IsStageMode && !player.Timer.IsBonusMode)
                    {
                        player.Timer.Start();
                        player.Stats.ThisRun.Ticks = player.Timer.Ticks;
                        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.START_ZONE_EXIT;
                        player.ReplayRecorder.MapSituations.Add(player.ReplayRecorder.Frames.Count);
                        // player.Controller.PrintToChat($"{ChatColors.Red}START_ZONE_EXIT: player.ReplayRecorder.MapSituations.Add({player.ReplayRecorder.Frames.Count})");
                        // Console.WriteLine($"START_ZONE_EXIT: player.ReplayRecorder.MapSituations.Add({player.ReplayRecorder.Frames.Count})");
                    }

                    // Prespeed display
                    player.Controller.PrintToCenter($"Prespeed: {velocity.ToString("0")} u/s");
                    player.Stats.ThisRun.StartVelX = velocity_x; // Start pre speed for the Map run
                    player.Stats.ThisRun.StartVelY = velocity_y; // Start pre speed for the Map run
                    player.Stats.ThisRun.StartVelZ = velocity_z; // Start pre speed for the Map run

#if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
#endif
                }

                // Map end zones -- hook into map_end
                else if (trigger.Entity.Name == "map_end")
                {
                    player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_ZONE_EXIT;
                }

                // Stage start zones -- hook into (s)tage#_start
                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
                {
#if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    Console.WriteLine($"===================== player.Timer.Checkpoint {player.Timer.Checkpoint} - player.Stats.ThisRun.Checkpoint.Count {player.Stats.ThisRun.Checkpoints.Count}");
#endif

                    int stage = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);

                    // Set replay situation
                    player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.STAGE_ZONE_EXIT;
                    player.ReplayRecorder.StageExitSituations.Add(player.ReplayRecorder.Frames.Count);
                    player.Stats.ThisRun.Ticks = player.Timer.Ticks;
                    // Console.WriteLine($"STAGE_ZONE_EXIT: player.ReplayRecorder.StageExitSituations.Add({player.ReplayRecorder.Frames.Count})");

                    // Start the Stage timer
                    if (player.Timer.IsStageMode && player.Timer.Stage == stage)
                    {
                        player.Timer.Start();
                        // player.Controller.PrintToChat($"{ChatColors.Green}Started{ChatColors.Default} Stage timer for stage {ChatColors.Green}{stage}{ChatColors.Default}");

                        // Show Prespeed for Stages - will be enabled/disabled by the user?
                        player.Controller.PrintToCenter($"Stage {stage} - Prespeed: {velocity.ToString("0")} u/s");
                    }
                    else if (player.Timer.IsRunning)
                    {
#if DEBUG
                        Console.WriteLine($"currentCheckpoint.EndVelX {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelX} - velocity_x {velocity_x}");
                        Console.WriteLine($"currentCheckpoint.EndVelY {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelY} - velocity_y {velocity_y}");
                        Console.WriteLine($"currentCheckpoint.EndVelZ {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelZ} - velocity_z {velocity_z}");
                        Console.WriteLine($"currentCheckpoint.Attempts {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].Attempts}");
#endif

                        // Update the Checkpoint object values
                        player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelX = velocity_x;
                        player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelY = velocity_y;
                        player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelZ = velocity_z;
                        player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndTouch = player.Timer.Ticks;

                        // Show Prespeed for Checkpoints - will be enabled/disabled by the user?
                        player.Controller.PrintToCenter($"Checkpoint {player.Timer.Checkpoint} - Prespeed: {velocity.ToString("0")} u/s");
                    }
                }

                // Checkpoint zones -- hook into "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$" map_c(heck)p(oint) 
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
                {
#if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    Console.WriteLine($"===================== player.Timer.Checkpoint {player.Timer.Checkpoint} - player.Stats.ThisRun.Checkpoint.Count {player.Stats.ThisRun.Checkpoints.Count}");
#endif

                    // This will populate the End velocities for the given Checkpoint zone (Stage = Checkpoint when in a Map Run)
                    if (player.Timer.Checkpoint != 0 && player.Timer.Checkpoint <= player.Stats.ThisRun.Checkpoints.Count)
                    {
#if DEBUG
                        Console.WriteLine($"currentCheckpoint.EndVelX {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelX} - velocity_x {velocity_x}");
                        Console.WriteLine($"currentCheckpoint.EndVelY {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelY} - velocity_y {velocity_y}");
                        Console.WriteLine($"currentCheckpoint.EndVelZ {player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelZ} - velocity_z {velocity_z}");
#endif

                        if (player.Timer.IsRunning && player.ReplayRecorder.IsRecording)
                        {
                            player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.CHECKPOINT_ZONE_EXIT;
                            player.ReplayRecorder.CheckpointExitSituations.Add(player.Timer.Ticks);
                        }

                        // Update the Checkpoint object values
                        player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelX = velocity_x;
                        player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelY = velocity_y;
                        player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndVelZ = velocity_z;
                        player.Stats.ThisRun.Checkpoints[player.Timer.Checkpoint].EndTouch = player.Timer.Ticks;

                        // Show Prespeed for stages - will be enabled/disabled by the user?
                        player.Controller.PrintToCenter($"Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} - Prespeed: {velocity.ToString("0")} u/s");
                    }
                }

                // Bonus start zones -- hook into (b)onus#_start
                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_start$").Success)
                {
#if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Bonus {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
#endif

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
                        player.Stats.ThisRun.Ticks = player.Timer.Ticks;

                        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.START_ZONE_EXIT;
                        player.ReplayRecorder.BonusSituations.Add(player.ReplayRecorder.Frames.Count);
                        Console.WriteLine($"START_ZONE_EXIT: player.ReplayRecorder.BonusSituations.Add({player.ReplayRecorder.Frames.Count})");
                    }

                    // Prespeed display
                    player.Controller.PrintToCenter($"Prespeed: {velocity.ToString("0")} u/s");
                    player.Stats.ThisRun.StartVelX = velocity_x; // Start pre speed for the Bonus run
                    player.Stats.ThisRun.StartVelY = velocity_y; // Start pre speed for the Bonus run
                    player.Stats.ThisRun.StartVelZ = velocity_z; // Start pre speed for the Bonus run
                }
            }

            return HookResult.Continue;
        }
    }
}
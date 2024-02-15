using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

public partial class SurfTimer
{
    /// <summary>
    /// Handler for trigger end touch hook - CBaseTrigger_EndTouchFunc
    /// </summary>
    /// <returns>CounterStrikeSharp.API.Core.HookResult</returns>
    /// <exception cref="Exception"></exception>
    internal HookResult OnTriggerEndTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        // CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
        CBaseTrigger trigger = new CBaseTrigger(caller.Handle);
        // CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
        CBaseEntity entity = new CBaseEntity(activator.Handle);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        if (!client.IsValid || client.UserId == -1 || !client.PawnIsAlive || !playerList.ContainsKey((int)client.UserId!)) // `client.IsBot` throws error in server console when going to spectator? + !playerList.ContainsKey((int)client.UserId!) make sure to not check for user_id that doesnt exists
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
                    // Replay
                    if(player.ReplayRecorder.IsRecording) 
                    {
                        // Saveing 2 seconds before leaving the start zone
                        player.ReplayRecorder.Frames.RemoveRange(0, Math.Max(0, player.ReplayRecorder.Frames.Count - (64*2))); // Todo make a plugin convar for the time saved before start of run 
                    }

                    // MAP START ZONE
                    if (!player.Timer.IsStageMode && !player.Timer.IsBonusMode)
                    {
                        player.Timer.Start();
                        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.START_RUN;
                    }

                    /* Revisit
                    // Wonky Prespeed check
                    // To-do: make the teleportation a bit more elegant (method in a class or something)
                    if (velocity > 666.0)
                    {
                        player.Controller.PrintToChat(
                            $"{PluginPrefix} {ChatColors.Red}You are going too fast! ({velocity.ToString("0")} u/s)");
                        player.Timer.Reset();
                        if (CurrentMap.StartZone != new Vector(0,0,0))
                            Server.NextFrame(() => player.Controller.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0,0,0), new Vector(0,0,0)));
                    }
                    */

                    // Prespeed display
                    player.Controller.PrintToCenter($"Prespeed: {velocity.ToString("0")} u/s");
                    player.Stats.ThisRun.StartVelX = velocity_x; // Start pre speed for the run
                    player.Stats.ThisRun.StartVelY = velocity_y; // Start pre speed for the run
                    player.Stats.ThisRun.StartVelZ = velocity_z; // Start pre speed for the run

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
                    #endif
                }

                // Stage start zones -- hook into (s)tage#_start
                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
                {
                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    Console.WriteLine($"===================== player.Timer.Checkpoint {player.Timer.Checkpoint} - player.Stats.ThisRun.Checkpoint.Count {player.Stats.ThisRun.Checkpoint.Count}");
                    #endif

                    // This will populate the End velocities for the given Checkpoint zone (Stage = Checkpoint when in a Map Run)
                    if (player.Timer.Checkpoint != 0 && player.Timer.Checkpoint <= player.Stats.ThisRun.Checkpoint.Count)
                    {
                        var currentCheckpoint = player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint];
                        #if DEBUG
                        Console.WriteLine($"currentCheckpoint.EndVelX {currentCheckpoint.EndVelX} - velocity_x {velocity_x}");
                        Console.WriteLine($"currentCheckpoint.EndVelY {currentCheckpoint.EndVelY} - velocity_y {velocity_y}");
                        Console.WriteLine($"currentCheckpoint.EndVelZ {currentCheckpoint.EndVelZ} - velocity_z {velocity_z}");
                        Console.WriteLine($"currentCheckpoint.Attempts {currentCheckpoint.Attempts}");
                        #endif

                        // Update the values
                        currentCheckpoint.EndVelX = velocity_x;
                        currentCheckpoint.EndVelY = velocity_y;
                        currentCheckpoint.EndVelZ = velocity_z;
                        currentCheckpoint.EndTouch = player.Timer.Ticks;
                        currentCheckpoint.Attempts += 1;
                        // Assign the updated currentCheckpoint back to the list as `currentCheckpoint` is supposedly a copy of the original object
                        player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint] = currentCheckpoint;

                        // Show Prespeed for stages - will be enabled/disabled by the user?
                        player.Controller.PrintToCenter($"Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} - Prespeed: {velocity.ToString("0")} u/s");
                    }
                    else
                    {
                        // Handle the case where the index is out of bounds
                    }
                }

                // Checkpoint zones -- hook into "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$" map_c(heck)p(oint) 
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
                {
                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    Console.WriteLine($"===================== player.Timer.Checkpoint {player.Timer.Checkpoint} - player.Stats.ThisRun.Checkpoint.Count {player.Stats.ThisRun.Checkpoint.Count}");
                    #endif

                    // This will populate the End velocities for the given Checkpoint zone (Stage = Checkpoint when in a Map Run)
                    if (player.Timer.Checkpoint != 0 && player.Timer.Checkpoint <= player.Stats.ThisRun.Checkpoint.Count)
                    {
                        var currentCheckpoint = player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint];
                        #if DEBUG
                        Console.WriteLine($"currentCheckpoint.EndVelX {currentCheckpoint.EndVelX} - velocity_x {velocity_x}");
                        Console.WriteLine($"currentCheckpoint.EndVelY {currentCheckpoint.EndVelY} - velocity_y {velocity_y}");
                        Console.WriteLine($"currentCheckpoint.EndVelZ {currentCheckpoint.EndVelZ} - velocity_z {velocity_z}");
                        #endif

                        // Update the values
                        currentCheckpoint.EndVelX = velocity_x;
                        currentCheckpoint.EndVelY = velocity_y;
                        currentCheckpoint.EndVelZ = velocity_z;
                        currentCheckpoint.EndTouch = player.Timer.Ticks;
                        currentCheckpoint.Attempts += 1;
                        // Assign the updated currentCheckpoint back to the list as `currentCheckpoint` is supposedly a copy of the original object
                        player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint] = currentCheckpoint;

                        // Show Prespeed for stages - will be enabled/disabled by the user?
                        player.Controller.PrintToCenter($"Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} - Prespeed: {velocity.ToString("0")} u/s");
                    }
                    else
                    {
                        // Handle the case where the index is out of bounds
                    }
                }
            
                // Bonus start zones -- hook into (b)onus#_start
                else if (Regex.Match(trigger.Entity.Name, "^b([1-9][0-9]?|onus[1-9][0-9]?)_start$").Success)
                {
                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Bonus {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    #endif

                    // BONUS START ZONE
                    if (!player.Timer.IsStageMode && player.Timer.IsBonusMode)
                    {
                        player.Timer.Start();
                        // To-do: bonus replay
                    }

                    // Prespeed display
                    player.Controller.PrintToCenter($"Prespeed: {velocity.ToString("0")} u/s");
                    player.Stats.ThisRun.StartVelX = velocity_x; // Start pre speed for the run
                    player.Stats.ThisRun.StartVelY = velocity_y; // Start pre speed for the run
                    player.Stats.ThisRun.StartVelZ = velocity_z; // Start pre speed for the run
                }
            }

            return HookResult.Continue;
        }
    }
}
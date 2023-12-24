using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API;

namespace SurfTimer;

public partial class SurfTimer
{
    // Trigger end touch handler - CBaseTrigger_EndTouchFunc
    internal HookResult OnTriggerEndTouch(DynamicHook handler)
    {
        CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
        CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        if (!client.IsValid || client.UserId == -1 || !client.PawnIsAlive) // `client.IsBot` throws error in server console when going to spectator?
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
                float velocity = (float)Math.Sqrt(player.Controller.PlayerPawn.Value!.AbsVelocity.X * player.Controller.PlayerPawn.Value!.AbsVelocity.X 
                                            + player.Controller.PlayerPawn.Value!.AbsVelocity.Y * player.Controller.PlayerPawn.Value!.AbsVelocity.Y 
                                            + player.Controller.PlayerPawn.Value!.AbsVelocity.Z * player.Controller.PlayerPawn.Value!.AbsVelocity.Z);
                float velocity_x = player.Controller.PlayerPawn.Value!.AbsVelocity.X;
                float velocity_y = player.Controller.PlayerPawn.Value!.AbsVelocity.Y;
                float velocity_z = player.Controller.PlayerPawn.Value!.AbsVelocity.Z;
                
                // Map start zones -- hook into map_start, (s)tage1_start
                if (trigger.Entity.Name.Contains("map_start") || 
                    trigger.Entity.Name.Contains("s1_start") || 
                    trigger.Entity.Name.Contains("stage1_start")) 
                {
                    // MAP START ZONE
                    player.Timer.Start();

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
                        #endif

                        // Update the values
                        currentCheckpoint.EndVelX = velocity_x;
                        currentCheckpoint.EndVelY = velocity_y;
                        currentCheckpoint.EndVelZ = velocity_z;
                        currentCheckpoint.EndTouch = player.Timer.Ticks; // To-do: what type of value we store in DB ?
                        currentCheckpoint.Attempts += 1;
                        
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
                        currentCheckpoint.EndTouch = player.Timer.Ticks; // To-do: what type of value we store in DB ?
                        currentCheckpoint.Attempts += 1;
                        
                        // Show Prespeed for stages - will be enabled/disabled by the user?
                        player.Controller.PrintToCenter($"Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} - Prespeed: {velocity.ToString("0")} u/s");
                    }
                    else
                    {
                        // Handle the case where the index is out of bounds
                    }
                }
            }

            return HookResult.Continue;
        }
    }
}
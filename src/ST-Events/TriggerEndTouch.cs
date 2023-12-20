using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace SurfTimer;

public partial class SurfTimer
{
    // Trigger end touch handler - CBaseTrigger_EndTouchFunc
    internal HookResult OnTriggerEndTouch(DynamicHook handler)
    {
        CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
        CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        
        if (client.IsBot || !client.IsValid || client.UserId == -1 || !client.PawnIsAlive)
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
                // Map start zones -- hook into map_start, (s)tage1_start
                if (trigger.Entity.Name.Contains("map_start") || 
                    trigger.Entity.Name.Contains("s1_start") || 
                    trigger.Entity.Name.Contains("stage1_start")) 
                {
                    // MAP START ZONE
                    player.Timer.Start();

                    // Prespeed display
                    float velocity = (float)Math.Sqrt(player.Controller.PlayerPawn.Value!.AbsVelocity.X * player.Controller.PlayerPawn.Value!.AbsVelocity.X 
                                                + player.Controller.PlayerPawn.Value!.AbsVelocity.Y * player.Controller.PlayerPawn.Value!.AbsVelocity.Y 
                                                + player.Controller.PlayerPawn.Value!.AbsVelocity.Z * player.Controller.PlayerPawn.Value!.AbsVelocity.Z);
                    player.Controller.PrintToCenter($"Prespeed: {velocity.ToString("0")} u/s");

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
                    #endif
                }

                // Stage start zones -- hook into (s)tage#_start
                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
                {
                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    #endif

                    // Show Prespeed for stages - will be enabled/disabled by the user?
                    float velocity = (float)Math.Sqrt(player.Controller.PlayerPawn.Value!.AbsVelocity.X * player.Controller.PlayerPawn.Value!.AbsVelocity.X 
                                                + player.Controller.PlayerPawn.Value!.AbsVelocity.Y * player.Controller.PlayerPawn.Value!.AbsVelocity.Y 
                                                + player.Controller.PlayerPawn.Value!.AbsVelocity.Z * player.Controller.PlayerPawn.Value!.AbsVelocity.Z);
                    player.Controller.PrintToCenter($"Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} - Prespeed: {velocity.ToString("0")} u/s");
                }
            }

            return HookResult.Continue;
        }
    }
}
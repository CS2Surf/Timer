using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ClassName}] OnTriggerEndTouch -> Could not assign `client` (name: {Name}). Exception: {Exception}",
                nameof(SurfTimer), name, ex.Message
            );
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
                ZoneType currentZone = GetZoneType(trigger.Entity.Name);

                switch (currentZone)
                {
                    // Map end zones -- hook into map_end
                    case ZoneType.MapEnd:
                        EndTouchHandleMapEndZone(player);
                        break;
                    // Map start zones -- hook into map_start, (s)tage1_start
                    case ZoneType.MapStart:
                        EndTouchHandleMapStartZone(player);
                        break;
                    // Stage start zones -- hook into (s)tage#_start
                    case ZoneType.StageStart:
                        EndTouchHandleStageStartZone(player, trigger);
                        break;
                    // Map checkpoint zones -- hook into map_(c)heck(p)oint#
                    case ZoneType.Checkpoint:
                        EndTouchHandleCheckpointZone(player, trigger);
                        break;
                    // Bonus start zones -- hook into (b)onus#_start
                    case ZoneType.BonusStart:
                        EndTouchHandleBonusStartZone(player, trigger);
                        break;
                    // Bonus end zones -- hook into (b)onus#_end
                    case ZoneType.BonusEnd:
                        EndTouchHandleBonusEndZone(player);
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
    }
}
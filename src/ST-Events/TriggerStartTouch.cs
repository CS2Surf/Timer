using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace SurfTimer;

public partial class SurfTimer
{
    /// <summary>
    /// Handler for trigger start touch hook - CBaseTrigger_StartTouchFunc
    /// </summary>
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
            _logger.LogCritical("[{ClassName}] OnTriggerStartTouch -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {PlayerName} ({UserId})",
                nameof(SurfTimer), client.PlayerName, client.UserId
            );

            Exception exception = new($"[{nameof(SurfTimer)}] OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
            throw exception;
        }
        // Implement Trigger Start Touch Here
        Player player = playerList[client.UserId ?? 0];

#if DEBUG
        player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
#endif

        if (DB == null)
        {
            _logger.LogCritical("[{ClassName}] OnTriggerStartTouch -> DB object is null, this shouldn't happen.",
                nameof(SurfTimer)
            );

            Exception exception = new Exception($"[{nameof(SurfTimer)}] OnTriggerStartTouch -> DB object is null, this shouldn't happen.");
            throw exception;
        }

        if (trigger.Entity!.Name != null)
        {
            ZoneType currentZone = GetZoneType(trigger.Entity.Name);

            switch (currentZone)
            {
                // Map end zones -- hook into map_end
                case ZoneType.MapEnd:
                    StartTouchHandleMapEndZone(player);
                    break;
                // Map start zones -- hook into map_start, (s)tage1_start
                case ZoneType.MapStart:
                    StartTouchHandleMapStartZone(player, trigger);
                    break;
                // Stage start zones -- hook into (s)tage#_start
                case ZoneType.StageStart:
                    StartTouchHandleStageStartZone(player, trigger);
                    break;
                // Map checkpoint zones -- hook into map_(c)heck(p)oint#
                case ZoneType.Checkpoint:
                    StartTouchHandleCheckpointZone(player, trigger);
                    break;
                // Bonus start zones -- hook into (b)onus#_start
                case ZoneType.BonusStart:
                    StartTouchHandleBonusStartZone(player, trigger);
                    break;
                // Bonus end zones -- hook into (b)onus#_end
                case ZoneType.BonusEnd:
                    StartTouchHandleBonusEndZone(player, trigger);
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


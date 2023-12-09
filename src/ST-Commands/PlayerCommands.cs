using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;

namespace SurfTimer;

public partial class SurfTimer
{
    // All player-related commands here
    [ConsoleCommand("css_r", "Reset back to the start of the map.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void ResetPlayer(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        // To-do: players[userid].Timer.Reset() -> teleport player
        playerList[player.UserId ?? 0].Timer.Reset();
        // player.Teleport(Map.StartZoneOrigin, 0, 0);
        return;
    }

    // 
    [ConsoleCommand("css_triggers", "List triggers eligible for hooking.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void Triggers(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        IEnumerable<CBaseEntity> triggers = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("trigger_multiple");
        player.PrintToChat($"Count of triggers: {triggers.Count()}");
        foreach (CBaseEntity trigger in triggers)
        {
            if (trigger.Entity!.Name != null)
            {
                player.PrintToChat($"Trigger -> Origin: {trigger.AbsOrigin}, Name: {trigger.Entity!.Name}");
            }
        }
        return;
    }
}
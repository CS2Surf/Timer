using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;

namespace SurfTimer;

public partial class SurfTimer
{
    // All map-related commands here
    [ConsoleCommand("css_tier", "Display the current map tier.")]
    [ConsoleCommand("css_mapinfo", "Display the current map tier.")]
    [ConsoleCommand("css_mi", "Display the current map tier.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void MapTier(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        player.PrintToChat($"{pluginCfg.Config.Prefix} {CurrentMap.Name} - {ChatColors.Green}Tier {CurrentMap.Tier}{ChatColors.Default} - {ChatColors.Yellow}{CurrentMap.Stages} Stages{ChatColors.Default}");
        return;
    }

    [ConsoleCommand("css_triggers", "List all valid zone triggers in the map.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void Triggers(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        IEnumerable<CBaseTrigger> triggers = Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple");
        player.PrintToChat($"Count of triggers: {triggers.Count()}");
        foreach (CBaseTrigger trigger in triggers)
        {
            if (trigger.Entity!.Name != null)
            {
                player.PrintToChat($"Trigger -> Origin: {trigger.AbsOrigin}, Radius: {trigger.Collision.BoundingRadius}, Name: {trigger.Entity!.Name}");
            }
        }

        player.PrintToChat($"Hooked Trigger -> Start -> {CurrentMap.StartZone} -> Angles {CurrentMap.StartZoneAngles}");
        player.PrintToChat($"Hooked Trigger -> End -> {CurrentMap.EndZone}");
        int i = 1;
        foreach (Vector stage in CurrentMap.StageStartZone)
        {
            if (stage.X == 0 && stage.Y == 0 && stage.Z == 0)
                continue;
            else
            {
                player.PrintToChat($"Hooked Trigger -> Stage {i} -> {stage} -> Angles {CurrentMap.StageStartZoneAngles[i]}");
                i++;
            }
        }

        i = 1;
        foreach (Vector bonus in CurrentMap.BonusStartZone)
        {
            if (bonus.X == 0 && bonus.Y == 0 && bonus.Z == 0)
                continue;
            else
            {
                player.PrintToChat($"Hooked Trigger -> Bonus {i} -> {bonus} -> Angles {CurrentMap.BonusStartZoneAngles[i]}");
                i++;
            }
        }

        return;
    }
}
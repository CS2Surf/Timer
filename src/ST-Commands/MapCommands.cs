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

        string msg = $"{Config.PluginPrefix} {CurrentMap.Name} - Tier {ChatColors.Green}{CurrentMap.Tier}{ChatColors.Default} - Author {ChatColors.Yellow}{CurrentMap.Author}{ChatColors.Default} - Added {ChatColors.Yellow}{DateTimeOffset.FromUnixTimeSeconds(CurrentMap.DateAdded).DateTime.ToString("dd.MM.yyyy HH:mm")}{ChatColors.Default}";

        if (CurrentMap.Stages > 1)
        {
            msg = string.Concat(msg, " - ", $"Stages {ChatColors.Yellow}{CurrentMap.Stages}{ChatColors.Default}");
        }
        else
        {
            msg = string.Concat(msg, " - ", $"Linear {ChatColors.Yellow}{CurrentMap.TotalCheckpoints} Checkpoints{ChatColors.Default}");
        }

        if (CurrentMap.Bonuses > 0)
        {
            msg = string.Concat(msg, " - ", $"Bonuses {ChatColors.Yellow}{CurrentMap.Bonuses}");
        }

        player.PrintToChat(msg);
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
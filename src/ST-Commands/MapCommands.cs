using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

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

        player.PrintToChat($"{PluginPrefix} {CurrentMap.Name} - {ChatColors.Green}Tier {CurrentMap.Tier}{ChatColors.Default} - {ChatColors.Yellow}{CurrentMap.Stages} Stages{ChatColors.Default}");
        return;
    }
}
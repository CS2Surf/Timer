using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace SurfTimer;

public partial class SurfTimer
{
    // All player-related commands here
    [ConsoleCommand("st_r", "Reset back to the start of the map.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void ResetPlayer(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        // To-do: players[userid].Timer.Reset() -> teleport player
        playerList[player.UserId ?? 0].Timer.Reset();
        return;
    }
}
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

public partial class SurfTimer
{
    public void OnTick()
    {
        foreach (var player in playerList.Values)
        {
            player.Timer.Tick();
            player.ReplayRecorder.Tick(player);
            player.HUD.Display();
        }

        // Replay BOT Ticks
        CurrentMap?.ReplayBot.Tick(); // When CurrentMap null the ? operator will terminate safely the operation
    }
}
namespace SurfTimer;

public partial class SurfTimer
{
    public void OnTick()
    {
        foreach (var player in playerList.Values)
        {
            player.Timer.Tick();
            player.HUD.Display();

            #if DEBUG
            if (player.Controller.IsValid && player.Controller.PawnIsAlive) player.Controller.PrintToCenter($"DEBUG >> PrintToCenter -> Player.Timer.Ticks: {player.Timer.Ticks}");
            #endif
        }
    }
}
using CounterStrikeSharp.API.Modules.Cvars;

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

        if (CurrentMap == null)
            return;

        // Need to disable maps from executing their cfgs. Currently idk how (But seriusly it a security issue)
        ConVar? bot_quota = ConVar.Find("bot_quota");
        if (bot_quota != null)
        {
            int cbq = bot_quota.GetPrimitiveValue<int>();
            if(cbq != CurrentMap.ReplayBots.Count)
            {
                bot_quota.SetValue(CurrentMap.ReplayBots.Count);
            }
        }

        for(int i = 0; i < CurrentMap!.ReplayBots.Count; i++)
        {
            CurrentMap.ReplayBots[i].Tick();
            if (CurrentMap.ReplayBots[i].RepeatCount == 0)
                CurrentMap.KickReplayBot(i);
        }
    }
}
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
            if (CurrentMap.ReplayBots[i].MapID != CurrentMap.ID)
                CurrentMap.ReplayBots[i].MapID = CurrentMap.ID;

            CurrentMap.ReplayBots[i].Tick();
            if (CurrentMap.ReplayBots[i].RepeatCount == 0)
            {
                int m = 1 + (CurrentMap.Stages > 0 ? 1 : 0) + (CurrentMap.Bonuses > 0 ? 1 :0);
                
                if(i == CurrentMap.ReplayBots.Count - 1)
                    continue;

                if (i < CurrentMap.ReplayBots.Count - m)
                {
                    CurrentMap.KickReplayBot(i);
                    continue;
                }

                if (CurrentMap.ReplayBots[i].Type == 1)
                    CurrentMap.ReplayBots[i].Stage = (CurrentMap.ReplayBots[i].Stage + 1) % CurrentMap.Bonuses;
                else if (CurrentMap.ReplayBots[i].Type == 2)
                    CurrentMap.ReplayBots[i].Stage = (CurrentMap.ReplayBots[i].Stage + 1) % CurrentMap.Stages;

                CurrentMap.ReplayBots[i].LoadReplayData(DB!);
                CurrentMap.ReplayBots[i].ResetReplay();
                CurrentMap.ReplayBots[i].RepeatCount = 3;
            }
        }
    }
}
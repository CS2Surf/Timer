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
        // Console.WriteLine($"======== public void OnTick -> bot_quota not null? {bot_quota != null}");
        if (bot_quota != null)
        {
            int cbq = bot_quota.GetPrimitiveValue<int>();

            int replaybot_count = 1 +
                                (CurrentMap.ReplayManager.StageWR != null ? 1 : 0) +
                                (CurrentMap.ReplayManager.BonusWR != null ? 1 : 0) +
                                CurrentMap.ReplayManager.CustomReplays.Count;

            if (cbq != replaybot_count)
            {
                bot_quota.SetValue(replaybot_count);
            }

            // _logger.LogInformation("public void OnTick -> Got bot_quota {cbq} | Setting to bot_quota {replaybot_count}", cbq, replaybot_count);
        }

        CurrentMap.ReplayManager.MapWR.Tick();
        CurrentMap.ReplayManager.StageWR?.Tick();
        CurrentMap.ReplayManager.BonusWR?.Tick();

        if (CurrentMap.ReplayManager.MapWR.MapTimeID != -1)
        {
            CurrentMap.ReplayManager.MapWR.FormatBotName();
        }

        // Here we will load the NEXT stage replay from AllStageWR
        if (CurrentMap.ReplayManager.StageWR?.RepeatCount == 0)
        {
            int next_stage;
            if (CurrentMap.ReplayManager.AllStageWR[(CurrentMap.ReplayManager.StageWR.Stage % CurrentMap.Stages) + 1][0].MapTimeID == -1)
                next_stage = 1;
            else
                next_stage = (CurrentMap.ReplayManager.StageWR.Stage % CurrentMap.Stages) + 1;

            CurrentMap.ReplayManager.AllStageWR[next_stage][0].Controller = CurrentMap.ReplayManager.StageWR.Controller;

            // _logger.LogInformation("public void OnTick() -> Finished replay cycle for stage {Stage}, changing to stage {next_stage}", CurrentMap.ReplayManager.StageWR.Stage, next_stage);
            CurrentMap.ReplayManager.StageWR = CurrentMap.ReplayManager.AllStageWR[next_stage][0];
            CurrentMap.ReplayManager.StageWR.LoadReplayData(repeat_count: 3);
            CurrentMap.ReplayManager.StageWR.FormatBotName();
            CurrentMap.ReplayManager.StageWR.Start();
        }

        if (CurrentMap.ReplayManager.BonusWR?.RepeatCount == 0)
        {
            int next_bonus;
            if (CurrentMap.ReplayManager.AllBonusWR[(CurrentMap.ReplayManager.BonusWR.Stage % CurrentMap.Bonuses) + 1][0].MapTimeID == -1)
                next_bonus = 1;
            else
                next_bonus = (CurrentMap.ReplayManager.BonusWR.Stage % CurrentMap.Bonuses) + 1;

            CurrentMap.ReplayManager.AllBonusWR[next_bonus][0].Controller = CurrentMap.ReplayManager.BonusWR.Controller;

            // _logger.LogInformation("public void OnTick() -> Finished replay cycle for bonus {Bonus}, changing to bonus {next_bonus}", CurrentMap.ReplayManager.BonusWR.Stage, next_bonus);
            CurrentMap.ReplayManager.BonusWR = CurrentMap.ReplayManager.AllBonusWR[next_bonus][0];
            CurrentMap.ReplayManager.BonusWR.LoadReplayData(repeat_count: 3);
            CurrentMap.ReplayManager.BonusWR.FormatBotName();
            CurrentMap.ReplayManager.BonusWR.Start();
        }

        for (int i = 0; i < CurrentMap.ReplayManager.CustomReplays.Count; i++)
        {
            if (CurrentMap.ReplayManager.CustomReplays[i].MapID != CurrentMap.ID)
                CurrentMap.ReplayManager.CustomReplays[i].MapID = CurrentMap.ID;

            CurrentMap.ReplayManager.CustomReplays[i].Tick();
            if (CurrentMap.ReplayManager.CustomReplays[i].RepeatCount == 0)
            {
                CurrentMap.KickReplayBot(i);
            }
        }
    }
}
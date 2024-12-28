using System.Net.Http.Json;

namespace SurfTimer;

internal class ApiCall
{
    public static async Task Api_Save_Stage_MapTime(Player player)
    {
        // This is a trick to record the time before the player exits the start zone
        int last_exit_tick = player.ReplayRecorder.LastExitTick();
        int last_enter_tick = player.ReplayRecorder.LastEnterTick();

        // player.Controller.PrintToChat($"CS2 Surf DEBUG >> OnTriggerStartTouch -> Last Exit Tick: {last_exit_tick} | Current Frame: {player.ReplayRecorder.Frames.Count}");

        int stage_run_time = player.ReplayRecorder.Frames.Count - 1 - last_exit_tick; // Would like some check on this
        int time_since_last_enter = player.ReplayRecorder.Frames.Count - 1 - last_enter_tick;

        int tt = -1;
        if (last_exit_tick - last_enter_tick > 2 * 64)
            tt = last_exit_tick - 2 * 64;
        else
            tt = last_enter_tick;

        API_CurrentRun stage_time = new()
        {
            player_id = player.Profile.ID,
            map_id = player.CurrMap.ID,
            style = player.Timer.Style,
            type = 2,
            stage = player.Timer.Stage - 1,
            run_time = stage_run_time,
            run_date = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            replay_frames = player.ReplayRecorder.SerializeReplayPortion(tt, time_since_last_enter)

        };

        await ApiMethod.POST(Config.API.Endpoints.ENDPOINT_CR_SAVE_STAGE_TIME, stage_time);
        // player.Stats.LoadStageTime(player);
        // await CurrentMap.ApiGetMapRecordAndTotals(); // Reload the Map record and totals for the HUD
    }
}
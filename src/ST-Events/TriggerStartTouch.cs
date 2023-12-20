using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace SurfTimer;

public partial class SurfTimer
{
    // Trigger start touch handler - CBaseTrigger_StartTouchFunc
    internal HookResult OnTriggerStartTouch(DynamicHook handler)
    {
        CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
        CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        
        if (client.IsBot || !client.IsValid)
        {
            return HookResult.Continue;
        }

        else 
        {
            // Implement Trigger Start Touch Here
            Player player = playerList[client.UserId ?? 0];
            #if DEBUG
            player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
            #endif

            // Get velocities for DB queries
            float velocity_x = player.Controller.PlayerPawn.Value!.AbsVelocity.X;
            float velocity_y = player.Controller.PlayerPawn.Value!.AbsVelocity.Y;
            float velocity_z = player.Controller.PlayerPawn.Value!.AbsVelocity.Z;

            if (trigger.Entity!.Name != null)
            {
                // Map end zones -- hook into map_end
                if (trigger.Entity.Name == "map_end")
                {
                    // MAP END ZONE
                    if (player.Timer.IsRunning)
                    {
                        player.Timer.Stop();
                        // To-do: make Style (currently 0) be dynamic
                        if (player.Stats.PB[0].RunTime == 0 || player.Timer.Ticks < player.Stats.PB[0].RunTime)
                        {
                            player.Stats.PB[0].RunTime = player.Timer.Ticks;
                            player.Controller.PrintToChat($"{PluginPrefix} You beat your PB in {player.HUD.FormatTime(player.Stats.PB[0].RunTime)} ({player.Timer.Ticks})!");
                        }
                        else
                        {
                            player.Controller.PrintToChat($"{PluginPrefix} You finished the map in {player.HUD.FormatTime(player.Stats.PB[0].RunTime)} ({player.Timer.Ticks})!");
                        }

                        // Add entry in DB for the run
                        // To-do: add `type`
                        // To-do: get the `start_vel` values for the run from CP implementation in other repository implementation of checkpoints and their speeds
                        // Console.WriteLine($"============== INSERT INTO `MapTimes` (`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`) VALUES ({player.Profile.ID}, {CurrentMap.ID}, 0, 0, 0, {player.Stats.PB[0].RunTime}, 123.000, 456.000, 789.000, {velocity_x}, {velocity_y}, {velocity_z}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}) ON DUPLICATE KEY UPDATE player_id=VALUES(player_id), map_id=VALUES(map_id), style=VALUES(style), type=VALUES(type), stage=VALUES(stage), run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date);");
                        Task<int> updatePlayerRunTask = DB.Write($"INSERT INTO `MapTimes` (`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`) VALUES ({player.Profile.ID}, {CurrentMap.ID}, 0, 0, 0, {player.Stats.PB[0].RunTime}, 123.000, 456.000, 789.000, {velocity_x}, {velocity_y}, {velocity_z}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}) ON DUPLICATE KEY UPDATE player_id=VALUES(player_id), map_id=VALUES(map_id), style=VALUES(style), type=VALUES(type), stage=VALUES(stage), run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date);");
                        if (updatePlayerRunTask.Result <= 0)
                            throw new Exception($"CS2 Surf ERROR >> OnTriggerStartTouch -> Failed to insert/update player run in database. Player: {player.Profile.Name} ({player.Profile.SteamID})");

                        // player.Timer.Reset();
                    }

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Red}Map Stop Zone");
                    #endif
                }

                // Map start zones -- hook into map_start, (s)tage1_start
                else if (trigger.Entity.Name.Contains("map_start") || 
                        trigger.Entity.Name.Contains("s1_start") || 
                        trigger.Entity.Name.Contains("stage1_start")) 
                {
                    player.Timer.Reset();

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
                    // player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> KeyValues: {trigger.Entity.KeyValues3}");
                    #endif
                }

                // Stage start zones -- hook into (s)tage#_start
                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
                {
                    int stage = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1;
                    player.Timer.Stage = stage;

                    // To-do: checkpoint functionality because stages = checkpoints

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    #endif
                }

                // Map checkpoint zones -- hook into map_(c)heck(p)oint#
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
                {
                    // To-do: checkpoint functionality

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.LightBlue}Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Zone");
                    #endif
                }
            }

            return HookResult.Continue;
        }
    }
}
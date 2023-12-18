using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Text.Json;

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

            // Get the velocity of the player - we will be using this values to compare and write to DB
            float velocity = (float)Math.Sqrt(player.Controller.PlayerPawn.Value!.AbsVelocity.X * player.Controller.PlayerPawn.Value!.AbsVelocity.X 
                                        + player.Controller.PlayerPawn.Value!.AbsVelocity.Y * player.Controller.PlayerPawn.Value!.AbsVelocity.Y 
                                        + player.Controller.PlayerPawn.Value!.AbsVelocity.Z * player.Controller.PlayerPawn.Value!.AbsVelocity.Z);
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
                        if (player.Stats.PB[0,0] == 0 || player.Timer.Ticks < player.Stats.PB[0,0])
                            player.Stats.PB[0,0] = player.Timer.Ticks;
                        player.Controller.PrintToChat($"{PluginPrefix} You finished the map in {player.HUD.FormatTime(player.Stats.PB[0,0])}!");

                        foreach (var item in player.Timer.CurrentRunCheckpoints)
                        {
                            int cp = item.GetProperty("cp").GetInt32();
                            string time = item.GetProperty("time").GetString();
                            int ticks = item.GetProperty("ticks").GetInt32();
                            double speed = item.GetProperty("speed").GetDouble();
                            double velX = item.GetProperty("velX").GetDouble();
                            double velY = item.GetProperty("velY").GetDouble();
                            double velZ = item.GetProperty("velZ").GetDouble(); 

                            Console.WriteLine($"CP: {cp} | Time: {time} | Ticks: {ticks} | Speed: {speed} | VelX: {velX} | VelY: {velY} | VelZ: {velZ}");

                            // Write new CPs to database
                            // Transactions?
                            // Task<int> newPbTask = DB.Write($"INSERT INTO `Checkpoints` (`maptime_id`, `cp`, `runtime`, `velX`, `velY`, `velZ`) VALUES ('0', {cp}, '{ticks}', {velX}, {velY}, {velZ});");
                            // int newPbTaskRows = newPbTask.Result;
                            // if (newPbTaskRows != 1)
                            //     throw new Exception($"CS2 Surf ERROR >> Inserting Checkpoints.");
                        }
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

                    // To-do: checkpoint functionality because stages = checkpoints when in a run on a Staged map
                    // To-do: This triggers more than once at random :monkaHmm: 
                    // This should patch up re-triggering *player.Timer.CurrentRunCheckpoints.Count < stage*
                    if (player.Timer.IsRunning && !player.Timer.StageMode && player.Timer.CurrentRunCheckpoints.Count < stage)
                    {
                        player.Controller.PrintToChat(
                            $"{PluginPrefix} CP [{ChatColors.Yellow}{stage}{ChatColors.Default}]: " + 
                            $"{ChatColors.Yellow}{player.HUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} " +
                            $"{ChatColors.Yellow}({velocity.ToString("0")}){ChatColors.Default} " + 
                            $"[PB: {ChatColors.Green}-00:00.000{ChatColors.Default} " +
                            $"{ChatColors.Red}(-1234){ChatColors.Default} | " +
                            $"WR: {ChatColors.Red}+00:00.000{ChatColors.Default} " +
                            $"{ChatColors.Green}(+1234){ChatColors.Default}]");
                        
                        // .... store in an array to INSERT/UPDATE in DB at the end of run?
                        string jsonString = $"{{ \"cp\": {stage}, \"time\": \"{player.HUD.FormatTime(player.Timer.Ticks)}\", \"ticks\": {player.Timer.Ticks}, \"speed\": {velocity}, \"velX\": {velocity_x}, \"velY\": {velocity_y}, \"velZ\": {velocity_z} }}";
                        JsonElement currRunCps = JsonDocument.Parse(jsonString).RootElement;
                        player.Timer.CurrentRunCheckpoints.Add(currRunCps);
                    }

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                    #endif
                }

                // Map checkpoint zones -- hook into map_(c)heck(p)oint#
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
                {
                    int checkpoint = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1;
                    player.Timer.Checkpoint = checkpoint;

                    // To-do: checkpoint functionality
                    if (player.Timer.IsRunning && !player.Timer.StageMode && player.Timer.CurrentRunCheckpoints.Count < checkpoint)
                    {
                        player.Controller.PrintToChat(
                            $"{PluginPrefix} CP [{ChatColors.Yellow}{checkpoint}{ChatColors.Default}]: " + 
                            $"{ChatColors.Yellow}{player.HUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} " +
                            $"{ChatColors.Yellow}({velocity.ToString("0")}){ChatColors.Default} " + 
                            $"[PB: {ChatColors.Green}-00:00.000{ChatColors.Default} " +
                            $"{ChatColors.Red}(-1234){ChatColors.Default} | " +
                            $"WR: {ChatColors.Red}+00:00.000{ChatColors.Default} " +
                            $"{ChatColors.Green}(+1234){ChatColors.Default}]");
                        
                        // .... store in an array to INSERT/UPDATE in DB at the end of run?
                        string jsonString = $"{{ \"cp\": {checkpoint}, \"time\": \"{player.HUD.FormatTime(player.Timer.Ticks)}\", \"ticks\": {player.Timer.Ticks}, \"speed\": {velocity}, \"velX\": {velocity_x}, \"velY\": {velocity_y}, \"velZ\": {velocity_z} }}";
                        JsonElement currRunCps = JsonDocument.Parse(jsonString).RootElement;
                        player.Timer.CurrentRunCheckpoints.Add(currRunCps);
                        // .... store in an array to INSERT/UPDATE in DB at the end of run?
                    }

                    #if DEBUG
                    player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.LightBlue}Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Zone");
                    #endif
                }
            }

            return HookResult.Continue;
        }
    }
}
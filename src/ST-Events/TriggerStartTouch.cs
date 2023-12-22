using System.Text.RegularExpressions;
using System.Text.Json;
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
        if (client.IsBot || !client.IsValid || !client.PawnIsAlive)
        {
            return HookResult.Continue;
        }
        else
        {
            // To-do: Sometimes this triggers before `OnPlayerConnect` and `playerList` does not contain the player how is this possible :thonk:
            if (!playerList.ContainsKey(client.UserId ?? 0))
            {
                /* 
                CS2 Surf ERROR >> OnTriggerStartTouch -> Player playerList does NOT contain client.UserId, this shouldnt happen. Player: tttt (0)
                    11:19:18 [EROR] (cssharp:Core) Error invoking callback
                    System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
                    ---> System.Collections.Generic.KeyNotFoundException: The given key '0' was not present in the dictionary.
                    at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
                    at SurfTimer.SurfTimer.OnTriggerStartTouch(DynamicHook handler)
                    at InvokeStub_Func`2.Invoke(Object, Object, IntPtr*)
                    at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)
                    --- End of inner exception stack trace ---
                    at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)
                    at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
                    at System.Delegate.DynamicInvokeImpl(Object[] args)
                    at CounterStrikeSharp.API.Core.FunctionReference.<>c__DisplayClass3_0.<.ctor>b__0(fxScriptContext* context) in /home/runner/work/CounterStrikeSharp/CounterStrikeSharp/managed/CounterStrikeSharp.API/Core/FunctionReference.cs:line 82
                */
                // For some reason, this happens as soon as player connects to the server (randomly)
                // Is an "entity" created for the player when they connect which triggers this???
                Console.WriteLine($"CS2 Surf ERROR >> OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldnt happen. Player: {client.PlayerName} ({client.UserId})");
                // return HookResult.Continue;
            }

            // Implement Trigger Start Touch Here
            Player player = playerList[client.UserId ?? 0];
            #if DEBUG
            player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
            #endif

            if (trigger.Entity!.Name != null)
            {
                // Get velocities for DB queries
                // Get the velocity of the player - we will be using this values to compare and write to DB
                float velocity = (float)Math.Sqrt(player.Controller.PlayerPawn.Value!.AbsVelocity.X * player.Controller.PlayerPawn.Value!.AbsVelocity.X
                                            + player.Controller.PlayerPawn.Value!.AbsVelocity.Y * player.Controller.PlayerPawn.Value!.AbsVelocity.Y
                                            + player.Controller.PlayerPawn.Value!.AbsVelocity.Z * player.Controller.PlayerPawn.Value!.AbsVelocity.Z);
                float velocity_x = player.Controller.PlayerPawn.Value!.AbsVelocity.X;
                float velocity_y = player.Controller.PlayerPawn.Value!.AbsVelocity.Y;
                float velocity_z = player.Controller.PlayerPawn.Value!.AbsVelocity.Z;

                // Map end zones -- hook into map_end
                if (trigger.Entity.Name == "map_end")
                {
                    // MAP END ZONE
                    if (player.Timer.IsRunning)
                    {
                        player.Timer.Stop();
                        // To-do: make Style (currently 0) be dynamic
                        if (player.Stats.PB[0].RunTime <= 0) // Player first ever PersonalBest for the map
                        {
                            player.Controller.PrintToChat($"{PluginPrefix} Congratulations on setting your PB in {ChatColors.Gold}{player.HUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} ({player.Timer.Ticks})!");
                        }
                        else if (player.Timer.Ticks < player.Stats.PB[0].RunTime) // Player beating their existing PersonalBest for the map
                        {
                            player.Controller.PrintToChat($"{PluginPrefix} You beat your PB in {ChatColors.Gold}{player.HUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} (Old: {ChatColors.BlueGrey}{player.HUD.FormatTime(player.Stats.PB[0].RunTime)}{ChatColors.Default})!");
                        }
                        else // Player did not beat their existing PersonalBest for the map
                        {
                            player.Controller.PrintToChat($"{PluginPrefix} You finished the map in {ChatColors.Yellow}{player.HUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default}!");
                            return HookResult.Continue;
                        }
                        player.Stats.PB[0].RunTime = player.Timer.Ticks;

                        if (DB == null)
                            throw new Exception("CS2 Surf ERROR >> OnTriggerStartTouch (Map end zone) -> DB object is null, this shouldnt happen.");

                        #if DEBUG
                        Console.WriteLine($"CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> " +
                                                                    $"============== INSERT INTO `MapTimes` " +
                                                                    $"(`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`) " +
                                                                    $"VALUES ({player.Profile.ID}, {CurrentMap.ID}, 0, 0, 0, {player.Stats.PB[0].RunTime}, " +
                                                                    $"123.000, 456.000, 789.000, {velocity_x}, {velocity_y}, {velocity_z}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}) " + // To-do: get the `start_vel` values for the run from CP implementation
                                                                    $"ON DUPLICATE KEY UPDATE run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), " +
                                                                    $"start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date);");
                        #endif

                        // Add entry in DB for the run
                        // To-do: add `type`
                        // To-do: get the `start_vel` values for the run from CP implementation in other repository implementation of checkpoints and their speeds
                        Task<int> updatePlayerRunTask = DB.Write($"INSERT INTO `MapTimes` " +
                                                                    $"(`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`) " +
                                                                    $"VALUES ({player.Profile.ID}, {CurrentMap.ID}, 0, 0, 0, {player.Stats.PB[0].RunTime}, " +
                                                                    $"123.000, 456.000, 789.000, {velocity_x}, {velocity_y}, {velocity_z}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}) " + // To-do: get the `start_vel` values for the run from CP implementation
                                                                    $"ON DUPLICATE KEY UPDATE run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), " +
                                                                    $"start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date);");
                        if (updatePlayerRunTask.Result <= 0)
                            throw new Exception($"CS2 Surf ERROR >> OnTriggerStartTouch (Map end zone) -> Failed to insert/update player run in database. Player: {player.Profile.Name} ({player.Profile.SteamID})");
                        else
                            player.Stats.LoadMapTimesData(player.Profile.ID, CurrentMap.ID, DB); // Load the MapTime PB data again (will refresh the MapTime ID for the Checkpoints query)
                        updatePlayerRunTask.Dispose();

                        // To-do: Transactions? Server freezes for a bit here sometimes
                        // Loop through the checkpoints and insert/update them in the database for the run
                        foreach (var item in player.Timer.CurrentRunCheckpoints)
                        {
                            int cp = item.CP;
                            int runTime = item.RunTime; // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
                            int ticks = item.Ticks; // To-do: this was supposed to be the ticks but that is used for run_time for HUD
                            double speed = item.Speed;
                            double startVelX = item.StartVelX;
                            double startVelY = item.StartVelY;
                            double startVelZ = item.StartVelZ;
                            double endVelX = item.EndVelX;
                            double endVelY = item.EndVelY;
                            double endVelZ = item.EndVelZ;
                            int attempts = item.Attempts;

                            #if DEBUG
                            Console.WriteLine($"CP: {cp} | Time: {runTime} | Ticks: {ticks} | Speed: {speed} | startVelX: {startVelX} | startVelY: {startVelY} | startVelZ: {startVelZ} | endVelX: {endVelX} | endVelY: {endVelY} | endVelZ: {endVelZ}");
                            Console.WriteLine($"CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> " +
                                                $"INSERT INTO `Checkpoints` " +
                                                $"(`maptime_id`, `cp`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, " +
                                                $"`end_vel_x`, `end_vel_y`, `end_vel_z`, `attempts`, `end_touch`) " +
                                                $"VALUES ({player.Stats.PB[0].ID}, {cp}, {runTime}, {startVelX}, {startVelY}, {startVelZ}, {endVelX}, {endVelY}, {endVelZ}, {attempts}, {ticks}) ON DUPLICATE KEY UPDATE " +
                                                $"run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), " +
                                                $"end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), attempts=VALUES(attempts), end_touch=VALUES(end_touch);");
                            #endif

                            // Insert/Update CPs to database
                            // To-do: Transactions?
                            // Check if the player has PB object initialized and if the player's character is currently active in the game
                            if (player.Stats.PB[0] != null && player.Controller.PlayerPawn.Value != null)
                            {
                                Task<int> newPbTask = DB.Write($"INSERT INTO `Checkpoints` " +
                                                $"(`maptime_id`, `cp`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, " +
                                                $"`end_vel_x`, `end_vel_y`, `end_vel_z`, `attempts`, `end_touch`) " +
                                                $"VALUES ({player.Stats.PB[0].ID}, {cp}, {runTime}, {startVelX}, {startVelY}, {startVelZ}, {endVelX}, {endVelY}, {endVelZ}, {attempts}, {ticks}) ON DUPLICATE KEY UPDATE " +
                                                $"run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), start_vel_z=VALUES(start_vel_z), " +
                                                $"end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), attempts=VALUES(attempts), end_touch=VALUES(end_touch);");
                                if (newPbTask.Result <= 0)
                                    throw new Exception($"CS2 Surf ERROR >> OnTriggerStartTouch (Checkpoint zones) -> Inserting Checkpoints. CP: {cp} | Name: {player.Profile.Name}");
                                newPbTask.Dispose();
                            }
                        }
                        player.Stats.PB[0].LoadCheckpointsForRun(player.Stats.PB[0].ID, DB); // Load the Checkpoints PB data again
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

                    // To-do:* checkpoint functionality because stages = checkpoints when in a run on a Staged map
                    // To-do:* This triggers more than once at random :monkaHmm: *already posted in CS# about OnPlayerConnect being triggered after OnStartTouch*
                    // This should patch up re-triggering *player.Timer.CurrentRunCheckpoints.Count < stage*
                    if (player.Timer.IsRunning && !player.Timer.IsStageMode && player.Timer.CurrentRunCheckpoints.Count <= stage)
                    {
                        player.Timer.Checkpoint = stage; // Stage = Checkpoint when in a run on a Staged map

                        #if DEBUG
                        Console.WriteLine($"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `stage`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1}");
                        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.PB[0].Checkpoint.Count = {player.Stats.PB[0].Checkpoint.Count}");
                        #endif

                        // Print checkpoint message
                        player.HUD.DisplayCheckpointMessages(PluginPrefix);

                        // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                        PersonalBest.CheckpointObject cp2 = new PersonalBest.CheckpointObject(stage,
                                                        player.Timer.Ticks, // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
                                                        player.Timer.Ticks, // To-do: this was supposed to be the ticks but that is used for run_time for HUD
                                                        velocity,
                                                        velocity_x,
                                                        velocity_y,
                                                        velocity_z,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        0);
                        player.Timer.CurrentRunCheckpoints.Add(cp2);
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

                    // This should patch up re-triggering *player.Timer.CurrentRunCheckpoints.Count < checkpoint*
                    if (player.Timer.IsRunning && !player.Timer.IsStageMode && player.Timer.CurrentRunCheckpoints.Count < checkpoint)
                    {
                        #if DEBUG
                        Console.WriteLine($"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `checkpoint`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1}");
                        Console.WriteLine($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Checkpoint zones) -> player.Stats.PB[0].Checkpoint.Count = {player.Stats.PB[0].Checkpoint.Count}");
                        #endif
                        
                        // Print checkpoint message
                        player.HUD.DisplayCheckpointMessages(PluginPrefix);

                        // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                        PersonalBest.CheckpointObject cp2 = new PersonalBest.CheckpointObject(checkpoint,
                                                        player.Timer.Ticks, // To-do: what type of value we use here? DB uses DECIMAL but `.Tick` is int???
                                                        player.Timer.Ticks, // To-do: this was supposed to be the ticks but that is used for run_time for HUD
                                                        velocity,
                                                        velocity_x,
                                                        velocity_y,
                                                        velocity_z,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        -1.0f,
                                                        0);
                        player.Timer.CurrentRunCheckpoints.Add(cp2);
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
/*
                  ___  _____  _________  ___ 
                 ___  /  _/ |/ / __/ _ \/ _ |
                ___  _/ //    / _// , _/ __ |
               ___  /___/_/|_/_/ /_/|_/_/ |_|

    Official Timer plugin for the CS2 Surf Initiative.
    Copyright (C) 2024  Liam C (Infra), Contributors.md

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

#define DEBUG

using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

// Gameplan: https://github.com/CS2Surf/Timer/tree/dev 
[MinimumApiVersion(100)]
public partial class SurfTimer : BasePlugin
{
    // Metadata
    public override string ModuleName => "CS2 SurfTimer";
    public override string ModuleVersion => "DEV-1";
    public override string ModuleDescription => "Official SurfTimer by the CS2 Surf Initiative.";
    public override string ModuleAuthor => "The CS2 Surf Initiative - github.com/cs2surf";
    public string PluginPrefix => $"[{ChatColors.DarkBlue}CS2 Surf{ChatColors.Default}]";

    // Globals
    private Dictionary<int, Player> playerList = new Dictionary<int, Player>(); // This can probably be done way better, revisit

    /* ========== ROUND HOOKS ========== */
    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Load cvars/other configs here
        return HookResult.Continue;
    }

    /* ========== PLAYER HOOKS ========== */
    // Player Connect
    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info) // To-do: move to post-authorisation
    {
        var player = @event.Userid;
        #if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnect -> {player.PlayerName} / {player.UserId}");
        #endif

        if (player.IsBot || !player.IsValid)
        {
            return HookResult.Continue;
        }
        else
        {
            playerList[player.UserId ?? 0] = new Player(player, new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices!.Handle));
            Server.PrintToChatAll($"{PluginPrefix} {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has connected.");

            // Player connection to-do

            return HookResult.Continue;
        }
    }

    // Player Disconnect 
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player.IsBot || !player.IsValid)
        {
            return HookResult.Continue;
        }
        else
        {
            playerList.Remove(player.UserId ?? 0);

            // Player disconnection to-do

            return HookResult.Continue;
        }
    }

    /* ========== PLUGIN LOAD ========== */
    public override void Load(bool hotReload = false)
    {
        Console.WriteLine(String.Format("  ____________    ____         ___\n"
                                    + " / ___/ __/_  |  / __/_ ______/ _/\n"
                                    + "/ /___\\ \\/ __/  _\\ \\/ // / __/ _/ \n"
                                    + "\\___/___/____/ /___/\\_,_/_/ /_/\n"  
                                    + $"[CS2 Surf] SurfTimer plugin loaded. Version: {ModuleVersion}"
        ));

        // Load database config & spawn database object
        try
        {
            JsonElement dbConfig = JsonDocument.Parse(File.ReadAllText(Server.GameDirectory + "/csgo/cfg/SurfTimer/database.json")).RootElement;
            TimerDatabase DB = new TimerDatabase(dbConfig.GetProperty("host").GetString(),
                                                dbConfig.GetProperty("database").GetString(),
                                                dbConfig.GetProperty("user").GetString(),
                                                dbConfig.GetProperty("password").GetString(),
                                                dbConfig.GetProperty("port").GetInt32(),
                                                dbConfig.GetProperty("timeout").GetInt32());
        }

        catch (Exception e)
        {
            Console.WriteLine($"[CS2 Surf] Error loading database config: {e.Message}");
            // To-do: Abort plugin loading?
        }

        // Tick listener
        RegisterListener<Listeners.OnTick>(() =>
        {
            foreach (var player in playerList.Values)
            {
                player.Timer.Tick();
                player.HUD.Display();

                #if DEBUG
                if (player.Controller.IsValid && player.Controller.PawnIsAlive) player.Controller.PrintToCenter($"DEBUG >> PrintToCenter -> Player.Timer.Ticks: {player.Timer.Ticks}");
                #endif
            }
        });

        // StartTouch Hook
        VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(handler =>
        {
            // Implement Trigger Start Touch Here
            // TO-DO: IMPLEMENT ZONES IN ST-Map
            CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
            CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
            Player player = playerList[new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value.Handle).UserId ?? 0];
            #if DEBUG
            player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity.Name}");
            #endif

            if (trigger.Entity.Name == "map_end") // TO-DO: IMPLEMENT ZONES IN ST-Map
            {
                // MAP END ZONE
                if (player.Timer.IsRunning)
                {
                    player.Timer.Stop();
                    player.Stats.PB[0,0] = player.Timer.Ticks;
                    player.Controller.PrintToChat($"{PluginPrefix} You finished the map in {player.HUD.FormatTime(player.Stats.PB[0,0])}!");
                    // player.Timer.Reset();
                }

                #if DEBUG
                player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> trigger_stop actioned");
                #endif
            }

            else if (trigger.Entity.Name == "map_start") // TO-DO: IMPLEMENT ZONES IN ST-Map
            {
                // MAP START ZONE
                player.Timer.Reset();

                #if DEBUG
                player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> trigger_start actioned");
                #endif
            }

            return HookResult.Continue;
        }, HookMode.Post);

        // EndTouch Hook
        VirtualFunctions.CBaseTrigger_EndTouchFunc.Hook(handler =>
        {
            // Implement Trigger End Touch Here
            // TO-DO: IMPLEMENT ZONES IN ST-Map
            CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
            CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
            Player player = playerList[new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value.Handle).UserId ?? 0];
            #if DEBUG
            player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_EndTouchFunc -> {trigger.DesignerName} -> {trigger.Entity.Name}");
            #endif

            if (trigger.Entity.Name == "map_start") // TO-DO: IMPLEMENT ZONES IN ST-Map
            {
                // MAP END ZONE
                player.Timer.Start();

                #if DEBUG
                player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> trigger_stop actioned");
                #endif
            }

            return HookResult.Continue;
        }, HookMode.Post);
    }
}

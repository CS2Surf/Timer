﻿/*
                      ___  _____  _________  ___ 
                     ___  /  _/ |/ / __/ _ \/ _ |
                    ___  _/ //    / _// , _/ __ |
                   ___  /___/_/|_/_/ /_/|_/_/ |_|

    Official Timer plugin for the CS2 Surf Initiative.
    Copyright (C) 2024  Liam C. (Infra)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

    Source: https://github.com/CS2Surf/Timer
*/

#define DEBUG

using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

// Gameplan: https://github.com/CS2Surf/Timer/tree/dev/README.md
[MinimumApiVersion(120)]
public partial class SurfTimer : BasePlugin
{
    // Metadata
    public override string ModuleName => "CS2 SurfTimer";
    public override string ModuleVersion => "DEV-1";
    public override string ModuleDescription => "Official Surf Timer by the CS2 Surf Initiative.";
    public override string ModuleAuthor => "The CS2 Surf Initiative - github.com/cs2surf";
    public string PluginPrefix => $"[{ChatColors.DarkBlue}CS2 Surf{ChatColors.Default}]"; // To-do: make configurable

    // Globals
    private Dictionary<int, Player> playerList = new Dictionary<int, Player>(); // This can probably be done way better, revisit
    internal TimerDatabase? DB = new TimerDatabase();
    public string PluginPath = Server.GameDirectory + "/csgo/addons/counterstrikesharp/plugins/SurfTimer/";
    internal Map CurrentMap = null!;

    /* ========== MAP START HOOKS ========== */
    public void OnMapStart(string mapName)
    {
        // Initialise Map Object
        // To-do: It seems like players connect very quickly and sometimes `CurrentMap` is null when it shouldn't be, lowered the timer ot 1.0 seconds for now
        if ((CurrentMap == null || CurrentMap.Name.Equals(mapName) == false) && mapName.Contains("surf_"))
        {
            AddTimer(1.0f, () => CurrentMap = new Map(mapName, DB!)); // Was 3 seconds, now 1 second

            AddTimer(3.0f, () =>Console.WriteLine(String.Format("  ____________    ____         ___\n"
                                    + " / ___/ __/_  |  / __/_ ______/ _/\n"
                                    + "/ /___\\ \\/ __/  _\\ \\/ // / __/ _/ \n"
                                    + "\\___/___/____/ /___/\\_,_/_/ /_/\n"  
                                    + $"[CS2 Surf] SurfTimer {ModuleVersion} - loading map {mapName}.\n"
                                    + $"[CS2 Surf] This software is licensed under the GNU Affero General Public License v3.0. See LICENSE for more information.\n"
                                    + $"[CS2 Surf] ---> Source Code: https://github.com/CS2Surf/Timer\n"
                                    + $"[CS2 Surf] ---> License Agreement: https://github.com/CS2Surf/Timer/blob/main/LICENSE\n"
            )));
        }
    }

    public void OnMapEnd()
    {
        // Clear/reset stuff here
        CurrentMap = null!;
        playerList.Clear();
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Load cvars/other configs here
        // Execute server_settings.cfg

        ConVarHelper.RemoveCheatFlagFromConVar("bot_stop");
        ConVarHelper.RemoveCheatFlagFromConVar("bot_freeze");
        ConVarHelper.RemoveCheatFlagFromConVar("bot_zombie");

        Server.ExecuteCommand("execifexists SurfTimer/server_settings.cfg");
        Console.WriteLine("[CS2 Surf] Executed configuration: server_settings.cfg");
        return HookResult.Continue;
    }

    /* ========== PLUGIN LOAD ========== */
    public override void Load(bool hotReload)
    {
        // Load database config & spawn database object
        try
        {
            JsonElement dbConfig = JsonDocument.Parse(File.ReadAllText(Server.GameDirectory + "/csgo/cfg/SurfTimer/database.json")).RootElement;
            DB = new TimerDatabase(dbConfig.GetProperty("host").GetString()!,
                                    dbConfig.GetProperty("database").GetString()!,
                                    dbConfig.GetProperty("user").GetString()!,
                                    dbConfig.GetProperty("password").GetString()!,
                                    dbConfig.GetProperty("port").GetInt32(),
                                    dbConfig.GetProperty("timeout").GetInt32());
            Console.WriteLine("[CS2 Surf] Database connection established.");
        }

        catch (Exception e)
        {
            Console.WriteLine($"[CS2 Surf] Error loading database config: {e.Message}");
            // To-do: Abort plugin loading
        }

        Console.WriteLine(String.Format("  ____________    ____         ___\n"
                                    + " / ___/ __/_  |  / __/_ ______/ _/\n"
                                    + "/ /___\\ \\/ __/  _\\ \\/ // / __/ _/ \n"
                                    + "\\___/___/____/ /___/\\_,_/_/ /_/\n"  
                                    + $"[CS2 Surf] SurfTimer plugin loaded. Version: {ModuleVersion}\n"
                                    + $"[CS2 Surf] This plugin is licensed under the GNU Affero General Public License v3.0. See LICENSE for more information. Source code: https://github.com/CS2Surf/Timer\n"
        ));

        // Map Start Hook
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        // Map End Hook
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        // Tick listener
        RegisterListener<Listeners.OnTick>(OnTick);

        // StartTouch Hook -- DEPRECATE BROKEN CS2 7TH FEB 2024
        // VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(OnTriggerStartTouch, HookMode.Post); 
        // EndTouch Hook -- DEPRECATE BROKEN CS2 7TH FEB 2024
        // VirtualFunctions.CBaseTrigger_EndTouchFunc.Hook(OnTriggerEndTouch, HookMode.Post); 

        HookEntityOutput("trigger_multiple", "OnStartTouch", OnTriggerStartTouch);
        HookEntityOutput("trigger_multiple", "OnEndTouch", OnTriggerEndTouch);
    }
}

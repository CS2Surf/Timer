/*
                   ___   _____   ____________  ___
                  ___   /  _/ | / / ____/ __ \/   |
                 ___    / //  |/ / /_  / /_/ / /| |
                ___   _/ // /|  / __/ / _, _/ ___ |
               ___   /___/_/ |_/_/   /_/ |_/_/  |_|

             ___   ___________ __    ___   _____ __  ______
            ___   /_  __/ ___// /   /   | / ___// / / / __ \
           ___     / /  \__ \/ /   / /| | \__ \/ /_/ / / / /
          ___     / /  ___/ / /___/ ___ |___/ / __  / /_/ /
         ___     /_/  /____/_____/_/  |_/____/_/ /_/_____/

    Official Timer plugin for the CS2 Surf Initiative.
    Copyright (C) 2024  Liam C. (Infra)
    Copyright (C) 2025  tslashd

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

using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Data;

namespace SurfTimer;

// Gameplan: https://github.com/CS2Surf/Timer/tree/dev/README.md
[MinimumApiVersion(333)]
public partial class SurfTimer : BasePlugin
{
    private readonly ILogger<SurfTimer> _logger;
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    private readonly IDataAccessService? _dataService;

    // Inject ILogger and store IServiceProvider globally
    public SurfTimer(ILogger<SurfTimer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        ServiceProvider = serviceProvider;
        _dataService = ServiceProvider.GetRequiredService<IDataAccessService>();
    }

    // Metadata
    public override string ModuleName => $"CS2 {Config.PluginName}";
    public override string ModuleVersion => "DEV-1";
    public override string ModuleDescription => "Official Surf Timer by the CS2 Surf Initiative.";
    public override string ModuleAuthor => "The CS2 Surf Initiative - github.com/cs2surf";

    // Globals
    private readonly ConcurrentDictionary<int, Player> playerList = new();
    internal static readonly TimerDatabase DB = new(Config.MySql.GetConnectionString()); // Initiate it with the correct connection string
    public static Map CurrentMap { get; private set; } = null!;

    /* ========== MAP START HOOKS ========== */
    public void OnMapStart(string mapName)
    {
        // Initialise Map Object
        if ((CurrentMap == null || CurrentMap.Name!.Equals(mapName)) && mapName.Contains("surf_"))
        {
            _logger.LogInformation(
                "[{Prefix}] New map {MapName} started. Initializing Map object.....",
                Config.PluginName,
                mapName
            );

            Server.NextWorldUpdateAsync(async () => // NextWorldUpdate runs even during server hibernation
            {
                _logger.LogInformation(
                    "{PluginLogo}\n"
                        + "[CS2 Surf] {PluginName} v.{ModuleVersion} - loading map {MapName}.\n"
                        + "[CS2 Surf] This software is licensed under the GNU Affero General Public License v3.0. See LICENSE for more information.\n"
                        + "[CS2 Surf] ---> Source Code: https://github.com/CS2Surf/Timer\n"
                        + "[CS2 Surf] ---> License Agreement: https://github.com/CS2Surf/Timer/blob/main/LICENSE\n",
                    Config.PluginLogo,
                    Config.PluginName,
                    ModuleVersion,
                    mapName
                );

                CurrentMap = new Map(mapName);
                await CurrentMap.InitializeAsync();
            });
        }
    }

    public void OnMapEnd()
    {
        _logger.LogInformation(
            "[{Prefix}] Map ({MapName}) ended. Cleaning up resources...",
            Config.PluginName, 
            CurrentMap.Name
        );

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
        _logger.LogTrace(
            "[{Prefix}] Executed configuration: server_settings.cfg",
            Config.PluginName
        );
        return HookResult.Continue;
    }

    /* ========== PLUGIN LOAD ========== */
    public override void Load(bool hotReload)
    {
        LocalizationService.Init(Localizer);

        bool accessService = false;

        try
        {
            accessService = Task.Run(() => _dataService!.PingAccessService())
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{Prefix}] PingAccessService threw an exception.",
                Config.PluginName
            );
        }

        if (accessService)
        {
            _logger.LogInformation(
                "[{Prefix}] {AccessService} connection established.",
                Config.PluginName,
                Config.Api.GetApiOnly() ? "API" : "DB"
            );
        }
        else
        {
            _logger.LogCritical(
                "[{Prefix}] Error connecting to the {AccessService}.",
                Config.PluginName,
                Config.Api.GetApiOnly() ? "API" : "DB"
            );

            Exception exception = new Exception(
                $"[{Config.PluginName}] Error connecting to the {(Config.Api.GetApiOnly() ? "API" : "DB")}"
            );
            throw exception;
        }

        _logger.LogInformation(
            """  
                {PluginLogo}  
                [CS2 Surf] {PluginName} plugin loaded. Version: {ModuleVersion}
                [CS2 Surf] This plugin is licensed under the GNU Affero General Public License v3.0. See LICENSE for more information. 
                Source code: https://github.com/CS2Surf/Timer
            """,
            Config.PluginLogo,
            Config.PluginName,
            ModuleVersion
        );

        // Map Start Hook
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        // Map End Hook
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        // Tick listener
        RegisterListener<Listeners.OnTick>(OnTick);

        HookEntityOutput("trigger_multiple", "OnStartTouch", OnTriggerStartTouch);
        HookEntityOutput("trigger_multiple", "OnEndTouch", OnTriggerEndTouch);
    }
}

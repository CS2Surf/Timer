using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;

namespace SurfTimer;

public partial class SurfTimer
{
    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var controller = @event.Userid;
        if (!controller!.IsValid || !controller.IsBot || CurrentMap.ReplayManager.IsControllerConnectedToReplayPlayer(controller))
            return HookResult.Continue;

        _logger.LogTrace("OnPlayerSpawn -> Player {Name} spawned.",
            controller.PlayerName
        );

        // Set the controller for the MapWR bot
        if (!CurrentMap.ReplayManager!.MapWR.IsPlayable && controller.IsBot)
        {
            CurrentMap.ReplayManager.MapWR.SetController(controller, -1);
            CurrentMap.ReplayManager.MapWR.LoadReplayData();

            AddTimer(1.5f, () =>
            {
                CurrentMap.ReplayManager.MapWR.Controller!.RemoveWeapons();
                CurrentMap.ReplayManager.MapWR.Start();
                CurrentMap.ReplayManager.MapWR.FormatBotName();
            });

            return HookResult.Continue;
        }

        // Set the controller for the StageWR bot
        if (CurrentMap.ReplayManager.StageWR != null && !CurrentMap.ReplayManager.StageWR.IsPlayable && controller.IsBot)
        {
            CurrentMap.ReplayManager.StageWR.SetController(controller, 3);
            CurrentMap.ReplayManager.StageWR.LoadReplayData(repeat_count: 3);

            AddTimer(1.5f, () =>
            {
                CurrentMap.ReplayManager.StageWR.Controller!.RemoveWeapons();
                CurrentMap.ReplayManager.StageWR.Start();
                CurrentMap.ReplayManager.StageWR.FormatBotName();
            });

            return HookResult.Continue;
        }

        // Spawn the BonusWR bot
        if (CurrentMap.ReplayManager.BonusWR != null && !CurrentMap.ReplayManager.BonusWR.IsPlayable && controller.IsBot)
        {
            CurrentMap.ReplayManager.BonusWR.SetController(controller, 3);
            CurrentMap.ReplayManager.BonusWR.LoadReplayData();

            AddTimer(1.5f, () =>
            {
                CurrentMap.ReplayManager.BonusWR.Controller!.RemoveWeapons();
                CurrentMap.ReplayManager.BonusWR.Start();
                CurrentMap.ReplayManager.BonusWR.FormatBotName();
            });

            return HookResult.Continue;
        }

        // // Spawn the CustomReplays bot (for PB replays?) - T
        // CurrentMap.ReplayManager.CustomReplays.ForEach(replay =>
        // {
        //     if (!replay.IsPlayable)
        //     {
        //         replay.SetController(controller, 3);
        //         replay.LoadReplayData();

        //         AddTimer(1.5f, () => {
        //             replay.Controller!.RemoveWeapons();
        //             replay.Start();
        //             replay.FormatBotName();
        //         });

        //         return;
        //     }
        // });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;

        string name = player!.PlayerName;
        string country;

        // GeoIP
        // Check if the IP is private before attempting GeoIP lookup
        string ipAddress = player.IpAddress!.Split(":")[0];
        if (!IsPrivateIP(ipAddress))
        {
            DatabaseReader geoipDB = new(Config.PluginPath + "data/GeoIP/GeoLite2-Country.mmdb");
            country = geoipDB.Country(ipAddress).Country.IsoCode ?? "XX";
            geoipDB.Dispose();
        }
        else
        {
            country = "LL";  // Handle local IP appropriately
        }
        // #if DEBUG
        //         Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnectFull -> GeoIP -> {name} -> {player.IpAddress!.Split(":")[0]} -> {country}");
        // #endif
        if (DB == null)
        {
            _logger.LogCritical("OnPlayerConnect -> DB object is null, this shouldn't happen.");
            Exception ex = new("CS2 Surf ERROR >> OnPlayerConnect -> DB object is null, this shouldn't happen.");
            throw ex;
        }

        // Create Player object and add to playerList
        PlayerProfile Profile = PlayerProfile.CreateAsync(player.SteamID, name, country).GetAwaiter().GetResult();
        playerList[player.UserId ?? 0] = new Player(player,
                                                new CCSPlayer_MovementServices(player.PlayerPawn.Value!.MovementServices!.Handle),
                                                Profile, CurrentMap);

        // Load MapTimes for the player's PB and their Checkpoints
        playerList[player.UserId ?? 0].Stats.LoadPlayerMapTimesData(playerList[player.UserId ?? 0]).GetAwaiter().GetResult(); // Holds here until result is available

        // Print join messages
        Server.PrintToChatAll($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["player_connected",
            name, country]}"
        );
        _logger.LogTrace("[{Prefix}] {PlayerName} has connected from {Country}.",
            Config.PluginName, name, playerList[player.UserId ?? 0].Profile.Country
        );
        return HookResult.Continue;
    }

    [GameEventHandler] // Player Disconnect Event
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null)
        {
            _logger.LogError("OnPlayerDisconnect -> 'player' is NULL ({IsNull})",
                player == null
            );
            return HookResult.Continue;
        }

        if (CurrentMap.ReplayManager.MapWR.Controller != null && CurrentMap.ReplayManager.MapWR.Controller.Equals(player) && CurrentMap.ReplayManager.MapWR.MapID != -1)
            CurrentMap.ReplayManager.MapWR.Reset();

        if (CurrentMap.ReplayManager.StageWR != null && CurrentMap.ReplayManager.StageWR.Controller != null && CurrentMap.ReplayManager.StageWR.Controller.Equals(player) && CurrentMap.ReplayManager.StageWR.MapID != -1)
            CurrentMap.ReplayManager.StageWR.Reset();

        if (CurrentMap.ReplayManager.BonusWR != null && CurrentMap.ReplayManager.BonusWR.Controller != null && CurrentMap.ReplayManager.BonusWR.Controller.Equals(player))
            CurrentMap.ReplayManager.BonusWR!.Reset();

        for (int i = 0; i < CurrentMap.ReplayManager.CustomReplays.Count; i++)
            if (CurrentMap.ReplayManager.CustomReplays[i].Controller != null && CurrentMap.ReplayManager.CustomReplays[i].Controller!.Equals(player))
                CurrentMap.ReplayManager.CustomReplays[i].Reset();


        if (player.IsBot || !player.IsValid)
        {
            return HookResult.Continue;
        }
        else
        {
            if (DB == null)
            {
                _logger.LogCritical("OnPlayerDisconnect -> DB object is null, this shouldnt happen.");
                throw new Exception("CS2 Surf ERROR >> OnPlayerDisconnect -> DB object is null, this shouldnt happen.");
            }

            if (!playerList.ContainsKey(player.UserId ?? 0))
            {
                _logger.LogError("OnPlayerDisconnect -> playerList does NOT contain player.UserId, this shouldn't happen. Player: {PlayerName} ({UserId})",
                    player.PlayerName, player.UserId
                );
            }
            else
            {
                // Update data in Player DB table
                playerList[player.UserId ?? 0].Profile.UpdatePlayerProfile(player.PlayerName).GetAwaiter().GetResult(); // Hold the thread until player data is updated

                // Remove player data from playerList
                playerList.Remove(player.UserId ?? 0);
            }
            return HookResult.Continue;
        }
    }

    /// <summary>
    /// Checks whether an IP is a local one. Allows testing the plugin in a local environment setup for GeoIP
    /// </summary>
    /// <param name="ip">IP to check</param>
    /// <returns>True for Private IP</returns>
    static bool IsPrivateIP(string ip)
    {
        var ipParts = ip.Split('.');
        int firstOctet = int.Parse(ipParts[0]);
        int secondOctet = int.Parse(ipParts[1]);

        // 10.x.x.x range
        if (firstOctet == 10)
            return true;

        // 172.16.x.x to 172.31.x.x range
        if (firstOctet == 172 && (secondOctet >= 16 && secondOctet <= 31))
            return true;

        // 192.168.x.x range
        if (firstOctet == 192 && secondOctet == 168)
            return true;

        return false;
    }
}
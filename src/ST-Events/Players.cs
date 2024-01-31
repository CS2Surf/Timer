using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using MaxMind.GeoIP2;

namespace SurfTimer;

public partial class SurfTimer
{
    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var controller = @event.Userid;
        if(!controller.IsValid || !controller.IsBot)
            return HookResult.Continue;

        for (int i = 0; i < CurrentMap.ReplayBots.Count; i++)
        {
            if(CurrentMap.ReplayBots[i].IsPlayable)
                continue;

            int repeats = -1;
            if(CurrentMap.ReplayBots[i].Stat_Prefix == "PB")
                repeats = 3;
            
            CurrentMap.ReplayBots[i].SetController(controller, repeats);
            Server.PrintToChatAll($"{PluginPrefix} {ChatColors.Lime}Loading replay data...");
            AddTimer(2f, () => {
                if(!CurrentMap.ReplayBots[i].IsPlayable)
                    return;

                CurrentMap.ReplayBots[i].Controller!.RemoveWeapons();
                
                CurrentMap.ReplayBots[i].LoadReplayData(DB!);

                CurrentMap.ReplayBots[i].Start();
            });
            
            return HookResult.Continue;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        #if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnect -> {player.PlayerName} / {player.UserId} / {player.SteamID}");
        Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnect -> {player.PlayerName} / {player.UserId} / Bot Diff: {player.PawnBotDifficulty}");
        #endif

        if (player.IsBot || !player.IsValid)
        {
            return HookResult.Continue;
        }
        else
        {
            int dbID, joinDate, lastSeen, connections;
            string name, country; 
            
            // GeoIP
            DatabaseReader geoipDB = new DatabaseReader(PluginPath + "data/GeoIP/GeoLite2-Country.mmdb");
            if (geoipDB.Country(player.IpAddress!.Split(":")[0]).Country.IsoCode is not null)
            {
                country = geoipDB.Country(player.IpAddress!.Split(":")[0]).Country.IsoCode!;
                #if DEBUG
                Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnect -> GeoIP -> {player.PlayerName} -> {player.IpAddress!.Split(":")[0]} -> {country}");
                #endif
            }
            else
                country = "XX";
            geoipDB.Dispose();

            if (DB == null)
                throw new Exception("CS2 Surf ERROR >> OnPlayerConnect -> DB object is null, this shouldnt happen.");

            // Load player profile data from database (or create an entry if first time connecting)
            Task<MySqlDataReader> dbTask = DB.Query($"SELECT * FROM `Player` WHERE `steam_id` = {player.SteamID} LIMIT 1;");
            MySqlDataReader playerData = dbTask.Result;
            if (playerData.HasRows && playerData.Read())
            {
                // Player exists in database
                dbID = playerData.GetInt32("id");
                name = playerData.GetString("name");
                if (country == "XX" && playerData.GetString("country") != "XX")
                    country = playerData.GetString("country");
                joinDate = playerData.GetInt32("join_date");
                lastSeen = playerData.GetInt32("last_seen");
                connections = playerData.GetInt32("connections");
                playerData.Close();

                #if DEBUG
                Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnect -> Returning player {name} ({player.SteamID}) loaded from database with ID {dbID}");
                #endif
            }
            else
            {
                playerData.Close();
                // Player does not exist in database
                name = player.PlayerName;
                joinDate = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                lastSeen = joinDate;
                connections = 1;

                // Write new player to database
                Task<int> newPlayerTask = DB.Write($@"
                    INSERT INTO `Player` (`name`, `steam_id`, `country`, `join_date`, `last_seen`, `connections`) 
                    VALUES ('{MySqlHelper.EscapeString(name)}', {player.SteamID}, '{country}', {joinDate}, {lastSeen}, {connections});
                ");
                int newPlayerTaskRows = newPlayerTask.Result;
                if (newPlayerTaskRows != 1)
                    throw new Exception($"CS2 Surf ERROR >> OnPlayerConnect -> Failed to write new player to database, this shouldnt happen. Player: {name} ({player.SteamID})");
                newPlayerTask.Dispose(); 

                // Get new player's database ID
                Task<MySqlDataReader> newPlayerDataTask = DB.Query($"SELECT `id` FROM `Player` WHERE `steam_id` = {player.SteamID} LIMIT 1;");
                MySqlDataReader newPlayerData = newPlayerDataTask.Result;
                if (newPlayerData.HasRows && newPlayerData.Read()) 
                {
                    #if DEBUG
                    // Iterate through data: 
                    for (int i = 0; i < newPlayerData.FieldCount; i++)
                    {
                        Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnect -> newPlayerData[{i}] = {newPlayerData.GetValue(i)}");
                    }
                    #endif
                    dbID = newPlayerData.GetInt32("id");
                }
                else
                    throw new Exception($"CS2 Surf ERROR >> OnPlayerConnect -> Failed to get new player's database ID after writing, this shouldnt happen. Player: {name} ({player.SteamID})");
                newPlayerData.Close();

                #if DEBUG
                Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnect -> New player {name} ({player.SteamID}) added to database with ID {dbID}");
                #endif
            }
            dbTask.Dispose();

            // Create Player object and add to playerList
            PlayerProfile Profile = new PlayerProfile(dbID, name, player.SteamID, country, joinDate, lastSeen, connections);
            playerList[player.UserId ?? 0] = new Player(player, 
                                                    new CCSPlayer_MovementServices(player.PlayerPawn.Value!.MovementServices!.Handle),
                                                    Profile, CurrentMap);
            
            #if DEBUG
            Console.WriteLine($"=================================== SELECT * FROM `MapTimes` WHERE `player_id` = {playerList[player.UserId ?? 0].Profile.ID} AND `map_id` = {CurrentMap.ID};");
            #endif

            // To-do: hardcoded Style value
            // Load MapTimes for the player's PB and their Checkpoints
            playerList[player.UserId ?? 0].Stats.LoadMapTimesData(playerList[player.UserId ?? 0], DB); // Will reload PB and Checkpoints for the player for all styles
            playerList[player.UserId ?? 0].Stats.LoadCheckpointsData(DB); // To-do: This really should go inside `LoadMapTimesData` imo cuz here we hardcoding load for Style 0

            // Print join messages
            Server.PrintToChatAll($"{PluginPrefix} {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has connected from {ChatColors.Lime}{playerList[player.UserId ?? 0].Profile.Country}{ChatColors.Default}.");
            Console.WriteLine($"[CS2 Surf] {player.PlayerName} has connected from {playerList[player.UserId ?? 0].Profile.Country}.");
            return HookResult.Continue;
        }
    }

    [GameEventHandler] // Player Disconnect Event
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;

        for (int i = 0; i < CurrentMap.ReplayBots.Count; i++)
            if (CurrentMap.ReplayBots[i].IsPlayable && CurrentMap.ReplayBots[i].Controller!.Equals(player) && CurrentMap.ReplayBots[i].Stat_MapTimeID != -1)
                CurrentMap.ReplayBots[i].Reset();

        if (player.IsBot || !player.IsValid)
        {
            return HookResult.Continue;
        }
        
        else
        {
            if (DB == null)
                throw new Exception("CS2 Surf ERROR >> OnPlayerDisconnect -> DB object is null, this shouldnt happen.");

            if (!playerList.ContainsKey(player.UserId ?? 0))
            {
                Console.WriteLine($"CS2 Surf ERROR >> OnPlayerDisconnect -> Player playerList does NOT contain player.UserId, this shouldn't happen. Player: {player.PlayerName} ({player.UserId})");
            }
            else
            {
                // Update data in Player DB table
                Task<int> updatePlayerTask = DB.Write($@"
                    UPDATE `Player` SET country = '{playerList[player.UserId ?? 0].Profile.Country}', 
                    `last_seen` = {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, `connections` = `connections` + 1 
                    WHERE `id` = {playerList[player.UserId ?? 0].Profile.ID} LIMIT 1;
                ");
                if (updatePlayerTask.Result != 1)
                    throw new Exception($"CS2 Surf ERROR >> OnPlayerDisconnect -> Failed to update player data in database. Player: {player.PlayerName} ({player.SteamID})");
                // Player disconnection to-do
                updatePlayerTask.Dispose();

                // Remove player data from playerList
                playerList.Remove(player.UserId ?? 0);
            }
            return HookResult.Continue;
        }
    }
}
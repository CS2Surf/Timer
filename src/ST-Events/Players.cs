using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using MaxMind.GeoIP2;

namespace SurfTimer;

public partial class SurfTimer
{
    [GameEventHandler] // Player Connect Event
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        #if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> OnPlayerConnect -> {player.PlayerName} / {player.UserId} / {player.SteamID}");
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

            // Load player data from database (or create an entry if first time connecting)
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
                Task<int> newPlayerTask = DB.Write($"INSERT INTO `Player` (`name`, `steam_id`, `country`, `join_date`, `last_seen`, `connections`) VALUES ('{MySqlHelper.EscapeString(name)}', {player.SteamID}, '{country}', {joinDate}, {lastSeen}, {connections});");
                int newPlayerTaskRows = newPlayerTask.Result;
                if (newPlayerTaskRows != 1)
                    throw new Exception($"CS2 Surf ERROR >> OnPlayerConnect -> Failed to write new player to database, this shouldnt happen. Player: {name} ({player.SteamID})");
                    
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
            PlayerProfile Profile = new PlayerProfile(dbID, name, player.SteamID, country, joinDate, lastSeen, connections);

            // Create Player object
            playerList[player.UserId ?? 0] = new Player(player, 
                                                    new CCSPlayer_MovementServices(player.PlayerPawn.Value!.MovementServices!.Handle),
                                                    Profile);
            
            // Print join messages
            Server.PrintToChatAll($"{PluginPrefix} {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has connected from {playerList[player.UserId ?? 0].Profile.Country}.");
            Console.WriteLine($"[CS2 Surf] {player.PlayerName} has connected from {playerList[player.UserId ?? 0].Profile.Country}.");
            return HookResult.Continue;
        }
    }

    [GameEventHandler] // Player Disconnect Event
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player.IsBot || !player.IsValid)
        {
            return HookResult.Continue;
        }
        
        else
        {
            // Update data in Player DB table
            Task<int> updatePlayerTask = DB.Write($"UPDATE `Player` SET country = '{playerList[player.UserId ?? 0].Profile.Country}', `last_seen` = {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, `connections` = `connections` + 1 WHERE `id` = {playerList[player.UserId ?? 0].Profile.ID} LIMIT 1;");
            if (updatePlayerTask.Result != 1)
                throw new Exception($"CS2 Surf ERROR >> OnPlayerDisconnect -> Failed to update player data in database. Player: {player.PlayerName} ({player.SteamID})");

            // Player disconnection to-do

            // Remove player data from playerList
            playerList.Remove(player.UserId ?? 0);
            return HookResult.Continue;
        }
    }
}
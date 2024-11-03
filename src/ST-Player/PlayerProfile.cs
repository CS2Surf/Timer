using CounterStrikeSharp.API.Core;
using Microsoft.VisualBasic;

namespace SurfTimer;

internal class PlayerProfile
{
    public int ID { get; set; } = 0;
    public string Name { get; set; } = "";
    public ulong SteamID { get; set; } = 0;
    public string Country { get; set; } = "";
    public int JoinDate { get; set; } = 0;
    public int LastSeen { get; set; } = 0;
    public int Connections { get; set; } = 0;

    public PlayerProfile(ulong steamId, string name = "", string country = "")
    {
        this.SteamID = steamId;
        this.Name = name;
        this.Country = country;
    }

    /// <summary>
    /// Deals with retrieving, creating and updating a Player's information in the database upon joining the server.
    /// Automatically detects whether to use API Calls or Queries.
    /// </summary>
    /// <param name="steamId">Steam ID of the player</param>
    /// <param name="name">Name of the player</param>
    /// <param name="country">Country of the player</param>
    /// <returns cref="PlayerProfile">PlayerProfile object</returns>
    public static async Task<PlayerProfile> CreateAsync(ulong steamId, string name = "", string country = "")
    {
        var profile = new PlayerProfile(steamId, name, country);
        await profile.InitializeAsync();
        return profile;
    }

    private async Task InitializeAsync()
    {
        await Get_Player_Profile();

        Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerProfile -> InitializeAsync -> [{(Config.API.GetApiOnly() ? "API" : "DB")}] We got ProfileID = {this.ID} ({this.Name})");
    }

    /// <summary>
    /// Retrieves all the data for the player from the database.
    /// </summary>
    public async Task Get_Player_Profile()
    {
        bool newPlayer = false;

        // Load player profile data from database
        using (var playerData = await SurfTimer.DB.QueryAsync(string.Format(Config.MySQL.Queries.DB_QUERY_PP_GET_PROFILE, this.SteamID)))
        {
            if (playerData.HasRows && playerData.Read())
            {
                // Player exists in database
                this.ID = playerData.GetInt32("id");
                this.Name = playerData.GetString("name");
                if (this.Country == "XX" && playerData.GetString("country") != "XX")
                    this.Country = playerData.GetString("country");
                this.JoinDate = playerData.GetInt32("join_date");
                this.LastSeen = playerData.GetInt32("last_seen");
                this.Connections = playerData.GetInt32("connections");
            }
            else
            {
                newPlayer = true;
            }
        }

#if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerProfile -> InitializeAsync -> [{(Config.API.GetApiOnly() ? "API" : "DB")}] Returning player {this.Name} ({this.SteamID}) loaded from database with ID {this.ID}");
#endif
        if (newPlayer)
            await Insert_Player_Profile();
    }

    /// <summary>
    /// Insert new player information into the database.
    /// Retrieves the ID of the newly created player.
    /// </summary>
    public async Task Insert_Player_Profile()
    {
        // Player does not exist in database
        int joinDate = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int lastSeen = joinDate;
        int connections = 1;

        // Write new player to database
        int newPlayerRows = await SurfTimer.DB.WriteAsync(string.Format(
            Config.MySQL.Queries.DB_QUERY_PP_INSERT_PROFILE,
            MySqlConnector.MySqlHelper.EscapeString(this.Name), this.SteamID, this.Country, joinDate, lastSeen, connections));
        if (newPlayerRows != 1)
        {
            Exception ex = new($"Error inserting new player profile for '{this.Name}' ({this.SteamID})");
            throw ex;
        }

        await Get_Player_Profile();
#if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerProfile -> Insert_Player_Profile -> [{(Config.API.GetApiOnly() ? "API" : "DB")}] New player {this.Name} ({this.SteamID}) added to database with ID {this.ID}");
#endif
    }

    /// <summary>
    /// Updates the information in the database for the player. Increments `connections` and changes nickname.
    /// </summary>
    /// <param name="name">Player Name</param>
    /// <exception cref="Exception"></exception>
    public async Task Update_Player_Profile(string name)
    {
        int updatePlayerTask = await SurfTimer.DB.WriteAsync(string.Format(Config.MySQL.Queries.DB_QUERY_PP_UPDATE_PROFILE, this.Country, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), this.ID, name));
        if (updatePlayerTask != 1)
            throw new Exception($"CS2 Surf ERROR >> internal class PlayerProfile -> Update_Player_Profile -> [{(Config.API.GetApiOnly() ? "API" : "DB")}] Failed to update player data in database. Player: {this.Name} ({this.SteamID})");
#if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> internal class PlayerProfile -> Update_Player_Profile -> [{(Config.API.GetApiOnly() ? "API" : "DB")}] Updated player {name} ({this.SteamID}) in database. ID {this.ID}");
#endif
    }
}
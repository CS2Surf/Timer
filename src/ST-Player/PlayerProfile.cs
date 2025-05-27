using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<PlayerProfile> _logger;

    public PlayerProfile(ulong steamId, string name = "", string country = "")
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<PlayerProfile>>();

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

    private async Task InitializeAsync([CallerMemberName] string methodName = "")
    {
        await Get_Player_Profile();

        _logger.LogTrace("[{ClassName}] {MethodName} -> InitializeAsync -> [{ConnType}] We got ProfileID {ProfileID} ({PlayerName})",
            nameof(PlayerProfile), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.ID, this.Name
        );
    }

    /// <summary>
    /// Retrieves all the data for the player from the database.
    /// </summary>
    public async Task Get_Player_Profile([CallerMemberName] string methodName = "")
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
        _logger.LogDebug("[{ClassName}] {MethodName} -> Get_Player_Profile -> [{ConnType}] Returning player {PlayerName} ({SteamID}) loaded with ID {ProfileID}.",
            nameof(PlayerProfile), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.Name, this.SteamID, this.ID
        );
#endif
        if (newPlayer)
            await Insert_Player_Profile();
    }

    /// <summary>
    /// Insert new player information into the database.
    /// Retrieves the ID of the newly created player.
    /// </summary>
    public async Task Insert_Player_Profile([CallerMemberName] string methodName = "")
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
        _logger.LogDebug("[{ClassName}] {MethodName} -> Insert_Player_Profile -> [{ConnType}] New player {PlayerName} ({SteamID}) added with ID {ProfileID}.",
            nameof(PlayerProfile), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.Name, this.SteamID, this.ID
        );
#endif
    }

    /// <summary>
    /// Updates the information in the database for the player. Increments `connections` and changes nickname.
    /// </summary>
    /// <param name="name">Player Name</param>
    /// <exception cref="Exception"></exception>
    public async Task Update_Player_Profile(string name, [CallerMemberName] string methodName = "")
    {
        int updatePlayerTask = await SurfTimer.DB.WriteAsync(string.Format(Config.MySQL.Queries.DB_QUERY_PP_UPDATE_PROFILE, this.Country, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), this.ID, name));
        if (updatePlayerTask != 1)
        {
            _logger.LogError("[{ClassName}] {MethodName} -> Update_Player_Profile -> [{ConnType}] Failed to update data in database. Player {PlayerName} ({SteamID})",
                nameof(PlayerProfile), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.Name, this.SteamID
            );
            throw new Exception($"CS2 Surf ERROR >> internal class PlayerProfile -> Update_Player_Profile -> [{(Config.API.GetApiOnly() ? "API" : "DB")}] Failed to update player data in database. Player: {this.Name} ({this.SteamID})");
        }
#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> Update_Player_Profile -> [{ConnType}] Updated player {PlayerName} ({SteamID}) in database with ID {ProfileID}.",
            nameof(PlayerProfile), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.Name, this.SteamID, this.ID
        );
#endif
    }
}
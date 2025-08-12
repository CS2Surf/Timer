using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurfTimer.Data;
using SurfTimer.Shared.DTO;
using SurfTimer.Shared.Entities;
using System.Runtime.CompilerServices;

namespace SurfTimer;

public class PlayerProfile : PlayerProfileEntity
{
    private readonly ILogger<PlayerProfile> _logger;
    private readonly IDataAccessService _dataService;

    internal PlayerProfile(ulong steamId, string name = "", string country = "")
    {
        // Resolve the logger instance from the DI container
        _logger = SurfTimer.ServiceProvider.GetRequiredService<ILogger<PlayerProfile>>();
        _dataService = SurfTimer.ServiceProvider.GetRequiredService<IDataAccessService>();


        this.SteamID = steamId;
        this.Name = name;
        this.Country = country;
    }

    /// <summary>
    /// Deals with retrieving, creating and updating a Player's information in the database upon joining the server.
    /// </summary>
    /// <param name="steamId">Steam ID of the player</param>
    /// <param name="name">Name of the player</param>
    /// <param name="country">Country of the player</param>
    /// <returns cref="PlayerProfile">PlayerProfile object</returns>
    internal static async Task<PlayerProfile> CreateAsync(ulong steamId, string name = "", string country = "")
    {
        var profile = new PlayerProfile(steamId, name, country);
        await profile.InitializeAsync();
        return profile;
    }

    internal async Task InitializeAsync([CallerMemberName] string methodName = "")
    {
        await GetPlayerProfile();

        _logger.LogTrace("[{ClassName}] {MethodName} -> InitializeAsync -> [{ConnType}] We got ProfileID {ProfileID} ({PlayerName})",
            nameof(PlayerProfile), methodName, Config.Api.GetApiOnly() ? "API" : "DB", this.ID, this.Name
        );
    }

    /// <summary>
    /// Retrieves all the data for the player profile from the database.
    /// </summary>
    internal async Task GetPlayerProfile([CallerMemberName] string methodName = "")
    {
        var profile = await _dataService.GetPlayerProfileAsync(this.SteamID);

        if (profile != null)
        {
            this.ID = profile.ID;
            this.Name = profile.Name;
            if (this.Country == "XX" && profile.Country != "XX")
                this.Country = profile.Country;
            this.JoinDate = profile.JoinDate;
            this.LastSeen = profile.LastSeen;
            this.Connections = profile.Connections;
        }
        else
        {
            await InsertPlayerProfile();
        }

#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> GetPlayerProfile -> [{ConnType}] Loaded player {PlayerName} ({SteamID}) with ID {ProfileID}.",
            nameof(PlayerProfile), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.Name, this.SteamID, this.ID
        );
#endif
    }

    /// <summary>
    /// Insert new player profile information into the database.
    /// Retrieves the ID of the newly created player.
    /// </summary>
    internal async Task InsertPlayerProfile([CallerMemberName] string methodName = "")
    {
        var profile = new PlayerProfileDto
        {
            SteamID = this.SteamID,
            Name = this.Name!,
            Country = this.Country!
        };

        this.ID = await _dataService.InsertPlayerProfileAsync(profile);

#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> InsertPlayerProfile -> [{ConnType}] New player {PlayerName} ({SteamID}) added with ID {ProfileID}.",
            nameof(PlayerProfile), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.Name, this.SteamID, this.ID
        );
#endif
    }

    /// <summary>
    /// Updates the information in the database for the player profile. Increments `connections` and changes nickname.
    /// </summary>
    /// <param name="name">Player Name</param>
    internal async Task UpdatePlayerProfile(string name, [CallerMemberName] string methodName = "")
    {
        this.Name = name;
        var dto = new PlayerProfileDto
        {
            SteamID = this.SteamID,
            Name = this.Name,
            Country = this.Country!
        };

        await _dataService.UpdatePlayerProfileAsync(dto, this.ID);

#if DEBUG
        _logger.LogDebug("[{ClassName}] {MethodName} -> UpdatePlayerProfile -> [{ConnType}] Updated player {PlayerName} ({SteamID}) with ID {ProfileID}.",
            nameof(PlayerProfile), methodName, Config.API.GetApiOnly() ? "API" : "DB", this.Name, this.SteamID, this.ID
        );
#endif
    }
}
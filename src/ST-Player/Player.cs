namespace SurfTimer;
using CounterStrikeSharp.API.Core;

internal class Player 
{
    // CCS requirements
    public CCSPlayerController Controller {get;}
    public CCSPlayer_MovementServices MovementServices {get;} // Can be used later for any movement modification (eg: styles)

    // Timer-related properties
    public PlayerTimer Timer {get; set;}
    public PlayerStats Stats {get; set;}
    public PlayerHUD HUD {get; set;}

    // Player information
    public PlayerProfile Profile {get; set;}

    // Constructor
    public Player(CCSPlayerController Controller, CCSPlayer_MovementServices MovementServices, PlayerProfile Profile)
    {
        this.Controller = Controller;
        this.MovementServices = MovementServices;

        this.Profile = Profile;

        this.Timer = new PlayerTimer();
        this.Stats = new PlayerStats();

        this.HUD = new PlayerHUD(this);
    }
}

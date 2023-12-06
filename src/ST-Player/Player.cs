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

    // Player vars
    public int dbID {get; set;} = 0; // Database -> Player.id
    public int Style {get; set;} = 0; // 0 = normal, 1+ = style index

    // Constructor
    public Player(CCSPlayerController Controller, CCSPlayer_MovementServices MovementServices)
    {
        this.Controller = Controller;
        this.MovementServices = MovementServices;

        this.Timer = new PlayerTimer();
        this.Stats = new PlayerStats();

        this.HUD = new PlayerHUD(this);
    }
}

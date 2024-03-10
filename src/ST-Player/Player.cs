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
    public ReplayRecorder ReplayRecorder { get; set; }
    public List<SavelocFrame> SavedLocations { get; set; }
    public int CurrentSavedLocation { get; set; }

    // Player information
    public PlayerProfile Profile {get; set;}

    // Map information
    public Map CurrMap = null!;

    // Constructor
    public Player(CCSPlayerController Controller, CCSPlayer_MovementServices MovementServices, PlayerProfile Profile, Map CurrMap)
    {
        this.Controller = Controller;
        this.MovementServices = MovementServices;

        this.Profile = Profile;

        this.Timer = new PlayerTimer();
        this.Stats = new PlayerStats();
        this.ReplayRecorder = new ReplayRecorder();
        this.SavedLocations = new List<SavelocFrame>();
        CurrentSavedLocation = 0;

        this.HUD = new PlayerHUD(this);
        this.CurrMap = CurrMap;
    }

    /// <summary>
    /// Checks if current player is spectating player <p>
    /// </summary>
    public bool IsSpectating(CCSPlayerController p)
    {
        if(p == null || this.Controller == null || this.Controller.Team != CounterStrikeSharp.API.Modules.Utils.CsTeam.Spectator)
            return false;

        return p.Pawn.SerialNum == this.Controller.ObserverPawn.Value!.ObserverServices!.ObserverTarget.SerialNum;
    }
}

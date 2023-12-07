namespace SurfTimer;

internal class PlayerProfile
{
    public int ID {get; set;} = 0;
    public string Name {get; set;} = "";
    public ulong SteamID {get; set;} = 0;
    public string Country {get; set;} = "";
    public int JoinDate {get; set;} = 0;
    public int LastSeen {get; set;} = 0;
    public int Connections {get; set;} = 0;

    public PlayerProfile(int ID, string Name, ulong SteamID, string Country, int JoinDate, int LastSeen, int Connections)
    {
        this.ID = ID;
        this.Name = Name;
        this.SteamID = SteamID;
        this.Country = Country;
        this.JoinDate = JoinDate;
        this.LastSeen = LastSeen;
        this.Connections = Connections;
    }
}
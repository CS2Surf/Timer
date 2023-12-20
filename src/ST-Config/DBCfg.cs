namespace SurfTimer;

[Cfg("/csgo/cfg/SurfTimer/database.json")]
public class DBCfg
{

    public int Port {get; set;} = 3306;
    public int Timeout {get; set;} = 10;
    public string Host {get; set;} = "db.example.com";
    public string Database {get; set;} = "db-name";
    public string User {get; set;} = "username";
    public string Password {get; set;} = "password";

}
namespace SurfTimer;
using CounterStrikeSharp.API;
using MySqlConnector; // https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html

// This will have functions for DB access and query sending
internal class TimerDatabase 
{
    private MySqlConnection _db;

    public TimerDatabase(string host, string database, string user, string password, int port, int timeout)
    {
        this._db = new MySqlConnection($"server={host};user={user};password={password};database={database};port={port};connect timeout={timeout};");
        this._db.Open();
    }

    public void Close()
    {
        this._db.Close();
    }

    public async Task<MySqlDataReader> Read(string query)
    {
        MySqlCommand cmd = new MySqlCommand(query, this._db);
        MySqlDataReader reader = await cmd.ExecuteReaderAsync();

        return reader;
    }
}
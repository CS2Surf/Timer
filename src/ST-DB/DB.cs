namespace SurfTimer;

using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using MySqlConnector; // https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html

// This will have functions for DB access and query sending
internal class TimerDatabase 
{
    private MySqlConnection? _db;
    private string _connString;

    public TimerDatabase()
    {
        // Null'd
    }

    public TimerDatabase(string host, string database, string user, string password, int port, int timeout)
    {
        this._connString = $"server={host};user={user};password={password};database={database};port={port};connect timeout={timeout};";
        this._db = new MySqlConnection(this._connString);
        this._db.Open();
    }

    public void Close()
    {
        if (this._db != null)
            this._db!.Close();
    }

    public async Task<MySqlDataReader> Query(string query)
    {
        MySqlCommand cmd = new MySqlCommand(query, this._db);
        MySqlDataReader reader = await cmd.ExecuteReaderAsync();

        return Task.FromResult(reader).Result;
    }

    public async Task<int> Write(string query)
    {
        MySqlCommand cmd = new MySqlCommand(query, this._db);
        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        return rowsAffected;
    }
}
namespace SurfTimer;

using System;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using MySqlConnector; // https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html

// This will have functions for DB access and query sending
internal class TimerDatabase
{
    private MySqlConnection? _db;
    private readonly string _connString = string.Empty;

    public TimerDatabase()
    {
        // Null'd
    }


    public TimerDatabase(string host, string database, string user, string password, int port, int timeout)
    {
        this._connString = $"server={host};user={user};password={password};database={database};port={port};connect timeout={timeout};";
        this._db = new MySqlConnection(_connString);
        this._db.Open();
    }

    public void Close()
    {
        if (this._db != null)
            this._db!.Close();
    }

    public async Task<MySqlDataReader> Query(string query)
    {
        return await Task.Run(async () =>
        {
            try
            {
                if (this._db == null)
                {
                    throw new InvalidOperationException("Database connection is not open.");
                }

                MySqlCommand cmd = new(query, this._db);
                MySqlDataReader reader = await cmd.ExecuteReaderAsync();

                return reader;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
                throw;
            }
        });
    }

    public async Task<int> Write(string query)
    {
        return await Task.Run(async () =>
        {
            try
            {
                if (this._db == null)
                {
                    throw new InvalidOperationException("Database connection is not open.");
                }

                MySqlCommand cmd = new(query, this._db);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing write operation: {ex.Message}");
                throw;
            }
        });
    }
}

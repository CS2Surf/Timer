using MySqlConnector;

namespace SurfTimer;

internal class TimerDatabase
{
    private readonly string _connString;

    public TimerDatabase(string connectionString)
    {
        _connString = connectionString;
    }

    /// <summary>
    /// Spawns a new connection to the database.
    /// </summary>
    /// <returns cref="MySqlConnection">DB Connection</returns>
    private MySqlConnection GetConnection()
    {
        var connection = new MySqlConnection(_connString);
        try
        {
            connection.Open();
        }
        catch (MySqlException mysqlEx)  // Specifically catch MySQL-related exceptions
        {
            Console.WriteLine($"[CS2 Surf] MySQL error when connecting: {mysqlEx.Message}");
            throw;
        }
        catch (Exception ex)  // Catch all other exceptions
        {
            Console.WriteLine($"[CS2 Surf] General error when connecting to the database: {ex.Message}");
            throw;  // Re-throw the exception without wrapping it
        }

        return connection;
    }

    /// <summary>
    /// Always encapsulate the block with `using` when calling this method.
    /// That way we ensure the proper disposal of the `MySqlDataReader` when we are finished with it.
    /// </summary>
    /// <param name="query">SELECT query to execute</param>
    public async Task<MySqlDataReader> QueryAsync(string query)
    {
        try
        {
            var connection = GetConnection();
            var cmd = new MySqlCommand(query, connection);
            return await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing query {query}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Automatically disposes of the connection and command are disposed of after usage.
    /// No need to encapsulate in `using` block.
    /// </summary>
    /// <param name="query">INSERT/UPDATE query to execute</param>
    /// <returns>rowsInserted, lastInsertedId</returns>
    public async Task<(int rowsInserted, long lastInsertedId)> WriteAsync(string query)
    {
        try
        {
            using var connection = GetConnection();
            using var cmd = new MySqlCommand(query, connection);
            var rows = await cmd.ExecuteNonQueryAsync();
            long lastId = cmd.LastInsertedId; // Retrieve the last ID inserted
            return (rows, lastId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing write operation {query}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Begins a transaction and executes it on the database.
    /// Used for inputting `Checkpoints` data after a run has been finished.
    /// No need to encapsulate in a `using` block, method disposes of connection and data itself.
    /// </summary>
    /// <param name="commands">INSERT/UPDATE queries to execute</param>
    public async Task TransactionAsync(List<string> commands)
    {
        // Create a new connection and open it
        using var connection = GetConnection();

        // Begin a transaction on the connection
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Execute each command within the transaction
            foreach (var commandText in commands)
            {
                using var cmd = new MySqlCommand(commandText, connection, transaction);
                await cmd.ExecuteNonQueryAsync();
            }

            // Commit the transaction
            await transaction.CommitAsync();
        }
        catch
        {
            // Roll back the transaction if an error occurs
            await transaction.RollbackAsync();
            throw;
        }
        // The connection and transaction are disposed here
    }
}

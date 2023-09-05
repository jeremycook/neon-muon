using Npgsql;

namespace NeonMS.DataAccess;

public static class ConnectionHelpers
{
    public static async Task<int> ExecuteNonQueryAsync(this NpgsqlConnection connection, string commandText, CancellationToken cancellationToken = default)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        try
        {
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(typeof(ConnectionHelpers), ex, "Error executing: {Sql}", cmd.CommandText);
            throw;
        }
    }

    public static async Task<int> ExecuteNonQueryWithErrorLoggingAsync(this NpgsqlCommand cmd, CancellationToken cancellationToken = default)
    {
        try
        {
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(typeof(ConnectionHelpers), ex, "Error executing: {Sql}", cmd.CommandText);
            throw;
        }
    }
}
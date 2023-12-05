using Npgsql;
using System.Text.Json;

namespace NeonMuon.DataAccess;

public static class ConnectionHelpers
{
    public static async Task<int> ExecuteNonQueryAsync(
        this NpgsqlConnection connection,
        string commandText,
        CancellationToken cancellationToken = default
    )
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

    public static async Task<int> ExecuteNonQueryWithErrorLoggingAsync(
        this NpgsqlCommand cmd,
        CancellationToken cancellationToken = default)
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

    public static async Task<List<T>> ListAsync<T>(
        this NpgsqlDataReader reader,
        CancellationToken cancellationToken
    )
    {
        var list = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            object?[] record = new object?[reader.FieldCount];
            reader.GetValues(record!);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (record[i] == DBNull.Value)
                {
                    record[i] = null;
                }
                else if (record[i] is string text && reader.GetDataTypeName(i) == "json")
                {
                    record[i] = JsonDocument.Parse(text);
                }
            }

            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            {
                T item = (T)record[0]!;
                list.Add(item);
            }
            else
            {
                T item = (T)Activator.CreateInstance(typeof(T), record)!;
                list.Add(item);
            }
        }

        return list;
    }

    public static async Task<List<object?[]>> ListAsync(
        this NpgsqlDataReader reader,
        CancellationToken cancellationToken
    )
    {
        var list = new List<object?[]>();
        while (await reader.ReadAsync(cancellationToken))
        {
            object?[] record = new object?[reader.FieldCount];
            reader.GetValues(record!);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (record[i] == DBNull.Value)
                {
                    record[i] = null;
                }
                else if (record[i] is string text && reader.GetDataTypeName(i) == "json")
                {
                    record[i] = JsonDocument.Parse(text);
                }
            }
            list.Add(record);
        }

        return list;
    }
}
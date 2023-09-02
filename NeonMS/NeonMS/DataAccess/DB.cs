using LinqToDB;
using LinqToDB.Data;
using NeonMS.Authentication;
using Npgsql;

namespace NeonMS.DataAccess;

public static class DB
{
    /// <summary>
    /// Configure during program startup.
    /// </summary>
    public static Dictionary<string, NpgsqlConnectionStringBuilder> Connections { get; set; } = null!;

    public static NpgsqlConnectionStringBuilder Build(ConnectionCredential credential, string? database)
    {
        var sourceConnection = Connections[credential.Connection];
        var builder = new NpgsqlConnectionStringBuilder();
        foreach (var conn in sourceConnection)
        {
            builder[conn.Key] = conn.Value;
        }
        builder.Username = credential.Username;
        builder.Password = credential.Password;
        if (database != null)
        {
            builder.Database = database;
        }

        return builder;
    }

    public static NpgsqlConnection Connection(ConnectionCredential credential, string? database = null)
    {
        NpgsqlConnectionStringBuilder builder = Build(credential, database);
        return new NpgsqlConnection(builder.ConnectionString);
    }

    public static DataConnection DataConnection(ConnectionCredential credential, string? database = null)
    {
        NpgsqlConnectionStringBuilder builder = Build(credential, database);
        return new DataConnection(ProviderName.PostgreSQL15, builder.ConnectionString);
    }

    public static async Task<DataConnection?> TryDataConnection(ConnectionCredential credential)
    {
        try
        {
            var dc = DataConnection(credential);

            var username = await dc.FromSqlScalar<string>($"select session_user").FirstOrDefaultAsync();
            if (username == credential.Username)
            {
                throw new InvalidOperationException();
            }

            return dc;
        }
        catch (Exception ex)
        {
            Log.Error(typeof(DB), ex, "Suppressed {ExceptionType}: {ExceptionMessage}", ex.GetBaseException().GetType(), ex.GetBaseException().Message);
            return null;
        }
    }
}

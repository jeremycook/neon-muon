using LinqToDB;
using LinqToDB.Data;
using NeonMS.Authentication;
using Npgsql;

namespace NeonMS.DataAccess;

public static class ConnectionFactory
{
    /// <summary>
    /// Configure during program startup.
    /// </summary>
    public static Dictionary<string, NpgsqlConnectionStringBuilder> Connections { get; set; } = null!;

    public static DataConnection DataConnection(ConnectionCredential credential)
    {
        var sourceConnection = Connections[credential.Connection];
        var copy = new NpgsqlConnectionStringBuilder();
        foreach (var conn in sourceConnection)
        {
            copy[conn.Key] = conn.Value;
        }
        copy.Username = credential.Username;
        copy.Password = credential.Password;

        return new DataConnection(ProviderName.PostgreSQL15, copy.ConnectionString);
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
            Log.Error(typeof(ConnectionFactory), ex, "Suppressed {ExceptionType}: {ExceptionMessage}", ex.GetBaseException().GetType(), ex.GetBaseException().Message);
            return null;
        }
    }
}

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

    public static DataConnection DataConnection(KeyValuePair<string, ConnectionCredential> credentials)
    {
        var sourceConnection = Connections[credentials.Key];
        var copy = new NpgsqlConnectionStringBuilder();
        foreach (var conn in sourceConnection)
        {
            copy[conn.Key] = conn.Value;
        }
        copy.Username = credentials.Value.Username;
        copy.Password = credentials.Value.Password;
        copy.Timeout = 5;
        //copy.RootCertificate = @"C:\Users\Jeremy\AppData\Roaming\postgresql\root.crt";
        //copy.SslMode = SslMode.VerifyCA;

        return new DataConnection(ProviderName.PostgreSQL15, copy.ConnectionString);
    }

    public static async Task<DataConnection?> TryDataConnection(KeyValuePair<string, ConnectionCredential> credentials)
    {
        try
        {
            var dc = DataConnection(credentials);

            var username = await dc.FromSqlScalar<string>($"select session_user").FirstOrDefaultAsync();
            if (username == credentials.Value.Username)
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

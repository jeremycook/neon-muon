using LinqToDB;
using LinqToDB.Data;
using Npgsql;

namespace NeonMS.DataAccess;

public static class DB
{
    static DB()
    {
        AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);
    }

    public static string DirectoryDatabase { get; set; } = "postgres";
    public static Dictionary<string, DataServer> Servers { get; set; } = null!;
    public static Dictionary<string, DataCredential> MasterCredentials { get; set; } = null!;

    public static async Task<NpgsqlConnection> MaintenanceConnection(CancellationToken cancellationToken = default)
    {
        return await MaintenanceConnection("Main", cancellationToken);
    }

    private static async Task<NpgsqlConnection> MaintenanceConnection(string name, CancellationToken cancellationToken = default)
    {
        var server = Servers[name];
        var credential = MasterCredentials[name];
        var connectionString = GetConnectionString(server, credential, DirectoryDatabase);

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        if (credential.Role != null)
        {
            await connection.ExecuteNonQueryAsync($"SET role TO {Quote.Identifier(credential.Role)}", cancellationToken: cancellationToken);
        }
        return connection;
    }

    public static async Task<DataConnection> OpenDataConnection(DataCredential credential, string database, CancellationToken cancellationToken = default)
    {
        var server = Servers["Main"];
        var connectionString = GetConnectionString(server, credential, database);

        var dataConnection = new DataConnection(ProviderName.PostgreSQL15, connectionString);
        if (credential.Role != null)
        {
            await dataConnection.ExecuteAsync($"SET role TO {Quote.Identifier(credential.Role)}", cancellationToken);
        }
        return dataConnection;
    }

    public static async Task<NpgsqlConnection> OpenConnection(DataCredential credential, string database, CancellationToken cancellationToken = default)
    {
        return await OpenConnection(Servers["Main"], credential, database, cancellationToken);
    }

    private static async Task<NpgsqlConnection> OpenConnection(DataServer server, DataCredential credential, string database, CancellationToken cancellationToken = default)
    {
        var connectionString = GetConnectionString(server, credential, database);

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        if (credential.Role != null)
        {
            await connection.ExecuteNonQueryAsync($"SET role TO {Quote.Identifier(credential.Role)}", cancellationToken);
        }
        return connection;
    }

    public static string GetConnectionString(DataCredential credential, string database)
    {
        return GetConnectionString(Servers["Main"], credential, database);
    }

    private static string GetConnectionString(DataServer server, DataCredential credential, string database)
    {
        // TODO: LRU cache

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = server.Host,
            Port = server.Port,
            MaxAutoPrepare = server.MaxAutoPrepare,
            IncludeErrorDetail = server.IncludeErrorDetail,
            CommandTimeout = server.CommandTimeout,
            Timeout = server.Timeout,
            Timezone = server.Timezone,

            Username = credential.Username,
            Password = credential.Password,

            Database = database,
        };

        return builder.ToString();
    }

    public static async Task<bool> IsValid(DataCredential credential, string database, CancellationToken cancellationToken = default)
    {
        return await IsValid(Servers["Main"], credential, database, cancellationToken);
    }

    private static async Task<bool> IsValid(DataServer server, DataCredential credential, string database, CancellationToken cancellationToken = default)
    {
        try
        {
            using var con = await OpenConnection(server, credential, database, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(typeof(DB), ex, "Suppressed {ExceptionType}: {ExceptionMessage}", ex.GetBaseException().GetType(), ex.GetBaseException().Message);
            throw;
        }
    }
}

public class DataServer
{
    private string? _Host;

    public string Host { get => _Host ?? throw new NullReferenceException("The Host has not been set."); set => _Host = value; }
    public int Port { get; set; } = 5432;

    public int CommandTimeout { get; set; } = 10;
    public int Timeout { get; set; } = 5;
    public int MaxAutoPrepare { get; set; } = 0;
    public bool IncludeErrorDetail { get; set; } = false;
    public string Timezone { get; set; } = "UTC";

    public override string ToString()
    {
        return string.Join("; ", typeof(DataServer).GetProperties().Select(x => x.Name + "=" + x.GetValue(this)));
    }
}

public class DataCredential
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Role { get; set; }
}
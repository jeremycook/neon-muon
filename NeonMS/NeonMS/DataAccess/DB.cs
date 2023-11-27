﻿using LinqToDB;
using Npgsql;

namespace NeonMS.DataAccess;

public static class DB
{
    static DB()
    {
        AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);
    }

    public static Dictionary<string, DataServer> Servers { get; } = [];
    public static Dictionary<string, MaintenanceCredential> MaintenanceCredentials { get; } = [];

    public static async Task<NpgsqlConnection> MaintenanceConnection(string dataServer, CancellationToken cancellationToken = default)
    {
        var server = Servers[dataServer];
        var credential = MaintenanceCredentials[dataServer];
        var connectionString = GetConnectionString(server, credential, credential.MaintenanceDatabase);

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(credential.Role))
        {
            await connection.ExecuteNonQueryAsync($"SET role TO {Quote.Identifier(credential.Role)}", cancellationToken: cancellationToken);
        }
        return connection;
    }

    public static async Task<NpgsqlConnection> OpenConnection(DataCredential credential, string database, CancellationToken cancellationToken = default)
    {
        return await OpenConnection(Servers[credential.Server], credential, database, cancellationToken);
    }

    private static async Task<NpgsqlConnection> OpenConnection(DataServer server, DataCredential credential, string database, CancellationToken cancellationToken = default)
    {
        var connectionString = GetConnectionString(server, credential, database);

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(credential.Role))
        {
            await connection.ExecuteNonQueryAsync($"SET role TO {Quote.Identifier(credential.Role)}", cancellationToken);
        }
        return connection;
    }

    public static string GetConnectionString(DataCredential credential, string database)
    {
        return GetConnectionString(Servers[credential.Server], credential, database);
    }

    private static string GetConnectionString(DataServer server, DataCredential credential, string database)
    {
        // TODO: LRU cache

        var maintenanceCredential = credential as MaintenanceCredential;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = maintenanceCredential?.MaintenanceHost ?? server.Host,
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

    public static async Task<bool> IsValid(DataCredential credential, CancellationToken cancellationToken = default)
    {
        if (!Servers.TryGetValue(credential.Server, out DataServer? server))
        {
            Log.Warn(typeof(DB), "Invalid {DataServer}", credential.Server);
            return false;
        }

        return await IsValid(server, credential, MaintenanceCredentials[credential.Server].MaintenanceDatabase, cancellationToken);
    }

    private static async Task<bool> IsValid(DataServer server, DataCredential credential, string database, CancellationToken cancellationToken = default)
    {
        try
        {
            using var con = await OpenConnection(server, credential, database, cancellationToken);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == "28P01")
        {
            Log.Info(typeof(DB), ex, "Invalid credentials {ExceptionType}: {ExceptionMessage}", ex.GetType(), ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Log.SuppressedError(typeof(DB), ex);
            return false;
        }
    }
}

public class DataServer
{
    public float TokenLifetimeHours { get; set; } = 1;

    public required string Host { get => _Host ?? throw new NullReferenceException("The Host has not been set."); set => _Host = value; }
    public int Port { get; set; } = 5432;

    public int CommandTimeout { get; set; } = 10;
    public int Timeout { get; set; } = 5;
    public int MaxAutoPrepare { get; set; } = 0;
    public bool IncludeErrorDetail { get; set; } = false;
    public string Timezone { get; set; } = "UTC";

    private string? _Host;

    public override string ToString()
    {
        return string.Join("; ", typeof(DataServer).GetProperties().Select(x => x.Name + "=" + x.GetValue(this)));
    }
}

public class DataCredential
{
    public required string Server { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; }
}

public class MaintenanceCredential : DataCredential
{
    public string? MaintenanceHost { get; set; }
    public required string MaintenanceDatabase { get; set; }
}
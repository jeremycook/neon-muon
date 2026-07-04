using Microsoft.Data.Sqlite;
using Sqlil.Core.Db;
using Sqlil.Core.Syntax;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace Sqlil.Sqlite;

public static class SqliteConnectionExtensions {

    public static SqliteConnection CreateInMemory(string? name = null) {
        name ??= Guid.NewGuid().ToString("N");
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = name,
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ConnectionString);
        return connection;
    }

    public static SqliteConnection OpenInMemory(string? name = null) {
        var connection = CreateInMemory(name);
        connection.Open();
        return connection;
    }

    public static List<T> List<T>(this SqliteConnection connection, Expression<Func<IQueryable<T>>> query) {
        return ((DbConnection)connection).List(query);
    }

    public static Task<List<T>> List<T>(this SqliteConnection connection, Expression<Func<IQueryable<T>>> query, CancellationToken cancellationToken) {
        return ((DbConnection)connection).List(query, cancellationToken);
    }

    public static T? Nullable<T>(this SqliteConnection connection, Expression<Func<IQueryable<T>>> query)
        where T : struct {
        return ((DbConnection)connection).Nullable(query);
    }

    public static ValueTask<T?> Nullable<T>(this SqliteConnection connection, Expression<Func<IQueryable<T>>> query, CancellationToken cancellationToken)
        where T : struct {
        return ((DbConnection)connection).Nullable(query, cancellationToken);
    }

    public static int Execute<T>(this SqliteConnection connection, Expression<Func<IQueryable<T>>> query)
        where T : struct {
        return ((DbConnection)connection).Execute(query);
    }

    public static ValueTask<int> Execute<T>(this SqliteConnection connection, Expression<Func<IQueryable<T>>> query, CancellationToken cancellationToken)
        where T : struct {
        return ((DbConnection)connection).Execute(query, cancellationToken);
    }

    public static (DbCommand Command, StableList<SqlColumn> SqlColumns) CreateCommand(this SqliteConnection connection, LambdaExpression expression) {
        return ((DbConnection)connection).CreateCommand(expression);
    }

    public static void ExecuteNonQuery(this SqliteConnection connection, string commandText) {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        cmd.ExecuteNonQuery();
    }

    public static async Task ExecuteNonQueryAsync(this SqliteConnection connection, string commandText, CancellationToken cancellationToken = default) {
        await connection.OpenAsync(cancellationToken);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

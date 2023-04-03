using DataCore;
using Microsoft.Data.Sqlite;

namespace DataMod.Sqlite;

public class SqliteQueryRunner<TDb> : IQueryRunner<TDb> {
    private readonly SqliteCommandComposer<TDb> composer;
    private readonly IDbConnectionPool<TDb, SqliteConnection> connectionFactory;

    public SqliteQueryRunner(SqliteCommandComposer<TDb> composer, IDbConnectionPool<TDb, SqliteConnection> connectionFactory) {
        this.composer = composer;
        this.connectionFactory = connectionFactory;
    }

    public int Execute(IQuery<TDb> query) {
        using var accessor = connectionFactory.Create();
        using var command = composer.CreateCommand(query);
        return accessor.Connection.Execute(command);
    }

    public async ValueTask<int> Execute(IQuery<TDb> query, CancellationToken cancellationToken = default) {
        using var accessor = connectionFactory.Create();
        using var command = composer.CreateCommand(query);
        return await accessor.Connection.ExecuteAsync(command, cancellationToken);
    }

    public async ValueTask<List<T1>> List<T1>(IQuery<TDb, T1> query, CancellationToken cancellationToken = default) {
        using var accessor = connectionFactory.Create();
        using var command = composer.CreateCommand(query);

        var list = await accessor.Connection.ListAsync<T1>(command, cancellationToken);

        return list;
    }

    public async ValueTask<T1?> Nullable<T1>(IQuery<TDb, T1> query, CancellationToken cancellationToken = default)
        where T1 : struct {
        var list = await List(query, cancellationToken);

        return list.Any()
            ? list.Single()
            : null;
    }
}

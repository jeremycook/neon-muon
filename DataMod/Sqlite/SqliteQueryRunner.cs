using DataCore;
using Microsoft.Data.Sqlite;
using SqliteMod;

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

        try {
            return accessor.Connection.Execute(command);
        }
        catch (Exception ex) {
            throw new Exception(ex.GetBaseException().Message + "\n" + command.CommandText, ex);
        }
    }

    public ValueTask<int> Execute(IQuery<TDb> query, CancellationToken cancellationToken = default) {
        using var accessor = connectionFactory.Create();
        using var command = composer.CreateCommand(query);

        try {
            return ValueTask.FromResult(accessor.Connection.Execute(command));
        }
        catch (Exception ex) {
            throw new Exception(ex.GetBaseException().Message + "\n" + command.CommandText, ex);
        }
    }

    public ValueTask<List<T1>> List<T1>(IQuery<TDb, T1> query, CancellationToken cancellationToken = default) {
        using var accessor = connectionFactory.Create();
        using var command = composer.CreateCommand(query);

        try {
            var list = accessor.Connection.List<T1>(command);
            return ValueTask.FromResult(list);
        }
        catch (Exception ex) {
            throw new Exception(ex.GetBaseException().Message + "\n" + command.CommandText, ex);
        }
    }

    public async ValueTask<T1?> Nullable<T1>(IQuery<TDb, T1> query, CancellationToken cancellationToken = default)
        where T1 : struct {
        var list = await List(query, cancellationToken);

        return list.Any()
            ? list.Single()
            : null;
    }
}

using DataCore;
using Microsoft.Data.Sqlite;

namespace DataMod.Sqlite;

public class SqliteProduceQueryCommand<TDb, TItem> : IQueryCommand<List<TItem>> {
    private readonly Sql sql;
    private readonly IDbConnectionFactory<TDb> connectionFactory;

    public SqliteProduceQueryCommand(Sql sql, IDbConnectionFactory<TDb> connectionFactory) {
        this.sql = sql;
        this.connectionFactory = connectionFactory;
    }

    public List<TItem>? Response { get; private set; }

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken) {
        using var connection = connectionFactory.Get<SqliteConnection>();
        await connection.OpenAsync(cancellationToken);
        try {
            Response = await connection.ListAsync<TItem>(sql, cancellationToken);
        }
        catch {
            throw;
        }
    }
}

using DataCore;
using Microsoft.Data.Sqlite;

namespace DataMod.Sqlite;

public class SqliteProduceQueryCommand<TDb, TItem> : IQueryCommand<List<TItem>> {
    private readonly Sql sql;
    private readonly IDbConnectionString<TDb> connectionString;

    public SqliteProduceQueryCommand(Sql sql, IDbConnectionString<TDb> connectionString) {
        this.sql = sql;
        this.connectionString = connectionString;
    }

    public List<TItem>? Response { get; private set; }

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken) {
        using var connection = new SqliteConnection(connectionString.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        Response = await connection.ListAsync<TItem>(sql, cancellationToken);
    }
}

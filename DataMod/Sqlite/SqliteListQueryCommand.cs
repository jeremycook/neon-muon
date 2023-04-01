using DataCore;
using Microsoft.Data.Sqlite;

namespace DataMod.Sqlite;

public class SqliteListQueryCommand<TDb, TItem> : IQueryCommand {
    private readonly Sql sql;
    private readonly IDbConnectionFactory<TDb> connectionFactory;

    public SqliteListQueryCommand(Sql sql, IDbConnectionFactory<TDb> connectionFactory) {
        this.sql = sql;
        this.connectionFactory = connectionFactory;
    }

    private List<TItem>? _response;
    public object? Response => _response ?? throw new InvalidOperationException("The Response has not been set.");

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken) {
        var connection = connectionFactory.Get<SqliteConnection>();
        await connection.OpenAsync(cancellationToken);
        try {
            _response = await connection.ListAsync<TItem>(sql, cancellationToken);
        }
        catch {
            throw;
        }
    }
}

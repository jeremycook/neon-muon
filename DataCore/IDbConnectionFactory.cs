using System.Data.Common;

namespace DataCore;

public interface IDbConnectionPool<TDb, TDbConnection> where TDbConnection : DbConnection {
    PooledDbConnection<TDbConnection> Create();
}

public class StaticDbConnectionPool<TDb, TDbConnection> : IDbConnectionPool<TDb, TDbConnection> where TDbConnection : DbConnection {
    private readonly TDbConnection connection;

    public StaticDbConnectionPool(TDbConnection connection) {
        this.connection = connection;
    }

    public PooledDbConnection<TDbConnection> Create() => new(connection, disposeConnection: false);
}

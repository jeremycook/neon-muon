using System.Data.Common;

namespace DataCore;

public interface IDbConnectionFactory<TDb> {
    TDbConnection Get<TDbConnection>()
        where TDbConnection : DbConnection;
}

public class StaticDbConnectionFactory<TDb> : IDbConnectionFactory<TDb> {
    public StaticDbConnectionFactory(DbConnection connection) {
        Connection = connection;
    }

    public DbConnection Connection { get; }

    public TDbConnection Get<TDbConnection>()
        where TDbConnection : DbConnection {
        return (TDbConnection)Connection;
    }
}

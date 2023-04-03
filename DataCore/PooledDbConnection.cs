using System.Data.Common;

namespace DataCore;

public class PooledDbConnection<TDbConnection> : IDisposable where TDbConnection : DbConnection {
    private readonly TDbConnection connection;
    private readonly bool disposeConnection;
    private bool disposedValue;

    public TDbConnection Connection {
        get {
            if (connection.State != System.Data.ConnectionState.Open) {
                connection.Open();
            }
            return connection;
        }
    }

    public PooledDbConnection(TDbConnection connection, bool disposeConnection) {
        this.connection = connection;
        this.disposeConnection = disposeConnection;
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                if (disposeConnection) {
                    connection.Dispose();
                }
            }
            disposedValue = true;
        }
    }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
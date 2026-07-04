using Microsoft.Data.Sqlite;

namespace Sqlil.Sqlite.Tests;

public class SqliteConnectionExtensionsTests {

    [Fact]
    public void CreateInMemory_ReturnsOpenableConnection() {
        using var connection = SqliteConnectionExtensions.CreateInMemory();
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
        connection.Open();
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public void OpenInMemory_ReturnsOpenConnection() {
        using var connection = SqliteConnectionExtensions.OpenInMemory();
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public void OpenInMemory_SameName_ReturnsSameDatabase() {
        var name = Guid.NewGuid().ToString("N");
        using var conn1 = SqliteConnectionExtensions.OpenInMemory(name);
        using var conn2 = SqliteConnectionExtensions.OpenInMemory(name);

        using (var cmd = conn1.CreateCommand()) {
            cmd.CommandText = "CREATE TABLE test (id INTEGER PRIMARY KEY)";
            cmd.ExecuteNonQuery();
        }

        using var cmd2 = conn2.CreateCommand();
        cmd2.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='test'";
        var result = cmd2.ExecuteScalar();
        Assert.Equal("test", result);
    }

    [Fact]
    public void ExecuteNonQuery_RunsDdl() {
        using var connection = SqliteConnectionExtensions.OpenInMemory();
        connection.ExecuteNonQuery("CREATE TABLE test (id INTEGER PRIMARY KEY, name TEXT)");

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='test'";
        Assert.Equal("test", cmd.ExecuteScalar());
    }

    [Fact]
    public void ExecuteNonQuery_RunsInsert() {
        using var connection = SqliteConnectionExtensions.OpenInMemory();
        connection.ExecuteNonQuery("CREATE TABLE test (id INTEGER PRIMARY KEY, name TEXT)");
        connection.ExecuteNonQuery("INSERT INTO test (name) VALUES ('hello')");

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test";
        Assert.Equal("hello", cmd.ExecuteScalar());
    }
}

using System.Data.Common;

namespace DataCore;

public interface IDbConnectionString<TDb>
{
    string ConnectionString { get; }
}

public class DbConnectionString<TDb> : IDbConnectionString<TDb>
{
    private readonly DbConnectionStringBuilder dbConnectionStringBuilder;

    public DbConnectionString(DbConnectionStringBuilder dbConnectionStringBuilder)
    {
        this.dbConnectionStringBuilder = dbConnectionStringBuilder;
    }

    public string ConnectionString => dbConnectionStringBuilder.ConnectionString;
}

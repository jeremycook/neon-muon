using DatabaseMod.Models;
using DataCore;
using static DataCore.Sql;

namespace DataMod.Sqlite;

public class SqliteQueryComposer<TDb> : IQueryComposer<TDb> {
    public SqliteQueryComposer(
        IDbConnectionFactory<TDb> connectionFactory,
        IReadOnlyDatabase<TDb> database) {
        ConnectionFactory = connectionFactory;
        Database = database;
    }

    private readonly SqlExpressionTranslator expressionTranslator = new();
    public IDbConnectionFactory<TDb> ConnectionFactory { get; }
    public IReadOnlyDatabase<TDb> Database { get; }

    public IReadOnlyCollection<IQueryCommand> Compose(IQuery<TDb> query) {
        return StartVisit(query).ToArray();
    }

    private IEnumerable<IQueryCommand> StartVisit(IQuery<TDb> query) {
        switch (query.QueryType.Name) {

            case QueryType.Produce:
                var produceResult = Produce(query as dynamic);
                foreach (var queryCommand in produceResult) {
                    yield return queryCommand;
                }
                break;

            default:
                var result = Produce(query as dynamic);
                foreach (var queryCommand in result) {
                    yield return queryCommand;
                }
                break;
        }
    }

    private Sql From<T1>(FromQuery<TDb, T1> query) {
        var sql = Interpolate($"FROM {Table(query)}");
        return sql;
    }

    private static Sql Table<T1>(FromQuery<TDb, T1> query) {
        return Identifier(query.T1Type.Name);
    }

    private Sql Filter<T1>(FilterQuery<TDb, T1> query) {
        if (query.Query is FromQuery<TDb, T1> fromQuery) {
            var condition = expressionTranslator.Translate(query.Condition);
            var sql = Interpolate($"{From(fromQuery)} WHERE {condition}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unable to filter: {query}");
        }
    }

    private Sql Take<T1>(TakeQuery<TDb, T1> query) {
        if (query.Query is FromQuery<TDb, T1> fromQuery) {
            var sql = Interpolate($"{From(fromQuery)} LIMIT {Raw(query.Take.ToString())}");
            return sql;
        }
        else if (query.Query is FilterQuery<TDb, T1> filterQuery) {
            var sql = Interpolate($"{Filter(filterQuery)} LIMIT {Raw(query.Take.ToString())}");
            return sql;
        }
        else if (query.Query.QueryType == QueryType.Map) {
            var sql = Interpolate($"{Map(query.Query as dynamic)} LIMIT {Raw(query.Take.ToString())}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unable to take: {query}");
        }
    }

    private Sql Map<T1, TMapped>(MapQuery<TDb, T1, TMapped> query) {
        if (query.Query is FromQuery<TDb, T1> fromQuery) {
            var sql = Interpolate($"SELECT * {From(fromQuery)}");
            return sql;
        }
        if (query.Query is FilterQuery<TDb, T1> filterQuery) {
            var sql = Interpolate($"SELECT * {Filter(filterQuery)}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unable to map: {query.Query}");
        }
    }

    private IEnumerable<IQueryCommand> Produce<T1>(ProduceQuery<TDb, T1> produceQuery) {
        return Produce(produceQuery.Query);
    }

    private Sql InsertQuery<T1>(InsertQuery<TDb, T1> query) {
        if (query.Query is FromQuery<TDb, T1> fromQuery) {
            var sql = Interpolate($"INSERT INTO {Table(fromQuery)} ({Columns(fromQuery)}) VALUES {Join(", ", query.Items.Select(Values))}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unable to insert with: {query}");
        }
    }

    private Sql Columns<T1>(FromQuery<TDb, T1> _) {
        return IdentifierList(typeof(T1).GetProperties().Select(p => p.Name));
    }

    private Sql Values<T1>(T1 item) {
        return Interpolate($"({Join(", ", typeof(T1).GetProperties().Select(p => p.GetValue(item)))})");
    }

    private IEnumerable<IQueryCommand> Produce<T1>(IQuery<TDb, T1> query) {
        // DQL
        if (query is FromQuery<TDb, T1> fromQuery) {
            var sql = Interpolate($"SELECT * {From(fromQuery)}");
            var command = new SqliteListQueryCommand<TDb, T1>(sql, ConnectionFactory);
            yield return command;
        }
        else if (query is FilterQuery<TDb, T1> filterQuery) {
            var sql = Interpolate($"SELECT * {Filter(filterQuery)}");
            var command = new SqliteListQueryCommand<TDb, T1>(sql, ConnectionFactory);
            yield return command;
        }
        else if (query.QueryType == QueryType.Take) {
            var sql = Interpolate($"SELECT * {Take(query as dynamic)}");
            var command = new SqliteListQueryCommand<TDb, T1>(sql, ConnectionFactory);
            yield return command;
        }
        else if (query.QueryType == QueryType.Map) {
            Sql sql = Map(query as dynamic);
            var command = new SqliteListQueryCommand<TDb, T1>(sql, ConnectionFactory);
            yield return command;
        }

        // DML
        else if (query is InsertQuery<TDb, T1> insertQuery) {
            Sql sql = InsertQuery(insertQuery);
            var command = new SqliteExecuteNonQueryCommand<TDb>(sql, ConnectionFactory);
            yield return command;
        }

        else {
            throw new NotSupportedException($"Unsupported query: {query}");
        }
    }
}

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

    public IReadOnlyCollection<IQueryCommand<object>> Compose(IQuery<TDb> query) {
        return StartVisit(query).ToArray();
    }

    private IEnumerable<IQueryCommand<object>> StartVisit(IQuery<TDb> query) {
        switch (query.Type.Name) {

            case QueryType.Produce:
                var dynamicResult = Produce(query as dynamic);
                var enumerable = (IEnumerable<IQueryCommand<object>>)dynamicResult;
                foreach (var queryCommand in enumerable) {
                    yield return queryCommand;
                }
                break;

            case QueryType.From:
                // A From without a Produce does not do anything
                // TODO: Throw instead?
                yield break;

            default:
                throw new NotSupportedException($"Unsupported query: {query}");
        }
    }

    private Sql From<T1>(FromQuery<TDb, T1> query) {
        var sql = Interpolate($"FROM {Identifier(query.T1Type.Name)}");
        return sql;
    }

    private Sql Filter<T1>(FilterQuery<TDb, T1> query) {
        if (query.Query is FromQuery<TDb, T1> fromQuery) {
            var condition = expressionTranslator.Translate(query.Condition);
            var sql = Interpolate($"{From(fromQuery)} WHERE {condition}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unsupported query: {query}");
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
            throw new NotSupportedException($"Unsupported query: {query}");
        }
    }

    private IEnumerable<IQueryCommand<List<T1>>> Produce<T1>(ProduceQuery<TDb, T1> query) {
        if (query.Query is FromQuery<TDb, T1> fromQuery) {
            var sql = Interpolate($"SELECT * {From(fromQuery)}");
            var command = new SqliteProduceQueryCommand<TDb, T1>(sql, ConnectionFactory);
            yield return command;
        }
        else if (query.Query is FilterQuery<TDb, T1> filterQuery) {
            var sql = Interpolate($"SELECT * {Filter(filterQuery)}");
            var command = new SqliteProduceQueryCommand<TDb, T1>(sql, ConnectionFactory);
            yield return command;
        }
        else if (query.Query.Type == QueryType.Map) {
            dynamic mapQuery = query.Query;
            Sql sql = Map(mapQuery);
            var command = new SqliteProduceQueryCommand<TDb, T1>(sql, ConnectionFactory);
            yield return command;
        }
        else {
            throw new NotSupportedException($"Unsupported query: {query}");
        }
    }
}

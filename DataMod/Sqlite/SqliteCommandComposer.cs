using DatabaseMod.Models;
using DataCore;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Reflection;
using static DataCore.Sql;

namespace DataMod.Sqlite;

public class SqliteCommandComposer<TDb> : IDbCommandComposer<TDb> {

    private static readonly Dictionary<string, MethodInfo> StaticMethods = typeof(SqliteCommandComposer<TDb>).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToDictionary(o => o.Name);

    public SqliteCommandComposer(IReadOnlyDatabase<TDb> database) {
        this.database = database;
    }

    private static readonly SqlExpressionTranslator expressionTranslator = new();
    private readonly IReadOnlyDatabase<TDb> database;

    public SqliteCommand CreateCommand(IQuery<TDb> query) {
        Sql sql = query.QueryType switch {
            QueryType.Map => CallMap(query),
            _ => Translate(query as dynamic),
        };

        var (commandText, parameterValues) = sql.ParameterizeSql();

        var command = new SqliteCommand(commandText);
        command.Parameters.AddRange(parameterValues);

        return command;
    }

    DbCommand IDbCommandComposer<TDb>.CreateCommand(IQuery<TDb> query) {
        return CreateCommand(query);
    }

    private static Sql CallMap(IQuery<TDb> mapQuery) {
        var genericMethod = StaticMethods[nameof(Map)];
        var typeArgs = mapQuery.GetType().GetGenericArguments();
        var method = genericMethod.MakeGenericMethod(typeArgs[1], typeArgs[2]);

        var result = method.Invoke(null, new[] { mapQuery });
        return (Sql)result!;
    }

    private static Sql Translate<T>(IQuery<TDb, T> query) {
        Sql sql;

        // DQL
        if (query is FromQuery<TDb, T> fromQuery) {
            sql = Map(MapQuery.CreateIdentityMap(fromQuery));
        }
        else if (query is FilterQuery<TDb, T> filterQuery) {
            sql = Map(MapQuery.CreateIdentityMap(filterQuery));
        }
        else if (query.QueryType == QueryType.Take) {
            sql = Map(MapQuery.CreateIdentityMap(query));
        }
        else if (query.QueryType == QueryType.Map) {
            sql = Map(query as dynamic);
        }

        // DML
        else if (query is InsertQuery<TDb, T> insertQuery) {
            sql = InsertQuery(insertQuery);
        }

        else {
            throw new NotSupportedException($"Unsupported query: {query}");
        }

        return sql;
    }

    private static Sql From<T>(FromQuery<TDb, T> query) {
        var sql = Interpolate($"FROM {Table(query)}");
        return sql;
    }

    private static Sql Table<T>(FromQuery<TDb, T> query) {
        return Identifier(query.T1Type.Name);
    }

    private static Sql Filter<T>(FilterQuery<TDb, T> query) {
        if (query.Query is FromQuery<TDb, T> fromQuery) {
            var condition = expressionTranslator.Translate(query.Condition);
            var sql = Interpolate($"{From(fromQuery)} WHERE {condition}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unable to filter: {query}");
        }
    }

    private static Sql Take<T>(TakeQuery<TDb, T> query) {
        if (query.Query is FromQuery<TDb, T> fromQuery) {
            var sql = Interpolate($"{From(fromQuery)} LIMIT {Raw(query.Take.ToString())}");
            return sql;
        }
        else if (query.Query is FilterQuery<TDb, T> filterQuery) {
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

    private static Sql Map<TSource, T>(MapQuery<TDb, TSource, T> query) {
        var mapSql = expressionTranslator.Translate(query.Map);
        if (query.Query is FromQuery<TDb, TSource> fromQuery) {
            var sql = Interpolate($"SELECT {mapSql} {From(fromQuery)}");
            return sql;
        }
        if (query.Query is FilterQuery<TDb, TSource> filterQuery) {
            var sql = Interpolate($"SELECT {mapSql} {Filter(filterQuery)}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unable to map: {query.Query}");
        }
    }

    private static Sql InsertQuery<T>(InsertQuery<TDb, T> query) {
        if (query.Query is FromQuery<TDb, T> fromQuery) {
            var sql = Interpolate($"INSERT INTO {Table(fromQuery)} ({Columns(fromQuery)}) VALUES {Join(", ", query.Items.Select(Values))}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unable to insert with: {query}");
        }
    }

    private static Sql Columns<T>(FromQuery<TDb, T> _) {
        return IdentifierList(typeof(T).GetProperties().Select(p => p.Name));
    }

    private static Sql Values<T>(T item) {
        return Interpolate($"({Join(", ", typeof(T).GetProperties().Select(p => p.GetValue(item)))})");
    }
}

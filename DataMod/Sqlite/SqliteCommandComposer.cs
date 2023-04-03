using DatabaseMod.Models;
using DataCore;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using static DataCore.Sql;

namespace DataMod.Sqlite;

public class SqliteCommandComposer<TDb> : IDbCommandComposer<TDb> {
    public SqliteCommandComposer(IReadOnlyDatabase<TDb> database) {
        this.database = database;
    }

    private static readonly SqlExpressionTranslator expressionTranslator = new();
    private readonly IReadOnlyDatabase<TDb> database;

    public SqliteCommand CreateCommand(IQuery<TDb> query) {
        var sql = (Sql)Translate(query as dynamic);

        var (commandText, parameterValues) = sql.ParameterizeSql();

        var command = new SqliteCommand(commandText);
        command.Parameters.AddRange(parameterValues);

        return command;
    }

    DbCommand IDbCommandComposer<TDb>.CreateCommand(IQuery<TDb> query) {
        return CreateCommand(query);
    }

    private Sql Translate<T1>(IQuery<TDb, T1> query) {
        Sql sql;

        // DQL
        if (query is FromQuery<TDb, T1> fromQuery) {
            sql = Map(MapQuery.CreateIdentityMap(fromQuery));
        }
        else if (query is FilterQuery<TDb, T1> filterQuery) {
            sql = Map(MapQuery.CreateIdentityMap(filterQuery));
        }
        else if (query.QueryType == QueryType.Take) {
            sql = Map(MapQuery.CreateIdentityMap(query));
        }
        else if (query.QueryType == QueryType.Map) {
            sql = Map(query as dynamic);
        }

        // DML
        else if (query is InsertQuery<TDb, T1> insertQuery) {
            sql = InsertQuery(insertQuery);
        }

        else {
            throw new NotSupportedException($"Unsupported query: {query}");
        }

        return sql;
    }

    private Sql Translate<T1, TMapped>(MapQuery<TDb, T1, TMapped> query) => Map(query);

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
        var mapSql = expressionTranslator.Translate(query.Map);
        if (query.Query is FromQuery<TDb, T1> fromQuery) {
            var sql = Interpolate($"SELECT {mapSql} {From(fromQuery)}");
            return sql;
        }
        if (query.Query is FilterQuery<TDb, T1> filterQuery) {
            var sql = Interpolate($"SELECT {mapSql} {Filter(filterQuery)}");
            return sql;
        }
        else {
            throw new NotSupportedException($"Unable to map: {query.Query}");
        }
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
}

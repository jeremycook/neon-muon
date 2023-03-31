using DatabaseMod.Models;
using DataCore;
using System.Collections.Generic;
using System.Reflection;

namespace DataMod.Sqlite;

public class SqliteQueryOrchestrator<TDb> : IQueryOrchestrator<TDb> {
    public SqliteQueryOrchestrator(
        IDbConnectionString<TDb> connectionString,
        IReadOnlyDatabase<TDb> database) {
        ConnectionString = connectionString;
        Database = database;
    }

    public IDbConnectionString<TDb> ConnectionString { get; }
    public IReadOnlyDatabase<TDb> Database { get; }

    public IReadOnlyCollection<IQueryCommand<object>> Compose<T1>(IQuery<TDb, T1> query) {
        return StartVisit(query).ToArray();
    }

    private IEnumerable<IQueryCommand<object>> StartVisit(object query) {
        var type = query.GetType();
        var typeDef = type.GetGenericTypeDefinition();
        var typeArgs = type.GetGenericArguments();

        if (typeDef == typeof(ProduceQuery<,>)) {
            var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == nameof(Produce));
            var genericMethod = method.MakeGenericMethod(typeArgs[1]);
            var result = genericMethod.Invoke(this, new[] { query })!;
            var enumerable = (IEnumerable<IQueryCommand<object>>)result;
            foreach (var queryCommand in enumerable) {
                yield return queryCommand;
            }
        }

        else if (typeDef == typeof(FromQuery<,>)) {
            // A From with a Produce does not do anything
            // TODO: Throw instead?
            yield break;
        }

        else {
            throw new NotSupportedException(type.AssemblyQualifiedName ?? type.Name);
        }
    }

    private Sql From<T>(FromQuery<TDb, T> query) {
        var sql = Sql.Interpolate($"SELECT * FROM {Sql.Identifier(query.Type.Name)}");
        return sql;
    }

    private IEnumerable<IQueryCommand<List<T1>>> Produce<T1>(ProduceQuery<TDb, T1> query) {
        if (query.Query is FromQuery<TDb, T1> fromQuery) {
            var sql = From(fromQuery);
            var command = new SqliteProduceQueryCommand<TDb, T1>(sql, ConnectionString);
            yield return command;
        }
        else {
            throw new NotSupportedException(query.ToString());
        }
    }
}

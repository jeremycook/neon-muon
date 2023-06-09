using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace Sqlil.Core.Db;

public static class DbConnectionExtensions {

    private static SelectStmtTranslator SelectStmtTranslator { get; } = new();

    private static SelectStmt TranslateToSelectStmt(LambdaExpression expression) {
        object translation = SelectStmtTranslator.Translate(expression, default);
        var result = translation switch {
            SelectStmt selectStmt => selectStmt,
            SelectCore selectCore => SelectStmt.Create(selectCore),
            _ => throw new NotImplementedException(translation.GetType().ToString()),
        };
        return result;
    }

    public static List<T> List<T>(this DbConnection dbConnection, Expression<Func<IQueryable<T>>> query) {
        var (cmd, sqlColumns) = dbConnection.CreateCommand(query);

        dbConnection.Open();
        using var reader = cmd.ExecuteReader();

        var records = new List<object?[]>();
        while (reader.Read()) {

            var values = new object?[sqlColumns.Count];
            reader.GetValues(values!);

            for (int i = 0; i < values.Length; i++) {
                object? val = values[i];
                var sqlOutput = sqlColumns[i];
                if (val == DBNull.Value) {
                    val = null;
                }
                else if (val is string text) {
                    if (sqlOutput.Type.IsAssignableTo(typeof(Guid?))) {
                        val = Guid.Parse(text);
                    }
                    else if (sqlOutput.Type.IsAssignableTo(typeof(DateOnly?))) {
                        val = DateOnly.Parse(text);
                    }
                    else if (sqlOutput.Type.IsAssignableTo(typeof(DateTime?))) {
                        var dt = DateTime.Parse(text);
                        val = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }
                    else {
                        // No change needed
                    }
                }
                else {
                    val = Convert.ChangeType(val, sqlColumns[i].Type);
                }
                values[i] = val;
            }
            records.Add(values);
        }

        var ctor = typeof(T).GetConstructor(sqlColumns.Select(x => x.Type).ToArray())
            ?? throw new Exception("A constructor was not found that matches: " + string.Join(", ", sqlColumns.Select(x => x.Type)));

        var items = new List<T>(records.Count);
        items.AddRange(records.Select(x => (T)ctor.Invoke(x)));
        return items;
    }

    public static async Task<List<T>> List<T>(this DbConnection dbConnection, Expression<Func<IQueryable<T>>> query, CancellationToken cancellationToken) {
        var (cmd, sqlColumns) = dbConnection.CreateCommand(query);

        await dbConnection.OpenAsync(cancellationToken);

        DbDataReader _reader;
        try {
            _reader = await cmd.ExecuteReaderAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is DbException) {
            throw new Exception(cmd.CommandText, ex);
        }

        using var reader = _reader;
        _reader = null!;

        var records = new List<object?[]>();
        while (await reader.ReadAsync(cancellationToken)) {

            var values = new object?[sqlColumns.Count];
            reader.GetValues(values!);

            for (int i = 0; i < values.Length; i++) {
                object? val = values[i];
                var sqlOutput = sqlColumns[i];
                if (val == DBNull.Value) {
                    val = null;
                }
                else if (val is string text) {
                    if (sqlOutput.Type.IsAssignableTo(typeof(Guid?))) {
                        val = Guid.Parse(text);
                    }
                    else if (sqlOutput.Type.IsAssignableTo(typeof(DateOnly?))) {
                        val = DateOnly.Parse(text);
                    }
                    else if (sqlOutput.Type.IsAssignableTo(typeof(DateTime?))) {
                        var dt = DateTime.Parse(text);
                        val = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }
                    else {
                        // No change needed
                    }
                }
                else {
                    val = Convert.ChangeType(val, sqlColumns[i].Type);
                }
                values[i] = val;
            }
            records.Add(values);
        }

        var ctor = typeof(T).GetConstructor(sqlColumns.Select(x => x.Type).ToArray())
            ?? throw new Exception("A constructor was not found that matches: " + string.Join(", ", sqlColumns.Select(x => x.Type)));

        var items = new List<T>(records.Count);
        items.AddRange(records.Select(x => (T)ctor.Invoke(x)));
        return items;
    }

    public static T? Nullable<T>(this DbConnection connection, Expression<Func<IQueryable<T>>> query)
        where T : struct {
        var list = connection.List(query);

        return list.Any()
            ? list.Single()
            : null;
    }

    public static async ValueTask<T?> Nullable<T>(
        this DbConnection connection,
        Expression<Func<IQueryable<T>>> query,
        CancellationToken cancellationToken
    ) where T : struct {
        var list = await connection.List(query, cancellationToken);

        return list.Any()
            ? list.Single()
            : null;
    }

    public static int Execute<T>(this DbConnection connection, Expression<Func<IQueryable<T>>> query)
        where T : struct {

        var (cmd, _) = connection.CreateCommand(query);

        connection.Open();
        return cmd.ExecuteNonQuery();
    }

    public static async ValueTask<int> Execute<T>(
        this DbConnection connection,
        Expression<Func<IQueryable<T>>> query,
        CancellationToken cancellationToken
    ) where T : struct {

        var (cmd, _) = connection.CreateCommand(query);

        await connection.OpenAsync(cancellationToken);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public static (DbCommand Command, StableList<SqlColumn> SqlColumns) CreateCommand(this DbConnection dbConnection, LambdaExpression expression) {
        var translation = TranslateToSelectStmt(expression);

        SqliteComposer sqliteComposer = new();
        var parameterizedSql = sqliteComposer.Compose(translation);

        var sqlColumns = parameterizedSql.Segments.OfType<SqlColumn>().ToStableList();
        var constantParameters = parameterizedSql.Segments.OfType<SqlConstantParameter>().ToArray();
        var inputParameters = parameterizedSql.Segments.OfType<SqlInputParameter>().ToArray();

        var parameterNumber = 1;
        var commandText = string.Concat(parameterizedSql.Segments.Select(x => x switch {
            SqlRaw raw => raw.Text,
            SqlConstantParameter constant => "@p" + parameterNumber++,
            SqlInputParameter input => "@" + (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
            SqlColumn output => string.Empty,
            _ => throw new NotSupportedException(x?.GetType().ToString())
        }));

        var cmd = dbConnection.CreateCommand();
        cmd.CommandText = commandText;

        parameterNumber = 1;

        cmd.Parameters.AddRange(constantParameters
            .Select(Constant => new { Constant, Param = cmd.CreateParameter() })
            .Select(x => {
                x.Param.ParameterName = "p" + parameterNumber++;
                x.Param.Value = x.Constant.Value;
                // TODO? Set DbType based on x.Constant.Type
                return x.Param;
            })
            .ToArray());

        cmd.Parameters.AddRange(inputParameters
            .Select(Input => new { Input, Param = cmd.CreateParameter() })
            .Select(x => {
                x.Param.ParameterName = (x.Input.SuggestedName != string.Empty ? x.Input.SuggestedName : "p") + parameterNumber++;
                x.Param.Value = true;
                // TODO? Set DbType based on x.Input.Type
                return x.Param;
            })
            .ToArray());

        return (cmd, sqlColumns);
    }
}
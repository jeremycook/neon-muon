using Microsoft.Data.Sqlite;
using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using System.Data.Common;
using System.Linq.Expressions;

namespace Sqlil.Sqlite.Tests;

public static class TestHelpers {
    private static readonly SelectStmtTranslator translator = new();
    private static readonly SqliteComposer composer = new();

    public static object Translate(LambdaExpression expression) {
        return translator.Translate(expression, default);
    }

    public static string Compose(object statement) {
        var parameterizedSql = composer.Compose(statement);
        var parameterNumber = 1;
        return string.Concat(parameterizedSql.Segments.Select(x => x switch {
            SqlRaw raw => raw.Text,
            SqlConstantParameter constant => "@p" + parameterNumber++,
            SqlInputParameter input => "@" + (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
            SqlColumn output => string.Empty,
            _ => throw new NotSupportedException(x?.GetType().ToString())
        }));
    }

    public static (string CommandText, DbCommand cmd) PrepareCommand(DbConnection connection, LambdaExpression expression) {
        var translation = Translate(expression);
        var parameterizedSql = composer.Compose(translation);
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

        var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;

        parameterNumber = 1;
        foreach (var constant in constantParameters) {
            cmd.Parameters.Add(new SqliteParameter("p" + parameterNumber++, constant.Value ?? DBNull.Value));
        }
        foreach (var input in inputParameters) {
            cmd.Parameters.Add(new SqliteParameter(
                (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
                true));
        }

        return (commandText, cmd);
    }

    public static List<string> QueryStrings(DbConnection connection, LambdaExpression expression) {
        var (commandText, cmd) = PrepareCommand(connection, expression);
        if (connection.State != System.Data.ConnectionState.Open) {
            connection.Open();
        }
        using var reader = cmd.ExecuteReader();
        var results = new List<string>();
        while (reader.Read()) {
            results.Add(reader.GetString(0));
        }
        return results;
    }

    public static int ExecuteDml(DbConnection connection, LambdaExpression expression) {
        var (commandText, cmd) = PrepareCommand(connection, expression);
        if (connection.State != System.Data.ConnectionState.Open) {
            connection.Open();
        }
        return cmd.ExecuteNonQuery();
    }

    public static int QueryCount(DbConnection connection, LambdaExpression expression) {
        var (commandText, cmd) = PrepareCommand(connection, expression);
        if (connection.State != System.Data.ConnectionState.Open) {
            connection.Open();
        }
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}

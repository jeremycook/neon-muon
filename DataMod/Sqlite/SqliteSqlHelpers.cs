using DataCore;
using Microsoft.Data.Sqlite;

namespace DataMod.Sqlite;

public static class SqliteSqlHelpers {
    public static string Quote(this SqlIdentifier sqlIdentifier) {
        return
            (!string.IsNullOrEmpty(sqlIdentifier.Prefix) ? "\"" + sqlIdentifier.Prefix.Replace("\"", "\"\"") + "\"." : string.Empty) +
            (sqlIdentifier.Value == "*" ? "*" : "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"");
    }

    public static string Quote(this SqlLiteral sqlLiteral) {
        return "'" + sqlLiteral.Value.Replace("'", "''") + "'";
    }

    public static (string CommandText, SqliteParameter[] Parameters) ParameterizeSql(this Sql sql) {
        var tempValues = new List<object>();
        var formatArgs = new List<string>(sql.Arguments.Count);

        foreach (var arg in sql.Arguments) {
            switch (arg) {
                case SqlIdentifier sqlIdentifier:
                    formatArgs.Add(Quote(sqlIdentifier));
                    break;

                case SqlLiteral sqlLiteral:
                    formatArgs.Add(Quote(sqlLiteral));
                    break;

                case Sql innerSql:
                    formatArgs.Add(GetParameterizedSql(innerSql, ref tempValues));
                    break;

                default:
                    formatArgs.Add($"${tempValues.Count + 1}");
                    tempValues.Add(arg ?? DBNull.Value);
                    break;
            }
        }

        string commandText = string.Format(sql.Format, args: formatArgs.ToArray());
        var parameters = tempValues
            .Select((val, i) => val switch {
                _ => new SqliteParameter("p" + i, val),
            })
            .ToArray();
        return (commandText, parameters);
    }

    private static string GetParameterizedSql(Sql sql, ref List<object> parameterValues) {
        var formatArgs = new List<string>(sql.Arguments.Count);

        foreach (var arg in sql.Arguments) {
            switch (arg) {
                case SqlIdentifier sqlIdentifier:
                    formatArgs.Add(Quote(sqlIdentifier));
                    break;

                case SqlLiteral sqlLiteral:
                    formatArgs.Add(Quote(sqlLiteral));
                    break;

                case Sql innerSql:
                    formatArgs.Add(GetParameterizedSql(innerSql, ref parameterValues));
                    break;

                default:
                    formatArgs.Add($"@p{parameterValues.Count}");
                    parameterValues.Add(arg ?? DBNull.Value);
                    break;
            }
        }

        return string.Format(sql.Format, args: formatArgs.ToArray());
    }
}

using Microsoft.Data.Sqlite;

namespace DataMod.Sqlite;

public static class SqliteSqlHelpers
{
    public static string Quote(this SqlIdentifier sqlIdentifier)
    {
        return
            (sqlIdentifier.Prefix is not null ? "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"." : string.Empty) +
            "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"";
    }

    public static string Quote(this SqlLiteral sqlLiteral)
    {
        return "'" + sqlLiteral.Value.Replace("'", "''") + "'";
    }

    public static (string commandText, SqliteParameter[] parameterValues) ParameterizeSql(this Sql sql)
    {
        var tempValues = new List<object>();
        var formatArgs = new List<string>(sql.Arguments.Count);

        foreach (var arg in sql.Arguments)
        {
            switch (arg)
            {
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
        var parameterValues = tempValues
            .Select(val => val switch
            {
                _ => new SqliteParameter() { Value = val },
            })
            .ToArray();
        return (commandText, parameterValues);
    }

    private static string GetParameterizedSql(Sql sql, ref List<object> parameterValues)
    {
        var formatArgs = new List<string>(sql.Arguments.Count);

        foreach (var arg in sql.Arguments)
        {
            switch (arg)
            {
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
                    formatArgs.Add($"${parameterValues.Count + 1}");
                    parameterValues.Add(arg ?? DBNull.Value);
                    break;
            }
        }

        return string.Format(sql.Format, args: formatArgs.ToArray());
    }
}

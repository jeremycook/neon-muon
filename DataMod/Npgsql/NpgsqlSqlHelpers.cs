using DataCore;
using Npgsql;

namespace DataMod.Npgsql;

public static class NpgsqlSqlHelpers
{
    public static string Quote(this SqlIdentifier sqlIdentifier)
    {
        return
            (!string.IsNullOrEmpty(sqlIdentifier.Prefix) ? "\"" + sqlIdentifier.Prefix.Replace("\"", "\"\"") + "\"." : string.Empty) +
            "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"";
    }

    public static string Quote(this SqlLiteral sqlLiteral)
    {
        return "'" + sqlLiteral.Value.Replace("'", "''") + "'";
    }

    public static NpgsqlBatchCommand NpgsqlBatchCommand(this Sql sql)
    {
        var (commandText, parameterValues) = ParameterizeSql(sql);

        NpgsqlBatchCommand batchCommand = new(commandText);
        batchCommand.Parameters.AddRange(parameterValues);

        return batchCommand;
    }

    public static (string commandText, NpgsqlParameter[] parameterValues) ParameterizeSql(this Sql sql)
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
                char[] charArray => new NpgsqlParameter() { Value = charArray, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.InternalChar },
                _ => new NpgsqlParameter() { Value = val },
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

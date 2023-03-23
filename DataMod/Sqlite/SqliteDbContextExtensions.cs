using Microsoft.Data.Sqlite;
using System.Reflection;

namespace DataMod.Sqlite;

public static class SqliteDbContextExtensions
{
    public static async ValueTask<int> ExecuteAsync(this SqliteConnection connection, Sql sql, CancellationToken cancellationToken = default)
    {
        var (CommandText, ParameterValues) = ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqliteException ex)
        {
            throw new Exception("An error occurred executing command: " + CommandText, ex);
        }
    }


    public static async ValueTask<List<T>> ListAsync<T>(this SqliteConnection connection, Sql sql, CancellationToken cancellationToken = default)
    {
        var (CommandText, ParameterValues) = ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        var list = new List<T>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            var columns = await reader.GetColumnSchemaAsync(cancellationToken);
            var primitive = columns.Count == 1 && columns[0].DataType == typeof(T);
            if (primitive)
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var value = reader.GetFieldValue<T>(0);
                    list.Add(value);
                }
            }
            else
            {
                var props = columns.Select(c => new
                {
                    Column = c,
                    Prop = typeof(T).GetProperty(c.ColumnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!,
                });
                while (await reader.ReadAsync(cancellationToken))
                {
                    var item = Activator.CreateInstance<T>();
                    foreach (var prop in props)
                    {
                        var value = reader.GetValue(prop.Column.ColumnOrdinal!.Value);
                        if (value == DBNull.Value)
                        {
                            value = null;
                        }

                        try
                        {
                            if (value is not null && value.GetType() != prop.Prop.PropertyType)
                            {
                                value = Convert.ChangeType(value, prop.Prop.PropertyType);
                            }

                            prop.Prop.SetValue(item, value);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error setting value of {prop.Prop.DeclaringType?.Name}.{prop.Prop.Name}. {ex.GetBaseException().Message}", ex);
                        }
                    }
                    list.Add(item);
                }
            }
        }

        return list;
    }


    public static (string CommandText, SqliteParameter[] ParameterValues) ParameterizeSql(Sql sql)
    {
        var tempValues = new List<object>();
        var formatArgs = new List<string>(sql.Arguments.Count);

        foreach (var arg in sql.Arguments)
        {
            switch (arg)
            {
                case SqlIdentifier sqlIdentifier:
                    formatArgs.Add(
                        (sqlIdentifier.Prefix is not null ? "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"." : string.Empty) +
                        "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"");
                    break;

                case SqlLiteral sqlLiteral:
                    formatArgs.Add("'" + sqlLiteral.Value.Replace("'", "''") + "'");
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

        var parameterValues = tempValues
            .Select(val => val switch
            {
                //char[] charArray => new SqliteParameter() { Value = charArray, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.InternalChar },
                _ => new SqliteParameter() { Value = val },
            })
            .ToArray();
        return (CommandText: string.Format(sql.Format, args: formatArgs.ToArray()), ParameterValues: parameterValues);
    }

    private static string GetParameterizedSql(Sql sql, ref List<object> parameterValues)
    {
        var formatArgs = new List<string>(sql.Arguments.Count);

        foreach (var arg in sql.Arguments)
        {
            switch (arg)
            {
                case SqlIdentifier sqlIdentifier:
                    formatArgs.Add(
                        (sqlIdentifier.Prefix is not null ? "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"." : string.Empty) +
                        "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"");
                    break;

                case SqlLiteral sqlLiteral:
                    formatArgs.Add("'" + sqlLiteral.Value.Replace("'", "''") + "'");
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

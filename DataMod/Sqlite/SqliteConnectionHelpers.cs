using DataCore;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace DataMod.Sqlite;

public static class SqliteConnectionHelpers
{
    public static async ValueTask<int> ExecuteAsync(this SqliteConnection connection, Sql sql, CancellationToken cancellationToken = default)
    {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

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
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

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
}

using DataCore;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Reflection;

namespace DataMod.Sqlite;

public static class SqliteConnectionHelpers {
    public static int Execute(this SqliteConnection connection, Sql sql) {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        return connection.Execute(command);
    }

    public static int Execute(this SqliteConnection connection, SqliteCommand command) {
        var lastConnection = command.Connection;
        command.Connection = connection;

        try {
            var modifications = command.ExecuteNonQuery();
            command.Connection = lastConnection;
            return modifications;
        }
        catch (SqliteException ex) {
            throw new Exception("Error executing: " + command.CommandText, ex);
        }
    }

    public static async ValueTask<int> ExecuteAsync(this SqliteConnection connection, Sql sql, CancellationToken cancellationToken = default) {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        return await connection.ExecuteAsync(command, cancellationToken);
    }

    public static async ValueTask<int> ExecuteAsync(this SqliteConnection connection, SqliteCommand command, CancellationToken cancellationToken = default) {
        var lastConnection = command.Connection;
        command.Connection = connection;

        try {
            var modifications = await command.ExecuteNonQueryAsync(cancellationToken);
            command.Connection = lastConnection;
            return modifications;
        }
        catch (SqliteException ex) {
            throw new Exception("Error executing: " + command.CommandText, ex);
        }
    }

    public static async ValueTask<List<T>> ListAsync<T>(this SqliteConnection connection, Sql sql, CancellationToken cancellationToken = default) {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        return await command.ListAsync<T>(cancellationToken);
    }

    public static async ValueTask<List<T>> ListAsync<T>(this SqliteConnection connection, SqliteCommand command, CancellationToken cancellationToken = default) {
        var lastConnection = command.Connection;
        command.Connection = connection;

        var list = await command.ListAsync<T>(cancellationToken);

        command.Connection = lastConnection;

        return list;
    }

    public static async ValueTask<List<T>> ListAsync<T>(this SqliteCommand command, CancellationToken cancellationToken = default) {
        var type = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        var list = new List<T>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken)) {

            var columns = await reader.GetColumnSchemaAsync(cancellationToken);

            var primitive = underlyingType.IsPrimitive || underlyingType == typeof(string) || underlyingType == typeof(Guid);
            if (primitive) {
                while (await reader.ReadAsync(cancellationToken)) {
                    var value = reader.GetFieldValue<T>(0);
                    list.Add(value);
                }
            }
            else {
                var columnNames = columns.Select(c => c.ColumnName).ToArray();

                // Grab the public constructor that best matches
                // the database columns.
                var ctor = type.GetConstructors()
                    .Where(t => t.GetParameters().All(p => columnNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase)))
                    .OrderByDescending(c => c.GetParameters().Count(p => columnNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase)))
                    .FirstOrDefault();

                Func<object?[]?, T> itemActivator;
                ParameterInfo[] parameters;
                if (ctor != null) {
                    itemActivator = parameters => (T)ctor.Invoke(parameters);
                    parameters = ctor.GetParameters();
                }
                else {
                    itemActivator = parameters => Activator.CreateInstance<T>();
                    parameters = Array.Empty<ParameterInfo>();
                }

                var settableProperties = type.GetProperties()
                    .Where(p => p.GetSetMethod() != null)
                    .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

                var settableColumns = columns
                    .Where(c => settableProperties.Keys.Contains(c.ColumnName))
                    .Select(c => (c.ColumnName, Property: settableProperties[c.ColumnName]))
                    .ToArray();

                while (await reader.ReadAsync(cancellationToken)) {
                    var parameterValues = parameters.Select(p => ConvertTo(reader.GetValue(p.Name!), p.ParameterType)).ToArray();
                    var item = itemActivator(parameterValues);
                    foreach (var (columnName, property) in settableColumns) {
                        try {
                            var value = reader.GetValue(columnName);
                            value = ConvertTo(value, property.PropertyType);
                            property.SetValue(item, value);
                        }
                        catch (Exception ex) {
                            throw new Exception($"Error setting value of {property.DeclaringType?.Name}.{property.Name}. {ex.GetBaseException().Message}", ex);
                        }
                    }
                    list.Add(item);
                }
            }
        }

        return list;
    }

    private static object? ConvertTo(object? value, Type targetType) {
        if (value == DBNull.Value) {
            value = null;
        }

        if (value is not null && value.GetType() != targetType) {
            if (value is string text && targetType == typeof(Guid)) {
                value = Guid.Parse(text);
            }
            else {
                value = Convert.ChangeType(value, targetType);
            }
        }

        return value;
    }
}

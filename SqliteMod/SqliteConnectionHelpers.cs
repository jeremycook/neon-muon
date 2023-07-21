using DatabaseMod.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using SqlMod;
using System.Data;
using System.Data.Common;

namespace SqliteMod;

public static class SqliteConnectionHelpers {

    public static string GetAppConnectionString(this IConfigurationRoot configuration, string connectionString) {
        string appDataDir = configuration.GetRequiredSection("AppDataDir").Value!;

        var csb = new SqliteConnectionStringBuilder(connectionString);
        csb.DataSource = Path.GetFullPath(csb.DataSource, appDataDir);

        return csb.ConnectionString;
    }

    public static string GetUserConnectionString(this IConfigurationRoot configuration, string connectionString, SqliteOpenMode mode = SqliteOpenMode.ReadOnly) {
        string userDataDir = configuration.GetRequiredSection("UserDataDir").Value!;

        var csb = new SqliteConnectionStringBuilder(connectionString) {
            Mode = mode,
        };
        csb.DataSource = Path.GetFullPath(csb.DataSource, userDataDir);

        return csb.ConnectionString;
    }

    public static Database GetDatabase(this SqliteConnection connection) {
        // TODO: Cache results based on connection string,
        // and add an connection.InvalidateDatabase() extension method that will invalidate the cache
        var database = new Database();
        database.ContributeSqlite(connection);
        return database;
    }

    public static SqliteCommand CreateCommand(this SqliteConnection connection, Sql sql) {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        return command;
    }

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

    public static long? Number(this SqliteConnection connection, Sql sql) {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        return (long?)connection.Scalar(command);
    }

    public static object? Scalar(this SqliteConnection connection, SqliteCommand command) {
        var lastConnection = command.Connection;
        command.Connection = connection;

        try {
            var result = command.ExecuteScalar();
            command.Connection = lastConnection;
            return result;
        }
        catch (SqliteException ex) {
            throw new Exception("Error executing: " + command.CommandText, ex);
        }
    }

    public static List<T> List<T>(this SqliteConnection connection, Sql sql) {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        return List<T>(command);
    }

    public static List<T> List<T>(this SqliteConnection connection, SqliteCommand command) {
        var lastConnection = command.Connection;
        command.Connection = connection;

        var list = List<T>(command);

        command.Connection = lastConnection;

        return list;
    }

    public static object?[] First(this SqliteConnection connection, Sql sql, StoreType[] returningStoreTypes) {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);

        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            object?[] record = new object?[reader.FieldCount];
            reader.GetValues(record);
            for (int i = 0; i < reader.FieldCount; i++) {
                record[i] = SqliteDatabaseHelpers.ConvertDatabaseValueToStoreValue(record[i]!, returningStoreTypes[i]);
            }
            return record;
        }

        throw new Exception("No records were returned.");
    }

    public static List<object?[]> List(this SqliteConnection connection, Sql sql, IEnumerable<StoreType> returningStoreTypes) {
        var (CommandText, ParameterValues) = SqliteSqlHelpers.ParameterizeSql(sql);
        var storeTypes = returningStoreTypes.ToArray();

        using var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.Parameters.AddRange(ParameterValues);


        var list = new List<object?[]>();

        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            object?[] record = new object?[reader.FieldCount];
            reader.GetValues(record);
            for (int i = 0; i < reader.FieldCount; i++) {
                record[i] = SqliteDatabaseHelpers.ConvertDatabaseValueToStoreValue(record[i]!, storeTypes[i]);
            }
            list.Add(record as dynamic);
        }

        return list;
    }

    public static List<T> List<T>(SqliteCommand command) {
        var type = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        var list = new List<T>();
        using (var reader = command.ExecuteReader()) {

            var primitive = underlyingType.IsPrimitive || underlyingType == typeof(string) || underlyingType == typeof(Guid);
            if (primitive) {
                while (reader.Read()) {
                    var value = reader.GetFieldValue<T>(0);
                    list.Add(value);
                }
            }
            else if (underlyingType == typeof(object?[])) {
                while (reader.Read()) {
                    var record = new object?[reader.FieldCount];
                    reader.GetValues(record);
                    for (int i = 0; i < reader.FieldCount; i++) {
                        if (record[i] == DBNull.Value) {
                            record[i] = null;
                        }
                    }
                    list.Add(record as dynamic);
                }
            }
            else {
                var columns = reader.GetColumnSchema();
                var columnNames = columns.Select(c => c.ColumnName).ToArray();

                Func<object?[]?, T> itemActivator;
                (string? Name, int Ordinal, Type Type)[]? parameters;

                // Grab the public constructor that best matches
                // the database columns.
                var ctor = type.GetConstructors()
                    .Where(t => t.GetParameters().All(p => columnNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase)))
                    .OrderByDescending(c => c.GetParameters().Count(p => columnNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase)))
                    .FirstOrDefault();

                if (ctor != null) {
                    itemActivator = parameters => (T)ctor.Invoke(parameters);
                    parameters = ctor.GetParameters()
                        .Select((p, i) => ValueTuple.Create(p.Name, p.Position, p.ParameterType))
                        .ToArray();
                }
                else if (underlyingType.IsValueType) {
                    var typeArgs = underlyingType.GetGenericArguments();
                    var ValueTuple_Create_T = typeof(ValueTuple).GetMethods()
                        .Where(m => m.Name == "Create" && m.GetParameters().Length == typeArgs.Length)
                        .SingleOrDefault()
                        ?? throw new NotSupportedException($"ValueTuple.Create method not found with {typeArgs.Length} generic arguments.");

                    var ValueTuple_Create = typeArgs.Length > 0
                        ? ValueTuple_Create_T.MakeGenericMethod(typeArgs)
                        : ValueTuple_Create_T;

                    itemActivator = parameters => (T)ValueTuple_Create.Invoke(null, parameters)!;
                    parameters = ValueTuple_Create.GetParameters()
                        .Select((p, i) => ValueTuple.Create<string?, int, Type>(null, p.Position, p.ParameterType))
                        .ToArray();
                }
                else {
                    throw new NotSupportedException();
                    itemActivator = parameters => Activator.CreateInstance<T>();
                    parameters = Array.Empty<(string? Name, int Ordinal, Type Type)>();
                }

                var settableProperties = type.GetProperties()
                    .Where(p => p.GetSetMethod() != null)
                    .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

                var settableColumns = columns
                    .Where(c => settableProperties.ContainsKey(c.ColumnName))
                    .Select(c => (c.ColumnName, Property: settableProperties[c.ColumnName]))
                    .ToArray();

                while (reader.Read()) {
                    var parameterValues =
                        parameters.Select(p => ConvertTo(p.Name != null ? reader.GetValue(p.Name) : reader.GetValue(p.Ordinal), p.Type)).ToArray();

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

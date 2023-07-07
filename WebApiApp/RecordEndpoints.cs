using DatabaseMod.Models;
using FileMod;
using Microsoft.Data.Sqlite;
using SqliteMod;
using SqlMod;
using System.Text.Json;

namespace WebApiApp;

public class RecordEndpoints {

    public enum SortDirection {
        Asc = 0,
        Desc = 1,
    }

    public record SelectRecordsInput(
        string Database,
        string Schema,
        string Table,
        string[] Columns,
        (string, SortDirection)[]? OrderBy = null
    ) { }

    public static IResult SelectRecords(
        UserFileProvider fileProvider,
        SelectRecordsInput input
    ) {
        var fullPath = fileProvider.GetFullPath(input.Database);

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadOnly,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        var database = connection.GetDatabase();
        var table = database.GetTable(input.Schema, input.Table);

        var tableSchema = input.Schema;
        var columnsByName = table.Columns.ToDictionary(o => o.Name);
        var columnNames = input.Columns
            .Select(name => columnsByName[name].Name)
            .ToArray();
        var storeTypes = columnNames
            .Select(name => columnsByName[name].StoreType)
            .ToArray();
        (string, SortDirection)[] orderBy;
        if (input.OrderBy?.Length > 0) {
            orderBy = input.OrderBy;
        }
        else {
            var match =
                table.Columns.FirstOrDefault(column => column.StoreType == StoreType.Text) ??
                table.Columns.First();

            orderBy = new[] {
                (match.Name, SortDirection.Asc),
            };
        }

        var sql = Sql.Interpolate($"""
            SELECT {Sql.IdentifierList(columnNames)}
            FROM {Sql.Identifier(tableSchema, table.Name)}
            ORDER BY {Sql.Join(", ", orderBy.Select(x => Sql.Interpolate($"{Sql.Identifier(x.Item1)} {Sql.Raw(x.Item2 == SortDirection.Desc ? "DESC" : "")}")))}
        """);

        var records = connection.List(sql, storeTypes);

        return Results.Ok(records);
    }

    public record InsertRecordsInput(
        string Database,
        string Schema,
        string Table,
        string[] Columns,
        JsonElement?[][] Records,
        string[] ReturningColumns
    ) { }

    public static IResult InsertRecords(
        UserFileProvider fileProvider,
        InsertRecordsInput input
    ) {
        var fullPath = fileProvider.GetFullPath(input.Database);

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fullPath,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var database = connection.GetDatabase();
        var table = database.GetTable(input.Schema, input.Table);
        var columnsByName = table.Columns.ToDictionary(o => o.Name);
        var insertedColumns = input.Columns
            .Select((name, i) => new { i, name, storeType = columnsByName[name].StoreType })
            .ToArray();
        var returningColumns = input.ReturningColumns.Length > 0
            ? input.ReturningColumns
            : table.GetPrimaryKey().Columns.ToArray();

        var returningRecords = new List<object?[]>();
        foreach (var record in input.Records) {
            var sql = Sql.Interpolate($"""
                INSERT INTO {Sql.Identifier(input.Schema, table.Name)} ({Sql.IdentifierList(insertedColumns.Select(column => column.name))})
                VALUES ({Sql.Join(", ", insertedColumns.Select(o => Sql.Value(SqliteDatabaseHelpers.ConvertJsonElementToStoreValue(record[o.i], o.storeType) ?? DBNull.Value)))})
                RETURNING {Sql.IdentifierList(returningColumns)}
            """);
            var newRecord = connection.First(sql, table.Columns.Select(column => column.StoreType).ToArray());
            returningRecords.Add(newRecord);
        }

        transaction.Commit();

        return Results.Ok(returningRecords);
    }

    public record UpdateRecordsInput(
        string Database,
        string Schema,
        string Table,
        string[] Columns,
        JsonElement?[][] Records
    ) { }

    public static IResult UpdateRecords(
        UserFileProvider fileProvider,
        UpdateRecordsInput input
    ) {
        var fullPath = fileProvider.GetFullPath(input.Database);

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fullPath,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var database = connection.GetDatabase();
        var table = database.GetTable(input.Schema, input.Table);
        var columnsByName = table.Columns.ToDictionary(o => o.Name);

        var pk = table.GetPrimaryKey()?.Columns;

        if (pk == null) {
            return Results.BadRequest("Cannot update values because this table doesn't have a primary key.");
        }

        var pkColumnNames = input.Columns
            .Select((name, i) => new { name, i })
            .Where(o => pk.Contains(o.name))
            .ToArray();

        if (pkColumnNames.Length != pk.Count) {
            return Results.BadRequest("The tables primary keys must be provided.");
        }

        var modifiedColumnNames = input.Columns
            .Select((name, i) => new { name, i })
            .Except(pkColumnNames)
            .ToArray();

        if (!modifiedColumnNames.All(o => columnsByName.ContainsKey(o.name))) {
            return Results.BadRequest("A column name is invalid.");
        }

        var changes = 0;
        foreach (var record in input.Records) {
            var sql = Sql.Interpolate($"""
                UPDATE {Sql.Identifier(input.Schema, input.Table)}
                SET {Sql.Join(", ", modifiedColumnNames.Select(o => Sql.Interpolate($"{Sql.Identifier(o.name)} = {Sql.Value(SqliteDatabaseHelpers.ConvertJsonElementToStoreValue(record[o.i], columnsByName[o.name].StoreType) ?? DBNull.Value)}")))}
                WHERE {Sql.Join(" AND ", pkColumnNames.Select(o => Sql.Interpolate($"{Sql.Identifier(o.name)} = {Sql.Value(SqliteDatabaseHelpers.ConvertJsonElementToStoreValue(record[o.i], columnsByName[o.name].StoreType))}")))}
            """);
            using var command = connection.CreateCommand(sql);
            changes += command.ExecuteNonQuery();
        }

        transaction.Commit();

        return Results.Ok(changes);
    }

    public record DeleteRecordsInput(
        string Database,
        string Schema,
        string Table,
        string[] Columns,
        JsonElement?[][] Records
    ) { }

    public static IResult DeleteRecords(
        UserFileProvider fileProvider,
        DeleteRecordsInput input
    ) {
        var fullPath = fileProvider.GetFullPath(input.Database);

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fullPath,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        var database = connection.GetDatabase();
        var table = database.GetTable(input.Schema, input.Table);
        var columnsByName = table.Columns.ToDictionary(o => o.Name);
        var storeTypes = input.Columns.Select(columnName => columnsByName[columnName].StoreType).ToArray();

        var sql = Sql.Interpolate($"""
            DELETE FROM {Sql.Identifier(input.Schema, input.Table)}
            WHERE ({Sql.Join(") OR (", input.Records.Select(r =>
                Sql.Join(" AND ", r.Select((value, i) =>
                    Sql.Interpolate($"(({Sql.Identifier(input.Columns[i])} IS NULL AND {Sql.Value(SqliteDatabaseHelpers.ConvertJsonElementToStoreValue(value, storeTypes[i]) ?? DBNull.Value)} IS NULL) OR {Sql.Identifier(input.Columns[i])} = {Sql.Value(SqliteDatabaseHelpers.ConvertJsonElementToStoreValue(value, storeTypes[i]) ?? DBNull.Value)})")
                ))
            ))})
        """);

        var changes = connection.Execute(sql);

        return Results.Ok(changes);
    }
}

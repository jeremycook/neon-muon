using DatabaseMod.Models;
using FileMod;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using SqliteMod;
using SqlMod;
using System.Text.Json;

namespace WebApiApp;

public class RecordEndpoints {

    public static IResult GetRecords(
        UserFileProvider fileProvider,
        string path,
        string tableName,
        [FromQuery] string[] columnNames
    ) {
        var fileInfo = fileProvider.GetFileInfo(path);

        if (!fileInfo.Exists) {
            return Results.NotFound();
        }

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fileInfo.PhysicalPath!,
            Mode = SqliteOpenMode.ReadOnly,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        //var countSql = Sql.Interpolate($"SELECT COUNT(*) FROM {Sql.Identifier(input.TableName)}");
        //var totalRows = connection.Number(countSql);

        var querySql = Sql.Interpolate($"SELECT {Sql.IdentifierList(columnNames)} FROM {Sql.Identifier(tableName)}");
        using var command = connection.CreateCommand(querySql);

        var columns = columnNames.Length;
        var records = new List<object?[]>();

        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            var values = new object?[columns];
            reader.GetValues(values);
            records.Add(values);
            for (int i = 0; i < columns; i++) {
                if (values[i] == DBNull.Value) {
                    values[i] = null;
                }
            }
        }

        return Results.Ok(records);
    }

    public record ChangeRecordsInput(
        string Path,
        string TableName,
        string[] ColumnNames,
        JsonElement?[][] Records
    );

    public static IResult InsertRecords(
        UserFileProvider fileProvider,
        ChangeRecordsInput input
    ) {
        var fileInfo = fileProvider.GetFileInfo(input.Path);

        if (!fileInfo.Exists) {
            return Results.NotFound();
        }

        var commandSql = Sql.Interpolate($"""
INSERT INTO {Sql.Identifier(input.TableName)} ({Sql.IdentifierList(input.ColumnNames)}) 
VALUES ({Sql.Join("), (", input.Records.Select(
    Sql.Value
))})
""");

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fileInfo.PhysicalPath!,
            Mode = SqliteOpenMode.ReadOnly,
        };

        using var connection = new SqliteConnection(builder.ConnectionString);
        using var command = connection.CreateCommand(commandSql);
        connection.Open();

        var changes = command.ExecuteNonQuery();

        return Results.Ok(changes);
    }

    public static IResult UpdateRecords(
        UserFileProvider fileProvider,
        ChangeRecordsInput input
    ) {
        var fileInfo = fileProvider.GetFileInfo(input.Path);

        if (!fileInfo.Exists) {
            return Results.NotFound();
        }

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fileInfo.PhysicalPath!,
        };

        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var database = new Database();
        database.ContributeSqlite(connection);
        var table = database.Schemas[0].Tables.Single(o => o.Name == input.TableName);
        var columnsByName = table.Columns.ToDictionary(o => o.Name);

        var pk = table.Indexes.SingleOrDefault(x => x.IndexType == TableIndexType.PrimaryKey)?.Columns;

        if (pk == null) {
            return Results.BadRequest("Cannot update values because this table doesn't have a primary key.");
        }


        var pkColumnNames = input.ColumnNames
            .Select((name, i) => new { name, i })
            .Where(o => pk.Contains(o.name))
            .ToArray();

        if (pkColumnNames.Length != pk.Count) {
            return Results.BadRequest("The tables primary keys must be provided.");
        }

        var modifiedColumnNames = input.ColumnNames
            .Select((name, i) => new { name, i })
            .Except(pkColumnNames)
            .ToArray();

        if (!modifiedColumnNames.All(o => columnsByName.ContainsKey(o.name))) {
            return Results.BadRequest("A column name is invalid.");
        }

        var changes = 0;
        foreach (var record in input.Records) {
            var sql = Sql.Interpolate($"""
UPDATE {Sql.Identifier(input.TableName)}
SET {Sql.Join(", ", modifiedColumnNames.Select(o => Sql.Interpolate($"{Sql.Identifier(o.name)} = {Sql.Value(ChangeToStoreType(columnsByName[o.name], record[o.i]) ?? DBNull.Value)}")))}
WHERE {Sql.Join(" AND ", pkColumnNames.Select(o => Sql.Interpolate($"{Sql.Identifier(o.name)} = {Sql.Value(ChangeToStoreType(columnsByName[o.name], record[o.i]))}")))}
""");
            using var command = connection.CreateCommand(sql);
            changes += command.ExecuteNonQuery();
        }

        transaction.Commit();

        return Results.Ok(changes);
    }

    private static object? ChangeToStoreType(Column column, JsonElement? jsonElement) {
        object? convertedValue = column.StoreType switch {
            StoreType.Text => jsonElement?.GetString(),
            StoreType.Boolean => jsonElement?.GetBoolean(),
            StoreType.Date => jsonElement?.GetDateTime() is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : null,
            StoreType.Double => jsonElement?.GetDouble(),
            StoreType.Integer => jsonElement?.GetInt64(),
            StoreType.Time => jsonElement?.GetDateTime() is DateTime dateTime ? TimeOnly.FromDateTime(dateTime) : null,
            StoreType.Timestamp => jsonElement?.GetDateTime(),
            StoreType.Uuid => jsonElement?.GetGuid(),
            _ => throw new NotImplementedException(column.StoreType.ToString()),
        };
        return convertedValue;
    }

    public static IResult DeleteRecords(
        UserFileProvider fileProvider,
        ChangeRecordsInput input
    ) {
        var fileInfo = fileProvider.GetFileInfo(input.Path);

        if (!fileInfo.Exists) {
            return Results.NotFound();
        }

        var commandSql = Sql.Interpolate($"DELETE FROM {Sql.Identifier(input.TableName)} WHERE ({Sql.Join(") OR (", input.Records.Select(r =>
            Sql.Join(" AND ", r.Select((value, i) =>
                Sql.Interpolate($"{Sql.Identifier(input.ColumnNames[i])} = {Sql.Value(value)}")
            ))
        ))})");

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fileInfo.PhysicalPath!,
            Mode = SqliteOpenMode.ReadOnly,
        };

        using var connection = new SqliteConnection(builder.ConnectionString);
        using var command = connection.CreateCommand(commandSql);
        connection.Open();

        // TODO: Exception handling
        var changes = command.ExecuteNonQuery();

        return Results.Ok(changes);
    }
}

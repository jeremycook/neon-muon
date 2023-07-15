using ClosedXML.Excel;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using FileMod;
using Microsoft.Data.Sqlite;
using SqliteMod;
using SqlMod;
using System.Data;

namespace WebApiApp;

public class DatabaseEndpoints {

    public static Database GetDatabase(
        UserFileProvider fileProvider,
        string path
    ) {
        var fullPath = fileProvider.GetFullPath(path);

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadOnly,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        var database = connection.GetDatabase();
        return database;
    }

    public static IResult AlterDatabase(
        UserFileProvider fileProvider,
        string path,
        DatabaseAlteration[] databaseAlterations
    ) {
        var fullPath = fileProvider.GetFullPath(path);

        var validAlterations = new[] {
            typeof(CreateColumn),
            typeof(AlterColumn),
            typeof(RenameColumn),
            typeof(DropColumn),

            typeof(CreateTable),
            typeof(DropTable),
            typeof(RenameTable),
        };

        var invalidAlterations = databaseAlterations
            .Where(alt => !validAlterations.Contains(alt.GetType()));
        if (invalidAlterations.Any()) {
            return Results.BadRequest($"Only these kind of alterations can be applied: " + string.Join(", ", validAlterations.Select(va => va.Name)));
        }

        var sqlStatements = SqliteDatabaseScripter.ScriptAlterations(databaseAlterations);

        var builder = new SqliteConnectionStringBuilder() {
            DataSource = fullPath,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using (var transaction = connection.BeginTransaction()) {
            try {
                foreach (var sql in sqlStatements) {
                    connection.Execute(sql);
                }
            }
            catch (Exception ex) {
                return Results.BadRequest(ex.Message);
            }

            transaction.Commit();
        }

        return Results.Ok();
    }

    public record CreateTableBasedOnFileNodeInput(string SourcePath);
    public static IResult CreateTableBasedOnFileNode(UserFileProvider fileProvider, string path, CreateTableBasedOnFileNodeInput input) {
        var databasePath = fileProvider.GetFullPath(path);
        var builder = new SqliteConnectionStringBuilder() {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
        };
        // Attempt to connect to the database before proceeding
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        connection.Close();

        var data = new Dictionary<string, DataTable>();
        var databaseAlterations = new List<DatabaseAlteration>();

        var fullPath = fileProvider.GetFullPath(input.SourcePath);
        if (fullPath.EndsWith(".xlsx")) {
            // Create a table for each sheet
            using var workBook = new XLWorkbook(fullPath);

            foreach (var worksheet in workBook.Worksheets) {
                string tableName = workBook.Worksheets.Count == 1
                    ? Path.GetFileNameWithoutExtension(fullPath)
                    : worksheet.Name.Trim();

                var dataTable = ExcelHelpers.ConvertWorksheetToDatabase(worksheet);

                var dataColumns = dataTable.Columns.OfType<DataColumn>();
                var pkDataColumn = dataColumns.FirstOrDefault(column => column.ColumnName.Equals("id", StringComparison.OrdinalIgnoreCase))
                        ?? dataColumns.FirstOrDefault(column => column.ColumnName.Equals(tableName + "id", StringComparison.OrdinalIgnoreCase))
                        ?? dataColumns.FirstOrDefault(column => column.ColumnName.Equals(tableName + " id", StringComparison.OrdinalIgnoreCase));

                Column[] sourceColumns = dataColumns
                    .Select(column => new Column(column.ColumnName, StoreType.Text, isNullable: column != pkDataColumn, defaultValueSql: null, computedColumnSql: null))
                    .ToArray();

                string[] primaryKey;
                Column[] columns;
                if (pkDataColumn != null) {
                    primaryKey = new[] { pkDataColumn.ColumnName };
                    columns = sourceColumns;
                }
                else {
                    var pkColumn = new Column(tableName + "Id", StoreType.Integer, isNullable: false, defaultValueSql: null, computedColumnSql: null);
                    primaryKey = new[] { pkColumn.Name };
                    columns = sourceColumns
                        .Prepend(pkColumn)
                        .ToArray();
                }

                var createTable = new CreateTable("", tableName, null, columns, primaryKey);

                data.Add(tableName, dataTable);
                databaseAlterations.Add(createTable);
            }
        }
        else {
            return Results.BadRequest($"Unable to process the file at {input.SourcePath}.");
        }

        // Reconnect to the database and perform all operations in a single transaction
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // Apply alterations
        var sqlAlterationStatements = SqliteDatabaseScripter.ScriptAlterations(databaseAlterations);
        try {
            foreach (var sql in sqlAlterationStatements) {
                connection.Execute(sql);
            }
        }
        catch (Exception ex) {
            return Results.BadRequest(ex.Message);
        }

        // Import the data
        var database = connection.GetDatabase();
        foreach (var (tableName, dataTable) in data) {
            const string schemaName = "";
            var table = database.GetTable("", tableName);
            var records = dataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray.Cast<string?>().Select(value => Sql.Value(value)).ToArray()).ToArray();
            string[] columns = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
            RecordEndpoints.InsertSqlRecords(connection, schemaName, table, columns, records);
        }

        // Good to go
        transaction.Commit();
        connection.Close();

        return Results.Ok();
    }

    public static FileNode GetDatabaseFileNode(UserFileProvider fileProvider, FileNode fileNode) {
        string fullPath = fileProvider.GetFullPath(fileNode.Path);
        string filename = Path.GetFileName(fullPath);

        var database = new Database();
        try {
            var builder = new SqliteConnectionStringBuilder() {
                DataSource = fullPath,
                Mode = SqliteOpenMode.ReadOnly,
            };
            using var connection = new SqliteConnection(builder.ConnectionString);
            connection.Open();
            database.ContributeSqlite(connection);
        }
        catch (SqliteException) {
            // TODO: Log error
            return fileNode;
        }

        var children = database.Schemas
            .SelectMany(schema => schema.Name == string.Empty
                ? schema.Tables.Select(table => new FileNode(table.Name, fileNode.Path + "/" + table.Name, false, null))
                : schema.Tables.Select(table => new FileNode(table.Name, fileNode.Path + "/" + schema.Name + "/" + table.Name, false, null))
            )
            .OrderBy(table => table.Name)
            .ToList();

        return new FileNode(filename, fileNode.Path, true, children);
    }
}

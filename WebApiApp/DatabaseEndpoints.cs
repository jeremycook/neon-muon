using ClosedXML.Excel;
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using FileMod;
using Microsoft.Data.Sqlite;
using SqliteMod;
using SqlMod;
using System.Data;
using System.Text.RegularExpressions;

namespace WebApiApp;

public class DatabaseEndpoints {

    public static Database GetDatabase(
        UserData userData,
        string path
    ) {
        using var connection = new SqliteConnection(userData.GetConnectionString(path));
        connection.Open();

        var database = connection.GetDatabase();
        return database;
    }

    public static IResult AlterDatabase(
        UserData userData,
        string path,
        DatabaseAlteration[] databaseAlterations
    ) {
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

        using var connection = new SqliteConnection(userData.GetConnectionString(path, SqliteOpenMode.ReadWrite));
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
    public static IResult CreateTableBasedOnFileNode(UserData userData, string path, CreateTableBasedOnFileNodeInput input) {
        // Attempt to connect to the database before proceeding
        using var connection = new SqliteConnection(userData.GetConnectionString(path, SqliteOpenMode.ReadWrite));
        connection.Open();
        connection.Close();

        var data = new Dictionary<string, DataTable>();
        var databaseAlterations = new List<DatabaseAlteration>();

        var fullPath = userData.GetFullPath(input.SourcePath);
        if (fullPath.EndsWith(".xlsx")) {
            // Create a table for each sheet
            using var workBook = new XLWorkbook(fullPath);

            foreach (var worksheet in workBook.Worksheets) {
                string tableName = workBook.Worksheets.Count == 1
                    ? Path.GetFileNameWithoutExtension(fullPath)
                    : worksheet.Name.Trim();

                var dataTable = ExcelHelpers.ConvertWorksheetToDatabase(worksheet);

                var dataColumns = dataTable.Columns.OfType<DataColumn>();
                var pkDataColumn = dataColumns.FirstOrDefault(column => Regex.IsMatch(column.ColumnName, $"^({Regex.Escape(tableName)})? ?ID$", RegexOptions.IgnoreCase));

                var columns = dataColumns
                    .OrderBy(column => column == pkDataColumn ? 0 : 1)
                    .Select(column => new Column(column.ColumnName, StoreType.General, isNullable: column != pkDataColumn, defaultValueSql: null, computedColumnSql: null))
                    .ToList();

                string[] primaryKey;
                if (pkDataColumn != null) {
                    primaryKey = new[] { pkDataColumn.ColumnName };
                    if (dataTable.Rows.Cast<DataRow>().All(row => int.TryParse(row[pkDataColumn] as string, out int _))) {
                        var pkColumn = columns.Single(column => column.Name == pkDataColumn.ColumnName);
                        pkColumn.StoreType = StoreType.Integer;
                    }
                }
                else {
                    var pkColumn = new Column(tableName + "Id", StoreType.Integer, isNullable: false, defaultValueSql: null, computedColumnSql: null);
                    primaryKey = new[] { pkColumn.Name };
                    columns.Insert(0, pkColumn);
                }

                var createTable = new CreateTable("", tableName, columns.ToArray(), indexes: new[] { new TableIndex(null, TableIndexType.PrimaryKey, primaryKey) }, foreignKeys: Array.Empty<TableForeignKey>(), owner: null);

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

    public static FileNode GetDatabaseFileNode(UserData userData, FileNode fileNode) {
        string filename = fileNode.Name;

        var database = new Database();
        try {
            using var connection = new SqliteConnection(userData.GetConnectionString(fileNode.Path));
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

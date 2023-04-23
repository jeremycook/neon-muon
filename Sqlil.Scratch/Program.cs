using Microsoft.Data.Sqlite;
using Sqlil.Core;
using Sqlil.Core.Syntax;
using Sqlil.Scratch;
using System.Data.Common;

// var translation = TranslationCases.SelectIdentity;
// Console.WriteLine(translation);
// Console.WriteLine();

// var commandText = SyntaxHelpers.GenerateCommandText(((SelectStmt)translation).ToSqlSegments());
// Console.WriteLine(commandText);

// SqliteComposer sqliteComposer = new();
// var parameterizedSql = sqliteComposer.Compose(translation);

// var sqlInputs = parameterizedSql.Segments.OfType<SqlInput>().ToArray();
// var sqlOutputs = parameterizedSql.Segments.OfType<SqlOutput>().ToArray();

// var parameterNumber = 1;
// var commandText = string.Concat(parameterizedSql.Segments.Select(x => x switch {
//     SqlRaw raw => raw.Text,
//     SqlInput input => "@" + (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
//     SqlOutput output => string.Empty,
//     _ => throw new NotSupportedException(x?.ToString())
// }));

// Console.WriteLine($@"Inputs: 
//   {string.Join(",\n  ", sqlInputs)}
// Outputs:
//   {string.Join(",\n  ", sqlOutputs)}
// Command Text:");
// Console.WriteLine("  " + commandText.ReplaceLineEndings("\n  "));
// Console.WriteLine();

using (DbConnection connection = new SqliteConnection(new SqliteConnectionStringBuilder() {
    DataSource = "Scratch" + Random.Shared.Next(),
    Mode = SqliteOpenMode.Memory,
    Cache = SqliteCacheMode.Shared,
}.ConnectionString)) {
    connection.Open();

    using (var ddl = connection.CreateCommand()) {
        ddl.CommandText = """
CREATE TABLE "User" (
	"UserId"	INTEGER NOT NULL,
	"Username"	TEXT NOT NULL DEFAULT 'JeremyCook' UNIQUE,
	"IsActive"	INTEGER NOT NULL DEFAULT 1,
	"Birthday"	TEXT,
	"Created"	TEXT NOT NULL DEFAULT (datetime()),
	PRIMARY KEY("UserId" AUTOINCREMENT)
);

INSERT INTO "User" DEFAULT VALUES;
UPDATE "User" SET "Birthday" = '2023-04-21';
""";
        ddl.ExecuteNonQuery();
    }

    var (cmd, sqlColumns) = connection.CreateCommand(FindUserById(1));
    Console.WriteLine(sqlColumns);
    Console.WriteLine();
    Console.WriteLine(cmd.CommandText);
    Console.WriteLine();

    var list = connection.List(FindUserById(1));
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(list));
}

static System.Linq.Expressions.Expression<Func<IQueryable<User>>> FindUserById(int userId) {
    return () => UserContext
        .Users
        .Where(u => u.IsActive && u.UserId == userId)
        .OrderBy(u => u.UserId)
        .Select(u => u);
}
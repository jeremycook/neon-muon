using Microsoft.Data.Sqlite;
using Sqlil.Core.Syntax;
using Sqlil.Scratch;
using System.Data.Common;

var translation = TranslationCases.SelectIdentity;
// Console.WriteLine(translation);
// Console.WriteLine();

// var commandText = SyntaxHelpers.GenerateCommandText(((SelectStmt)translation).ToSqlSegments());
// Console.WriteLine(commandText);

SqliteComposer sqliteComposer = new();
var parameterizedSql = sqliteComposer.Compose(translation);

var sqlInputs = parameterizedSql.Segments.OfType<SqlInput>().ToArray();
var sqlOutputs = parameterizedSql.Segments.OfType<SqlOutput>().ToArray();

var parameterNumber = 1;
var commandText = string.Concat(parameterizedSql.Segments.Select(x => x switch {
    SqlRaw raw => raw.Text,
    SqlInput input => "@" + (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + parameterNumber++,
    SqlOutput output => string.Empty,
    _ => throw new NotSupportedException(x?.ToString())
}));

Console.WriteLine($@"Inputs: 
  {string.Join(",\n  ", sqlInputs)}
Outputs:
  {string.Join(",\n  ", sqlOutputs)}
Command Text:");
Console.WriteLine("  " + commandText.ReplaceLineEndings("\n  "));
Console.WriteLine();

using (DbConnection connection = new SqliteConnection(new SqliteConnectionStringBuilder() {
    DataSource = "Scratch" + Random.Shared.Next(),
    Mode = SqliteOpenMode.Memory,
    Cache = SqliteCacheMode.Shared,
}.ConnectionString)) {
    connection.Open();

    {
        var ddl = connection.CreateCommand();
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

    var cmd = connection.CreateCommand();
    cmd.CommandText = commandText;
    parameterNumber = 1;
    cmd.Parameters.AddRange(sqlInputs
        .Select(Input => new { Input, Param = cmd.CreateParameter() })
        .Select(x => {
            x.Param.ParameterName = (x.Input.SuggestedName != string.Empty ? x.Input.SuggestedName : "p") + parameterNumber++;
            x.Param.Value = true;
            return x.Param;
        })
        .ToArray());

    using var reader = cmd.ExecuteReader();

    var list = new List<object[]>();
    while (reader.Read()) {
        var values = new object[sqlOutputs.Length];
        reader.GetValues(values);
        for (int j = 0; j < values.Length; j++) {
            var val = values[j];
            var sqlOutput = sqlOutputs[j];
            if (val == DBNull.Value) {
                val = null!;
            }
            else if (val is string text) {
                if (sqlOutput.Type.IsAssignableTo(typeof(Guid?))) {
                    val = Guid.Parse(text);
                }
                else if (sqlOutput.Type.IsAssignableTo(typeof(DateOnly?))) {
                    val = DateOnly.Parse(text);
                }
                else if (sqlOutput.Type.IsAssignableTo(typeof(DateTime?))) {
                    var dt = DateTime.Parse(text);
                    val = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
                else {
                    // No change needed
                }
            }
            else {
                val = Convert.ChangeType(val, sqlOutputs[j].Type);
            }
            values[j] = val;
        }
        list.Add(values);
    }
    System.Text.Json.JsonSerializer.Serialize(list).Dump();
}


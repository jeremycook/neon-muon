using Sqlil.Core.Syntax;
using Sqlil.Scratch;

var translation = TranslationCases.Where;
// Console.WriteLine(translation);
// Console.WriteLine();

// var commandText = SyntaxHelpers.GenerateCommandText(((SelectStmt)translation).ToSqlSegments());
// Console.WriteLine(commandText);

SqliteComposer sqliteComposer = new();
var parameterizedSql = sqliteComposer.Compose(translation);

var i = 1;
Console.WriteLine($@"Outputs: 
  {string.Join(",\n  ", parameterizedSql.Segments.OfType<SqlOutput>())}
Inputs:
  {string.Join(",\n  ", parameterizedSql.Segments.OfType<SqlInput>())}
Command Text:");
Console.WriteLine("  " + string.Concat(parameterizedSql.Segments.Select(x => x switch {
    SqlRaw raw => raw.Text,
    SqlInput input => "@" + (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + i++,
    SqlOutput output => string.Empty,
    _ => throw new NotSupportedException(x?.ToString())
})).ReplaceLineEndings("\n  "));
Console.WriteLine();

// System.Text.Json.JsonSerializer.Serialize(translation).Dump();

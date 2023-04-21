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
Console.WriteLine("(" + string.Join(", ", parameterizedSql.Segments.OfType<SqlOutput>()) + ") Exec(" + string.Join(", ", parameterizedSql.Segments.OfType<SqlInput>()) + ") {");
Console.WriteLine("  " + string.Concat(parameterizedSql.Segments.Select(x => x switch {
    SqlText text => text.Text,
    SqlInput sql => "@" + (sql.SuggestedName != string.Empty ? sql.SuggestedName : "p") + i++,
    SqlOutput sql => "#" + (sql.SuggestedName != string.Empty ? sql.SuggestedName : "p") + i++,
    _ => throw new NotSupportedException(x?.ToString())
})).ReplaceLineEndings("\n  "));
Console.WriteLine("}");
Console.WriteLine();

// System.Text.Json.JsonSerializer.Serialize(translation).Dump();

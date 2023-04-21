using Sqlil.Core.Syntax;
using Sqlil.Scratch;

var translation = TranslationCases.Where.Dump();
Console.WriteLine(translation);

var commandText = SyntaxHelpers.GenerateCommandText(((SelectStmt)translation).ToSqlSegments());
Console.WriteLine(commandText);

// System.Text.Json.JsonSerializer.Serialize(translation).Dump();

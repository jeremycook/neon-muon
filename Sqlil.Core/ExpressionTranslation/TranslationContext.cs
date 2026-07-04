using Sqlil.Core.Syntax;
namespace Sqlil.Core.ExpressionTranslation;

public readonly record struct TranslationContext(TableName? ParameterName) { }

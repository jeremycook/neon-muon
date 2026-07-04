using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public readonly record struct TranslationContext(
    TableName? ParameterName,
    Dictionary<ParameterExpression, TableName>? OuterParameters = null
) { }

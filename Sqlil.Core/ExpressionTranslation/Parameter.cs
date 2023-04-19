using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public static class Parameter {
    public static ExprBindParameter Translate(ParameterExpression expression) {
        return ExprBindParameter.Create(expression.Type, expression.Name!);
    }
}
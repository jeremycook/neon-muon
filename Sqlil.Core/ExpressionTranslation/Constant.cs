using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public static class Constant {
    public static object Translate(ConstantExpression expression) {
        if (expression.Value is int integer) {
            return ExprLiteralInteger.Create(integer);
        }

        else if (expression.Value is string text) {
            return ExprLiteralString.Create(text);
        }

        else {
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public static class Constant {
    public static object Translate(ConstantExpression expression) {
        if (expression.Type.IsPrimitive ||
            expression.Type == typeof(string)) {
            return ExprBindConstant.Create(expression.Type, expression.Value);
        }

        else {
            throw new ExpressionNotSupportedException($"The {expression.Type} type is not supported.", expression);
        }
    }
}
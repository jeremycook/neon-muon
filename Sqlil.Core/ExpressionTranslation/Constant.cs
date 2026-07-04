using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    protected virtual object Constant(ConstantExpression expression) {
        if (expression.Type.IsPrimitive ||
            expression.Type == typeof(string) ||
            expression.Type == typeof(decimal) ||
            expression.Type == typeof(Guid) ||
            expression.Type == typeof(DateTime) ||
            expression.Type == typeof(DateOnly) ||
            expression.Type == typeof(TimeOnly) ||
            expression.Type == typeof(byte[]) ||
            expression.Type.IsEnum) {
            return ExprBindConstant.Create(expression.Type, expression.Value);
        }

        // Handle nullable versions of the above types
        var underlyingType = Nullable.GetUnderlyingType(expression.Type);
        if (underlyingType != null) {
            return ExprBindConstant.Create(expression.Type, expression.Value);
        }

        else {
            throw new ExpressionNotSupportedException($"The {expression.Type} type is not supported.", expression);
        }
    }
}

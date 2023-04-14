using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

internal class Unary {
    internal static object Translate(UnaryExpression expression, TranslationContext context) {

        if (ExprUnary.OperatorMap.ContainsKey(expression.NodeType)) {
            var operand = (Expr)AnExpression.Translate(expression.Operand, context);

            return ExprUnary.Create(expression.NodeType, operand);
        }

        throw new ExpressionNotSupportedException(expression);
    }
}

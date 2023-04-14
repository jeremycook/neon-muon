using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

internal class Binary {
    internal static object Translate(BinaryExpression expression, TranslationContext context) {

        var left = (Expr)AnExpression.Translate(expression.Left, context);
        var right = (Expr)AnExpression.Translate(expression.Right, context);

        return ExprBinary.Create(expression.NodeType, left, right);
    }
}
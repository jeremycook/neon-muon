using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

internal class New {
    internal static object Translate(NewExpression expression, TranslationContext context) {
        var result = StableList.Create<Expr>(
            expression.Arguments
                .Select(arg => (Expr)AnExpression.Translate(arg, context))
                .ToArray()
        );
        return result;
    }
}
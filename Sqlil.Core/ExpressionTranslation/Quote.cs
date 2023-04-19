using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

internal class Quote {
    internal static object Translate(UnaryExpression expression, TranslationContext context) {
        return AnExpression.Translate(expression.Operand, context);
    }
}

using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

internal class New {
    internal static object Translate(NewExpression expression, TranslationContext context) {
        var result = StableList.Create<ResultColumn>(
            expression.Arguments
                .Select((arg, i) => ResultColumnExpr.Create((Expr)AnExpression.Translate(arg, context), expression.Members?[i].Name))
                .ToArray()
        );
        return result;
    }
}
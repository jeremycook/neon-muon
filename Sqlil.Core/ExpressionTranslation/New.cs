using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

internal class New {
    internal static object Translate(NewExpression expression, TranslationContext context) {
        var result = StableList.Create<ResultColumn>(
            expression.Arguments
                .Select((arg, i) => ResultColumnExpr.Create(
                    Expr: (Expr)AnExpression.Translate(arg, context),
                    ColumnAlias: ColumnName.Create(expression.Members![i].Name, AnExpression.GetMemberType(expression.Members[i]))
                // AnExpression.GetColumnName(arg)
                // Identifier.Create(expression.Members![i].Name, expression.Members![i].Type)
                ))
                .ToArray()
        );
        return result;
    }
}
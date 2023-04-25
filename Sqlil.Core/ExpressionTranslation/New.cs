using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    public virtual object New(NewExpression expression, TranslationContext context) {
        var result = StableList.Create<ResultColumn>(
            expression.Arguments
                .Select((arg, i) => ResultColumnExpr.Create(
                    Expr: (Expr)Translate(arg, context),
                    ColumnAlias: ColumnName.Create(expression.Members![i].Name, GetMemberType(expression.Members[i]))
                // AnExpression.GetColumnName(arg)
                // Identifier.Create(expression.Members![i].Name, expression.Members![i].Type)
                ))
                .ToArray()
        );
        return result;
    }
}
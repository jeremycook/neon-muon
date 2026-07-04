using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    public virtual object New(NewExpression expression, TranslationContext context) {
        var result = StableList.Create<ResultColumn>(
            expression.Arguments
                .Select((arg, i) => {
                    var translated = Translate(arg, context);
                    var expr = ExtractExpr(translated) ?? (Expr)translated;
                    return ResultColumnExpr.Create(
                        Expr: expr,
                        ColumnAlias: ColumnName.Create(expression.Members![i].Name, GetMemberType(expression.Members[i]))
                    );
                })
                .ToArray()
        );
        return result;
    }
}
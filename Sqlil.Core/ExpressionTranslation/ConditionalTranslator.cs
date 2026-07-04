using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    protected virtual Expr Conditional(ConditionalExpression expression, TranslationContext context) {
        var test = (Expr)Translate(expression.Test, context);
        var ifTrue = (Expr)Translate(expression.IfTrue, context);
        var ifFalse = (Expr)Translate(expression.IfFalse, context);

        var whenClauses = StableList.Create((When: test, Then: ifTrue));
        return ExprCase.Create(whenClauses, Else: ifFalse);
    }
}

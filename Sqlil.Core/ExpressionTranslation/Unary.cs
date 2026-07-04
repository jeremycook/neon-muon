using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    public virtual object Unary(UnaryExpression expression, TranslationContext context) {
        var operand = (Expr)Translate(expression.Operand, context);
        return ExprUnary.Create(expression.NodeType, operand);
    }
}

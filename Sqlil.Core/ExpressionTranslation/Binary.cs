using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    public virtual object Binary(BinaryExpression expression, TranslationContext context) {

        var left = (Expr)Translate(expression.Left, context);
        var right = (Expr)Translate(expression.Right, context);

        return ExprBinary.Create(expression.NodeType, left, right);
    }
}
using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    public virtual object Binary(BinaryExpression expression, TranslationContext context) {

        var left = (Expr)Translate(expression.Left, context);
        var right = (Expr)Translate(expression.Right, context);

        // Detect null comparisons and convert to IS NULL / IS NOT NULL
        if (expression.NodeType is ExpressionType.Equal or ExpressionType.NotEqual) {
            var leftConst = expression.Left as ConstantExpression;
            var rightConst = expression.Right as ConstantExpression;

            // Handle Convert wrappers around constants (nullable lifting)
            if (leftConst == null && expression.Left is UnaryExpression { Operand: ConstantExpression lc })
                leftConst = lc;
            if (rightConst == null && expression.Right is UnaryExpression { Operand: ConstantExpression rc })
                rightConst = rc;

            bool leftIsNull = leftConst != null && leftConst.Value == null;
            bool rightIsNull = rightConst != null && rightConst.Value == null;

            if (leftIsNull || rightIsNull) {
                var nonNullExpr = leftIsNull ? right : left;
                if (expression.NodeType == ExpressionType.Equal) {
                    return ExprIsNull.Create(nonNullExpr, IsNot: false);
                }
                else {
                    return ExprIsNull.Create(nonNullExpr, IsNot: true);
                }
            }
        }

        return ExprBinary.Create(expression.NodeType, left, right);
    }
}

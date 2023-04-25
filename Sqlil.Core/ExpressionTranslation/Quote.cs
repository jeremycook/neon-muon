using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    public virtual object Quote(UnaryExpression expression, TranslationContext context) {
        return Translate(expression.Operand, context);
    }
}

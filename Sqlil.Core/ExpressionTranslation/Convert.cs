using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    protected virtual object Convert(UnaryExpression expression, TranslationContext context) {
        // TODO? Encode the conversion
        var result = Translate(expression.Operand, context);
        return result;
    }
}
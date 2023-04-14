using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public static class Lambda {
    public static object Translate(LambdaExpression expression, TranslationContext context) {
        // if (expression.Parameters.Count == 1 && expression.Body == expression.Parameters[0]) {
        //     // o => o
        //     var prefix = expression.Parameters[0].Name;
        //     var elements = GetResultColumn(expression.ReturnType);
        //     foreach (var element in elements) {
        //         yield return SqlIdentifier.Create(prefix, element);
        //     }
        // }
        // else {
        //     var elements = Process(expression.Body, expression);
        //     foreach (var element in elements) {
        //         yield return element;
        //     }
        // }
        // return SelectStmt.Create()

        object result = AnExpression.Translate(expression.Body, context);
        return result;
    }
}

using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public static class Lambda {
    public static object Translate(LambdaExpression expression, TranslationContext context) {
        if (expression.Parameters.Count == 1 && expression.Parameters[0] == expression.Body) {
            // Input:  o => o
            // Output: o.Prop1, o.Prop2

            Identifier tableName =
                context.ParameterName ??
                expression.Parameters[0].Name ??
                throw new NullReferenceException("table name");

            var result = StableList.Create<ResultColumn>(
                expression.ReturnType.GetProperties()
                    .Select((prop, i) => {
                        return ResultColumnExpr.Create(ExprColumn.Create(tableName, prop.Name));
                    })
                    .ToArray()
            );
            return result;
        }
        else {
            object result = AnExpression.Translate(expression.Body, context);
            return result;
        }
    }
}

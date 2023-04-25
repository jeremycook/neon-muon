using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {

    protected virtual object Lambda(LambdaExpression expression, TranslationContext context) {
        if (expression.Parameters.Count == 1 && expression.Parameters[0] == expression.Body) {
            // In:  o => o
            // Out: o.Prop1, o.Prop2

            TableName tableName =
                context.ParameterName ??
                TableName.Create(expression.Parameters[0].Name ?? string.Empty, expression.Parameters[0].Type);

            var result = StableList.Create<ResultColumn>(
                expression.ReturnType.GetProperties()
                    .Select((prop, i) => {
                        return ResultColumnExpr.Create(ExprColumn.Create(tableName, ColumnName.Create(prop.Name, prop.PropertyType)));
                    })
                    .ToArray()
            );
            return result;
        }
        else {
            object result = Translate(expression.Body, context);
            return result;
        }
    }
}

using Sqlil.Core.Syntax;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    protected virtual InsertStmt Insert(MethodCallExpression expression, TranslationContext context) {
        // IQueryable<T>.Insert<T>(source, value)
        var source = Translate(expression.Arguments[0], context);
        var valueArg = expression.Arguments[1];

        TableName tableName = source switch {
            TableOrSubqueryTable table => table.TableName,
            SelectStmt selectStmt => GetTableNameFromSelectStmt(selectStmt),
            _ => throw new ExpressionNotSupportedException($"Insert source not supported: {source.GetType()}."),
        };

        var (columnNames, values) = ExtractInsertValues(valueArg, tableName.Type);

        return InsertStmt.Create(tableName, columnNames, values);
    }

    private static (StableList<ColumnName> ColumnNames, StableList<StableList<Expr>> Values) ExtractInsertValues(Expression valueArg, Type tableType) {
        var properties = tableType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        // Unwrap Convert nodes
        var unwrapped = valueArg is UnaryExpression unary ? unary.Operand : valueArg;

        // Handle New expression: new User(0, "Alice", true, ...)
        if (unwrapped is NewExpression newExpr) {
            var ctor = newExpr.Constructor;
            var ctorParams = ctor.GetParameters();

            var columnNames = StableList.Create(ctorParams.Select(p =>
                ColumnName.Create(p.Name!, p.ParameterType)
            ).ToArray());

            var exprValues = StableList.Create(newExpr.Arguments.Select(arg =>
                (Expr)TranslateInsertValue(arg)
            ).ToArray());

            var values = StableList.Create(exprValues);
            return (columnNames, values);
        }

        // Handle constant value: Insert(new User(...)) where the value is captured
        object? value = null;
        if (unwrapped is ConstantExpression constant) {
            value = constant.Value;
        }

        if (value != null) {
            var columnNames = StableList.Create(properties.Select(p =>
                ColumnName.Create(p.Name, p.PropertyType)
            ).ToArray());

            var exprValues = StableList.Create(properties.Select(p => {
                var propValue = p.GetValue(value);
                return (Expr)ExprBindConstant.Create(p.PropertyType, propValue);
            }).ToArray());

            var values = StableList.Create(exprValues);
            return (columnNames, values);
        }

        throw new ExpressionNotSupportedException("Insert value must be a constant or new expression.");
    }

    private static Expr TranslateInsertValue(Expression expression) {
        return expression switch {
            ConstantExpression constant => ExprBindConstant.Create(constant.Type, constant.Value),
            UnaryExpression unary => TranslateInsertValue(unary.Operand),
            NewExpression => EvaluateExpression(expression),
            _ => throw new ExpressionNotSupportedException($"Insert value expression not supported: {expression.GetType()}."),
        };
    }

    private static Expr EvaluateExpression(Expression expression) {
        var value = Expression.Lambda(expression).Compile().DynamicInvoke();
        return ExprBindConstant.Create(expression.Type, value);
    }

    private static TableName GetTableNameFromSelectStmt(SelectStmt selectStmt) {
        if (selectStmt.SelectCores.Count == 1 &&
            selectStmt.SelectCores[0] is SelectCoreNormal core &&
            core.TableOrSubqueries.Count == 1 &&
            core.TableOrSubqueries[0] is TableOrSubqueryTable table) {
            return table.TableName;
        }
        throw new ExpressionNotSupportedException("Could not determine table name from SelectStmt.");
    }
}

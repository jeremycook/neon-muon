using Sqlil.Core.Syntax;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    protected virtual UpdateStmt Update(MethodCallExpression expression, TranslationContext context) {
        // IQueryable<T>.Update<T>(source, map)
        var source = Translate(expression.Arguments[0], context);
        var mapArg = expression.Arguments[1];

        TableName tableName;
        Expr? where = null;

        switch (source) {
            case TableOrSubqueryTable table:
                tableName = table.TableName;
                break;
            case SelectStmt selectStmt:
                tableName = GetTableNameFromSelectStmt(selectStmt);
                where = GetWhereFromSelectStmt(selectStmt);
                break;
            case SelectCoreNormal core:
                tableName = GetTableNameFromSelectCore(core);
                where = core.Where;
                break;
            default:
                throw new ExpressionNotSupportedException($"Update source not supported: {source.GetType()}.");
        }

        // Unwrap the map lambda
        LambdaExpression? mapLambda = mapArg switch {
            LambdaExpression lambda => lambda,
            UnaryExpression { Operand: LambdaExpression innerLambda } => innerLambda,
            _ => null,
        };

        if (mapLambda == null) {
            throw new ExpressionNotSupportedException("Update map must be a lambda expression.");
        }

        var setClauses = ExtractSetClauses(mapLambda, tableName.Type);

        // Strip table qualifiers from WHERE clause (SQLite UPDATE/DELETE don't support table aliases)
        if (where != null) {
            where = StripTableQualifiers(where);
        }

        return UpdateStmt.Create(tableName, setClauses, where);
    }

    private static StableList<(ColumnName ColumnName, Expr Value)> ExtractSetClauses(LambdaExpression mapLambda, Type tableType) {
        var body = mapLambda.Body;

        // Handle New expression: u => new User(arg0, arg1, ...)
        if (body is NewExpression newExpr) {
            var ctor = newExpr.Constructor;
            var parameters = ctor.GetParameters();

            var setClauses = new List<(ColumnName, Expr)>();
            for (int i = 0; i < parameters.Length; i++) {
                var paramName = parameters[i].Name!;
                var paramType = parameters[i].ParameterType;
                var value = TranslateMapValue(newExpr.Arguments[i]);
                setClauses.Add((ColumnName.Create(paramName, paramType), value));
            }
            return setClauses.ToStableList();
        }

        // Handle MemberInit expression: u => new User { Username = "Bob" }
        if (body is MemberInitExpression memberInit) {
            var setClauses = new List<(ColumnName, Expr)>();
            foreach (var binding in memberInit.Bindings) {
                if (binding is MemberAssignment assignment) {
                    var propName = assignment.Member.Name;
                    var propType = assignment.Member is PropertyInfo prop ? prop.PropertyType : typeof(object);
                    var value = TranslateMapValue(assignment.Expression);
                    setClauses.Add((ColumnName.Create(propName, propType), value));
                }
            }
            return setClauses.ToStableList();
        }

        throw new ExpressionNotSupportedException("Update map body must be a New or MemberInit expression.");
    }

    private static Expr TranslateMapValue(Expression expression) {
        return expression switch {
            ConstantExpression constant => ExprBindConstant.Create(constant.Type, constant.Value),
            UnaryExpression { Operand: ConstantExpression innerConstant } =>
                ExprBindConstant.Create(innerConstant.Type, innerConstant.Value),
            MemberExpression member => TranslateMapMemberAccess(member),
            _ => throw new ExpressionNotSupportedException($"Update map value not supported: {expression.GetType()}."),
        };
    }

    private static Expr TranslateMapMemberAccess(MemberExpression member) {
        if (member.Expression is ParameterExpression parameter) {
            // Don't include table qualifier - SQLite UPDATE SET doesn't support aliases
            return ExprColumn.Create(
                ColumnName.Create(member.Member.Name, GetMemberTypeFromMember(member.Member))
            );
        }
        throw new ExpressionNotSupportedException($"Update map member access not supported: {member.GetType()}.");
    }

    private static Type GetMemberTypeFromMember(MemberInfo memberInfo) {
        return memberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.PropertyType,
            FieldInfo fieldInfo => fieldInfo.FieldType,
            _ => throw new NotImplementedException(memberInfo?.ToString())
        };
    }

    private static Expr? GetWhereFromSelectStmt(SelectStmt selectStmt) {
        if (selectStmt.SelectCores.Count == 1 &&
            selectStmt.SelectCores[0] is SelectCoreNormal core) {
            return core.Where;
        }
        return null;
    }

    private static TableName GetTableNameFromSelectCore(SelectCoreNormal core) {
        if (core.TableOrSubqueries.Count == 1 &&
            core.TableOrSubqueries[0] is TableOrSubqueryTable table) {
            return table.TableName;
        }
        throw new ExpressionNotSupportedException("Could not determine table name from SelectCore.");
    }
}

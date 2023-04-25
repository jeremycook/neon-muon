using Sqlil.Core.Syntax;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    public virtual object Translate(Expression expression, TranslationContext context) {
        var result = expression.NodeType switch {
            ExpressionType.Constant => Constant((ConstantExpression)expression),
            ExpressionType.Convert => Convert((UnaryExpression)expression, context),
            ExpressionType.Lambda => Lambda((LambdaExpression)expression, context),
            ExpressionType.Parameter => Parameter((ParameterExpression)expression),
            ExpressionType.Call => Call((MethodCallExpression)expression, context),
            ExpressionType.MemberAccess => MemberAccess((MemberExpression)expression, context),
            ExpressionType.Quote => Quote((UnaryExpression)expression, context),
            ExpressionType.New => New((NewExpression)expression, context),
            _ => expression switch {
                BinaryExpression binary => Binary(binary, context),
                UnaryExpression unary => Unary(unary, context),
                _ => throw new ExpressionNotSupportedException(expression)
            }
        };
        return result;
    }

    protected virtual TableName GetTableName(Expression expression) {
        TableName result = expression switch {

            ParameterExpression parameter =>
                parameter.Name is not null && parameter.Name.StartsWith('<')
                    ? TableName.Create("__anon" + (uint)parameter.Name.GetHashCode(), parameter.Type)
                    : TableName.Create(parameter.Name ?? string.Empty, parameter.Type),

            UnaryExpression unary =>
                GetTableName(unary.Operand),

            LambdaExpression lambda =>
                GetTableName(lambda.Parameters[0]),

            MemberExpression member when member.Expression is not null =>
                GetTableName(member.Expression),

            // NewExpression newExpression => newExpression.

            _ => throw new ExpressionNotSupportedException(expression),
        };

        return result;
    }

    protected virtual ColumnName GetColumnName(Expression expression) {
        ColumnName result = expression switch {

            ParameterExpression parameter =>
                parameter.Name is not null && parameter.Name.StartsWith('<')
                    ? ColumnName.Create("__anon" + parameter.Name.GetHashCode(), parameter.Type)
                    : ColumnName.Create(parameter.Name ?? string.Empty, parameter.Type),

            UnaryExpression unary =>
                GetColumnName(unary.Operand),

            LambdaExpression lambda =>
                GetColumnName(lambda.Parameters[0]),

            MemberExpression member when member.Expression is not null =>
                GetColumnName(member.Expression),

            // NewExpression newExpression => newExpression.

            _ => throw new ExpressionNotSupportedException(expression),
        };

        return result;
    }

    /// <summary>
    /// Returns the Type that implementation implements that most closely matches implementedType, or null if not found.
    /// </summary>
    protected virtual Type? IsType(Type implementation, Type implementedType) {
        if (implementedType.IsGenericType) {
            if (implementedType.IsInterface) {
                var result = implementation
                    .GetInterfaces()
                    .Prepend(implementation)
                    .SingleOrDefault(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == implementedType);
                return result;
            }
            else {
                var type = implementation;
                while (type != typeof(object)) {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == implementedType) {
                        return type;
                    }
                    type = type?.BaseType ?? typeof(object);
                }
                return null;
            }
            throw new ArgumentException($"The {nameof(implementedType)} is not an interface type.", nameof(implementedType));
        }
        else {
            return implementation.IsAssignableTo(implementedType)
                ? implementation
                : null;
        }
    }

    protected virtual ExprBindParameter Parameter(ParameterExpression expression) {
        return ExprBindParameter.Create(expression.Type, expression.Name!);
    }

    protected virtual Type GetMemberType(MemberInfo memberInfo) {
        var result = memberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.PropertyType,
            FieldInfo fieldInfo => fieldInfo.FieldType,
            _ => throw new NotImplementedException(memberInfo?.ToString())
        };
        return result;
    }
}

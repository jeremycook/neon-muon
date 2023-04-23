using Sqlil.Core.Syntax;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlil.Core.ExpressionTranslation;

public static class AnExpression {
    public static object Translate(Expression expression, TranslationContext context) {
        var result = expression.NodeType switch {
            ExpressionType.Constant => Constant.Translate((ConstantExpression)expression),
            ExpressionType.Convert => Convert((UnaryExpression)expression, context),
            ExpressionType.Lambda => Lambda.Translate((LambdaExpression)expression, context),
            ExpressionType.Parameter => Parameter.Translate((ParameterExpression)expression),
            ExpressionType.Call => Call.Translate((MethodCallExpression)expression, context),
            ExpressionType.MemberAccess => MemberAccess.Translate((MemberExpression)expression, context),
            ExpressionType.Quote => Quote.Translate((UnaryExpression)expression, context),
            ExpressionType.New => New.Translate((NewExpression)expression, context),
            _ => expression switch {
                BinaryExpression binary => Binary.Translate(binary, context),
                UnaryExpression unary => Unary.Translate(unary, context),
                _ => throw new ExpressionNotSupportedException(expression)
            }
        };
        return result;
    }

    private static object Convert(UnaryExpression expression, TranslationContext context) {
        // TODO? Encode the conversion
        var result = Translate(expression.Operand, context);
        return result;
    }

    internal static TableName GetParameterName(Expression expression) {
        TableName result = expression switch {

            ParameterExpression parameter =>
                parameter.Name is not null && parameter.Name.StartsWith('<')
                    ? TableName.Create("__HC" + parameter.Name.GetHashCode(), parameter.Type)
                    : TableName.Create(parameter.Name ?? string.Empty, parameter.Type),

            UnaryExpression unary =>
                GetParameterName(unary.Operand),

            LambdaExpression lambda =>
                GetParameterName(lambda.Parameters[0]),

            MemberExpression member when member.Expression is not null =>
                GetParameterName(member.Expression),

            // NewExpression newExpression => newExpression.

            _ => throw new ExpressionNotSupportedException(expression),
        };

        return result;
    }

    internal static ColumnName GetColumnName(Expression expression) {
        ColumnName result = expression switch {

            ParameterExpression parameter =>
                parameter.Name is not null && parameter.Name.StartsWith('<')
                    ? ColumnName.Create("__HC" + parameter.Name.GetHashCode(), parameter.Type)
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
    internal static Type? IsType(Type implementation, Type implementedType) {
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

    internal static Type GetMemberType(MemberInfo memberInfo) {
        var result = memberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.PropertyType,
            FieldInfo fieldInfo => fieldInfo.FieldType,
            _ => throw new NotImplementedException(memberInfo?.ToString())
        };
        return result;
    }
}

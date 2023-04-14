using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public static class AnExpression {
    public static object Translate(Expression expression, TranslationContext context) {
        var result = expression.NodeType switch {
            ExpressionType.Constant => Constant.Translate((ConstantExpression)expression),
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

    internal static string GetParameterName(Expression expression) {
        var result = expression switch {

            ParameterExpression parameter => parameter.Name is not null && parameter.Name.StartsWith('<')
                ? "__" + parameter.Name.GetHashCode()
                : parameter.Name ?? string.Empty,

            UnaryExpression unary => GetParameterName(unary.Operand),

            LambdaExpression lambda => GetParameterName(lambda.Parameters[0]),

            // MemberExpression member when member.Expression is not null => GetParameterName(member.Expression),

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
}

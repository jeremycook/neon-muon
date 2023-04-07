using System.Linq.Expressions;
using System.Reflection;

namespace DataCore;

public class SqlExpressionTranslator {
    public virtual Sql Translate(Expression node) {
        switch (node) {
            case LambdaExpression lambda:
                if (lambda.Parameters[0] == lambda.Body) {
                    if (lambda.Body.Type.Name.StartsWith("ValueTuple`")) {
                        throw new NotImplementedException("ValueTuple support");
                        //return Sql.Join(", ", lambda.Body.Type.GetProperties()
                        //    .SelectMany((prop, i) => {
                        //        var underType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                        //        if (underType.IsPrimitive || underType == typeof(Guid) || underType == typeof(string)) {
                        //            return new[] { prop.Name };
                        //        }

                        //        Sql.Identifier(lambda.Body.Type.Name, "*")

                        //    });
                    }
                    else {
                        return Sql.Identifier(lambda.Body.Type.Name, "*");
                    }
                }
                else {
                    return Translate(lambda.Body);
                }

            case MemberExpression member:
                switch (member.Expression) {
                    case ParameterExpression parameter:
                        // Since the parameter name is generally arbitrary (as in `o => o.Something`)
                        // we will prefix with the type name instead. A more powerful translator would use
                        // additional context to decide what the prefix of the identifier should be.
                        return Sql.Identifier(parameter.Type.Name, member.Member.Name);

                    case ConstantExpression constant when member.Member is FieldInfo fi:
                        return Sql.Value(fi.GetValue(constant.Value));

                    case MemberExpression innerMember:
                        var sql = Translate(innerMember);
                        return sql;
                }
                break;

            case BinaryExpression binary:
                return Sql.Interpolate($"({Translate(binary.Left)} {GetBinaryOperation(binary)} {Translate(binary.Right)})");

            case ConstantExpression constant:
                return Sql.Value(constant.Value);

            case MemberInitExpression memberInit:
                return Sql.Join(", ", memberInit.Bindings.Select(GetBinding).Cast<object?>());

            case MethodCallExpression methodCall:

                if (methodCall.Object is not null) {
                    // Instance method call

                    if (methodCall.Type == typeof(string) &&
                        methodCall.Arguments.Count == 0) {
                        return methodCall.Method.Name switch {
                            nameof(string.ToLower) => Sql.Interpolate($"lower({Translate(methodCall.Object)})"),
                            nameof(string.ToUpper) => Sql.Interpolate($"upper({Translate(methodCall.Object)})"),
                            _ => throw new NotImplementedException(),
                        };
                    }
                }
                else {
                    // Static method call
                    return Sql.Join(", ", methodCall.Arguments.Select(Translate));
                }

                break;

            case NewExpression newExpression:
                return Sql.Join(", ", newExpression.Constructor!.GetParameters().Select((p, i) => Sql.Interpolate($"{Translate(newExpression.Arguments[i])} {Sql.Identifier(p.Name!)}")));

            case ParameterExpression parameter:
                // TODO: Should these be handled differently?
                return Sql.Empty;
        }

        throw new NotSupportedException($"{node.GetType().Name}: {node.NodeType}");
    }

    private static Sql GetBinaryOperation(BinaryExpression binary) {
        var op = binary.NodeType switch {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"{binary.NodeType}"),
        };
        return Sql.Raw(op);
    }

    private Sql GetBinding(MemberBinding binding) {
        return binding switch {
            MemberAssignment assignment => Sql.Interpolate($"{Sql.Identifier(binding.Member.Name)} = {Translate(assignment.Expression)}"),
            _ => throw new NotSupportedException($"{binding.GetType()}: {binding.BindingType}"),
        };
    }

    private static bool IsPrimitive(Type type) {
        return type.IsPrimitive || type == typeof(Guid) || type == typeof(string) || Nullable.GetUnderlyingType(type)?.IsPrimitive == true;
    }

    public static IEnumerable<string> GetResultColumn(Type type) {
        var under = Nullable.GetUnderlyingType(type) ?? type;
        if (under.Name.StartsWith("ValueTuple`")) {
            return under.GetFields().SelectMany(f => IsPrimitive(f.FieldType)
                ? new[] { f.Name }
                : GetResultColumn(f.FieldType).Select(name => f.Name + "." + name));
        }
        else if (IsPrimitive(under)) {
            throw new NotSupportedException();
        }
        else {
            return under.GetProperties().Select(prop => prop.Name);
        }
    }
}

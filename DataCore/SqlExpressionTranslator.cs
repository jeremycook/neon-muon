using System.Linq.Expressions;
using System.Reflection;

namespace DataCore;

public class SqlExpressionTranslator {
    public virtual Sql Translate(Expression node) {
        switch (node) {
            case LambdaExpression lambda:
                return Translate(lambda.Body);

            case MemberExpression member:
                switch (member.Expression) {
                    case ParameterExpression parameter:
                        // Since the parameter name is generally arbitrary (as in `o => o.Something`)
                        // we will prefix with the type name instead. A more complicated translator would use
                        // additional context to decide what the prefix of the identifier should be.
                        return Sql.Identifier(parameter.Type.Name, member.Member.Name);

                    case ConstantExpression constant when member.Member is FieldInfo fi:
                        return Sql.Value(fi.GetValue(constant.Value));
                }
                break;

            case BinaryExpression binary:
                return Sql.Interpolate($"({Translate(binary.Left)} {GetBinaryOperation(binary)} {Translate(binary.Right)})");

            case ConstantExpression constant:
                return Sql.Value(constant.Value);

            case MemberInitExpression memberInit:
                return Sql.Join(", ", memberInit.Bindings.Select(GetBinding).Cast<object?>());

            case ParameterExpression parameter:
                // TODO: How should these be handled.
                break;
        }

        throw new NotSupportedException($"{node.GetType()}: {node.NodeType}");
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
}

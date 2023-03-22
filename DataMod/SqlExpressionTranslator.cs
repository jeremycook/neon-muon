using System.Linq.Expressions;
using System.Reflection;

namespace DataMod;

public class SqlExpressionTranslator
{
    public virtual Sql Visit(Expression node)
    {
        switch (node)
        {
            case LambdaExpression lambda:
                return Visit(lambda.Body);

            case MemberExpression member:
                switch (member.Expression)
                {
                    case ParameterExpression parameter:
                        return Sql.Identifier(parameter.Name, member.Member.Name);

                    case ConstantExpression constant when member.Member is FieldInfo fi:
                        return Sql.Value(fi.GetValue(constant.Value));
                }
                break;

            case BinaryExpression binary:
                return Sql.Interpolate($"({Visit(binary.Left)} {GetBinaryOperation(binary)} {Visit(binary.Right)})");

            case ConstantExpression constant:
                return Sql.Value(constant.Value);

            case MemberInitExpression memberInit:
                return Sql.Join(", ", memberInit.Bindings.Select(GetBinding).Cast<object?>());

            case ParameterExpression parameter:
                return Sql.Empty;
        }

        throw new NotSupportedException($"{node.GetType()}: {node.NodeType}");
    }

    private static Sql GetBinaryOperation(BinaryExpression binary)
    {
        var op = binary.NodeType switch
        {
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

    private Sql GetBinding(MemberBinding binding)
    {
        return binding switch
        {
            MemberAssignment assignment => Sql.Interpolate($"{Sql.Identifier(binding.Member.Name)} = {Visit(assignment.Expression)}"),
            _ => throw new NotSupportedException($"{binding.GetType()}: {binding.BindingType}"),
        };
    }
}

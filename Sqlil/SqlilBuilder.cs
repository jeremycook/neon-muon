using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Sqlil;

public class SqlilBuilder {
    public IEnumerable<ISqlil> Build(LambdaExpression expression) {
        return Lambda(expression, null);
    }

    static IEnumerable<ISqlil> New(NewExpression expression, Expression context) {
        foreach (var arg in expression.Arguments) {
            var results = Process(arg, expression);
            foreach (var result in results)
                yield return result;
        }
    }

    static IEnumerable<ISqlil> MemberAccess(MemberExpression expression, Expression context) {
        if (expression.Member is PropertyInfo property) {
            if (IsType(property.PropertyType, typeof(IQueryable<>)) is Type iqueryable) {
                yield return SqlTable.Create(iqueryable.GetGenericArguments()[0]);
                yield break;
            }

            else if (expression.Expression is ParameterExpression parameter) {
                var parameterName = GetParameterName(parameter);
                yield return SqlIdentifier.Create(parameterName, property.Name);
                yield break;
            }

            else if (expression.Expression is MemberExpression member) {
                var memberSql = Process(member, expression).ToImmutableArray();
                if (memberSql.Length == 1 && memberSql[0] is SqlIdentifier identifier) {
                    yield return SqlIdentifier.Create(identifier.Identifier, property.Name);
                    yield break;
                }
                else {
                    var query = SqlQuery.Create(memberSql);
                    yield return SqlAlias.Create(query, property.Name);
                    yield break;
                }
            }
        }

        else if (expression.Expression is not null) {
            foreach (var segment in Process(expression.Expression, expression)) {
                yield return segment;
            }
            yield break;
        }

        else {
            throw new NotImplementedException($"{nameof(MemberAccess)}: {expression.NodeType} {expression.GetType().Name} support has not been implemented.");
        }
    }

    private static string GetParameterName(ParameterExpression parameter) {
        return parameter.Name is not null && parameter.Name.StartsWith('<')
            ? '_' + Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(parameter.Name))[0..8])
            : parameter.Name ?? string.Empty;
    }

    static IEnumerable<ISqlil> Lambda(LambdaExpression expression, Expression? context) {
        if (expression.Parameters.Count == 1 && expression.Body == expression.Parameters[0]) {
            // Expand o => o
            var prefix = expression.Parameters[0].Name;
            var elements = GetResultColumn(expression.ReturnType);
            foreach (var element in elements) {
                yield return SqlIdentifier.Create(prefix, element);
            }
        }
        else {
            var elements = Process(expression.Body, expression);
            foreach (var element in elements) {
                yield return element;
            }
        }
    }

    static IEnumerable<string> GetResultColumn(Type type) {
        var under = Nullable.GetUnderlyingType(type) ?? type;
        if (under.Name.StartsWith("ValueTuple`")) {
            var results = under.GetFields().SelectMany(f => IsPrimitive(f.FieldType)
                ? new[] { f.Name }
                : GetResultColumn(f.FieldType).Select(name => f.Name + "." + name));
            return results;
        }
        else if (IsPrimitive(under)) {
            throw new NotSupportedException();
        }
        else {
            var results = under.GetProperties().Select(prop => prop.Name);
            return results;
        }
    }

    static IEnumerable<SqlIdentifier> Parameter(ParameterExpression expression, Expression _) {
        var parameterName = GetParameterName(expression);
        if (parameterName != string.Empty) {
            yield return SqlIdentifier.Create(parameterName);
        }
    }

    static IEnumerable<ISqlil> Quote(UnaryExpression expression, Expression context) {
        return Process(expression.Operand, expression);
    }

    static IEnumerable<ISqlil> Call(MethodCallExpression expression, Expression context) {
        if (expression.Object is not null) {
            // Instance method
        }
        else {
            // Static method

            if (expression.Method.DeclaringType == typeof(Queryable)) {
                if (expression.Method.Name == nameof(Queryable.Select)) {
                    // IQueryable<TResult> Select<TSource, TResult>(
                    //     this IQueryable<TSource> source,
                    //     Expression<Func<TSource, int, TResult>> selector
                    // )

                    // SELECT selector FROM source

                    if (expression.Arguments.Count != 2) throw new Exception("Expected 2 arguments");

                    var source = (MemberExpression)expression.Arguments[0];
                    var selector = (UnaryExpression)expression.Arguments[1];
                    var selectorOperand = (LambdaExpression)selector.Operand;
                    var alias = selectorOperand.Parameters[0].Name;

                    // Expand o => [something]
                    var selectElements = Process(selector, expression);
                    yield return SqlSelect.Create(selectElements.ToImmutableArray());

                    yield return SqlKeyword.Create("FROM");

                    var fromSql = Process(source, expression).Single();
                    yield return SqlAlias.Create(fromSql, alias);

                    yield break;
                }

                else if (expression.Method.Name == nameof(Queryable.Join)) {
                    // IQueryable<TResult> Queryable.Join(
                    //     this IQueryable<TOuter> outer
                    //     IEnumerable<TInner> inner
                    //     Expression<Func<TOuter, TKey>> outerKeySelector
                    //     Expression<Func<TInner, TKey>> innerKeySelector
                    //     Expression<Func<TOuter, TInner, TResult>> resultSelector
                    // )

                    // SELECT resultSelector FROM outer JOIN inner ON outerKeySelect = innerKeySelector

                    if (expression.Arguments.Count != 5) throw new Exception("Expected 5 arguments");

                    var outer = expression.Arguments[0];
                    var inner = expression.Arguments[1];
                    var outerKeySelector = (UnaryExpression)expression.Arguments[2];
                    var innerKeySelector = (UnaryExpression)expression.Arguments[3];
                    var resultSelector = (UnaryExpression)expression.Arguments[4];

                    var resultSelectorOperand = (LambdaExpression)resultSelector.Operand;
                    var outerAlias = resultSelectorOperand.Parameters[0].Name ?? throw new NullReferenceException("outerAlias");
                    var innerAlias = resultSelectorOperand.Parameters[1].Name ?? throw new NullReferenceException("innerAlias");

                    // Expand o => [something]
                    var selectElements = Process(resultSelector, expression);
                    yield return SqlSelect.Create(selectElements.ToImmutableArray());

                    yield return SqlKeyword.Create("FROM");

                    var outerSql = Process(outer, expression).ToImmutableArray();
                    if (outerSql.Length == 1 && outerSql[0] is SqlTable) {
                        yield return SqlAlias.Create(outerSql[0], outerAlias);
                    }
                    else {
                        var query = SqlQuery.Create(outerSql);
                        yield return SqlAlias.Create(query, outerAlias);
                    }

                    yield return SqlKeyword.Create("JOIN");

                    var innerSql = Process(inner, expression).ToImmutableArray();
                    if (innerSql.Length == 1 && innerSql[0] is SqlTable) {
                        yield return SqlAlias.Create(innerSql[0], innerAlias);
                    }
                    else {
                        var query = SqlQuery.Create(innerSql);
                        yield return SqlAlias.Create(query, innerAlias);
                    }

                    yield return SqlKeyword.Create("ON");

                    var outerKeySql = Process(outerKeySelector, expression).Single();
                    yield return outerKeySql;

                    yield return SqlKeyword.Create("=");

                    var innerKeySql = Process(innerKeySelector, expression).Single();
                    yield return innerKeySql;

                    yield break;
                }
            }

            else if (expression.Method.Name == nameof(ValueTuple.Create)) {
                // Static Create method

                foreach (var arg in expression.Arguments) {
                    //var selectors = Process(arg, expression);
                    var selectors = GetSelectors(arg, expression);

                    //var aliases = selectors.Zip(selectorAliases).Select(z => SqlAlias.Create(z.First, z.Second));

                    foreach (var selector in selectors)
                        yield return selector;
                }

                yield break;
            }
        }

        throw new NotImplementedException($"{nameof(Call)}: {expression.NodeType} {expression.GetType().Name} support has not been implemented.");
    }

    static IEnumerable<ISqlil> GetSelectors(Expression expression, Expression context) {

        IEnumerable<ISqlil> results = expression switch {

            ParameterExpression parameter => GetResultColumn(parameter.Type)
                .Select(p => SqlIdentifier.Create(parameter.Name, p) as ISqlil)
                .ToImmutableArray(),

            MemberExpression member => member.Expression is ParameterExpression innerParameter
                ? ImmutableArray.Create(SqlIdentifier.Create(innerParameter.Name, member.Member.Name) as ISqlil)
                : Process(member.Expression, member)
                    .Select(p => SqlAlias.Create(p, member.Member.Name) as ISqlil)
                    .ToImmutableArray(), // TODO: Need member.Name?

            _ => throw new NotImplementedException($"{nameof(GetSelectors)}: {expression.NodeType} {expression.GetType().Name} support has not been implemented."),
        };

        return results;
    }

    /// <summary>Returns the Type that implementation implements that most closely matches implementedType, or null if not found.</summary>
    static Type? IsType(Type implementation, Type implementedType) {
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

    static bool Implements(Type implementation, Type implementedType) {
        if (implementedType.IsGenericType) {
            if (implementedType.IsInterface) {
                var result = implementation
                    .GetInterfaces()
                    .Prepend(implementation)
                    .Any(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == implementedType);
                return result;
            }
            else {
                var type = implementation;
                while (type != typeof(object)) {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == implementedType) {
                        return true;
                    }
                    type = type?.BaseType ?? typeof(object);
                }
                return false;
            }
            throw new ArgumentException($"The {nameof(implementedType)} is not an interface type.", nameof(implementedType));
        }
        else {
            return implementation.IsAssignableTo(implementedType);
        }
    }

    static bool IsPrimitive(Type type) {
        return type.IsPrimitive || type == typeof(Guid) || type == typeof(string) || Nullable.GetUnderlyingType(type)?.IsPrimitive == true;
    }

    static IEnumerable<ISqlil> Process(Expression expression, Expression context) {
        switch (expression.NodeType) {
            case ExpressionType.Add:
                break;
            case ExpressionType.AddChecked:
                break;
            case ExpressionType.And:
                break;
            case ExpressionType.AndAlso:
                break;
            case ExpressionType.ArrayLength:
                break;
            case ExpressionType.ArrayIndex:
                break;
            case ExpressionType.Call:
                return Call((MethodCallExpression)expression, context);
            case ExpressionType.Coalesce:
                break;
            case ExpressionType.Conditional:
                break;
            case ExpressionType.Constant:
                break;
            case ExpressionType.Convert:
                break;
            case ExpressionType.ConvertChecked:
                break;
            case ExpressionType.Divide:
                break;
            case ExpressionType.Equal:
                break;
            case ExpressionType.ExclusiveOr:
                break;
            case ExpressionType.GreaterThan:
                break;
            case ExpressionType.GreaterThanOrEqual:
                break;
            case ExpressionType.Invoke:
                break;
            case ExpressionType.Lambda:
                return Lambda((LambdaExpression)expression, context);
            case ExpressionType.LeftShift:
                break;
            case ExpressionType.LessThan:
                break;
            case ExpressionType.LessThanOrEqual:
                break;
            case ExpressionType.ListInit:
                break;
            case ExpressionType.MemberAccess:
                return MemberAccess((MemberExpression)expression, context);
            case ExpressionType.MemberInit:
                break;
            case ExpressionType.Modulo:
                break;
            case ExpressionType.Multiply:
                break;
            case ExpressionType.MultiplyChecked:
                break;
            case ExpressionType.Negate:
                break;
            case ExpressionType.UnaryPlus:
                break;
            case ExpressionType.NegateChecked:
                break;
            case ExpressionType.New:
                return New((NewExpression)expression, context);
            case ExpressionType.NewArrayInit:
                break;
            case ExpressionType.NewArrayBounds:
                break;
            case ExpressionType.Not:
                break;
            case ExpressionType.NotEqual:
                break;
            case ExpressionType.Or:
                break;
            case ExpressionType.OrElse:
                break;
            case ExpressionType.Parameter:
                var parameterElements = Parameter((ParameterExpression)expression, context);
                return parameterElements.Cast<ISqlil>();
            case ExpressionType.Power:
                break;
            case ExpressionType.Quote:
                return Quote((UnaryExpression)expression, context);
            case ExpressionType.RightShift:
                break;
            case ExpressionType.Subtract:
                break;
            case ExpressionType.SubtractChecked:
                break;
            case ExpressionType.TypeAs:
                break;
            case ExpressionType.TypeIs:
                break;
            case ExpressionType.Assign:
                break;
            case ExpressionType.Block:
                break;
            case ExpressionType.DebugInfo:
                break;
            case ExpressionType.Decrement:
                break;
            case ExpressionType.Dynamic:
                break;
            case ExpressionType.Default:
                break;
            case ExpressionType.Extension:
                break;
            case ExpressionType.Goto:
                break;
            case ExpressionType.Increment:
                break;
            case ExpressionType.Index:
                break;
            case ExpressionType.Label:
                break;
            case ExpressionType.RuntimeVariables:
                break;
            case ExpressionType.Loop:
                break;
            case ExpressionType.Switch:
                break;
            case ExpressionType.Throw:
                break;
            case ExpressionType.Try:
                break;
            case ExpressionType.Unbox:
                break;
            case ExpressionType.AddAssign:
                break;
            case ExpressionType.AndAssign:
                break;
            case ExpressionType.DivideAssign:
                break;
            case ExpressionType.ExclusiveOrAssign:
                break;
            case ExpressionType.LeftShiftAssign:
                break;
            case ExpressionType.ModuloAssign:
                break;
            case ExpressionType.MultiplyAssign:
                break;
            case ExpressionType.OrAssign:
                break;
            case ExpressionType.PowerAssign:
                break;
            case ExpressionType.RightShiftAssign:
                break;
            case ExpressionType.SubtractAssign:
                break;
            case ExpressionType.AddAssignChecked:
                break;
            case ExpressionType.MultiplyAssignChecked:
                break;
            case ExpressionType.SubtractAssignChecked:
                break;
            case ExpressionType.PreIncrementAssign:
                break;
            case ExpressionType.PreDecrementAssign:
                break;
            case ExpressionType.PostIncrementAssign:
                break;
            case ExpressionType.PostDecrementAssign:
                break;
            case ExpressionType.TypeEqual:
                break;
            case ExpressionType.OnesComplement:
                break;
            case ExpressionType.IsTrue:
                break;
            case ExpressionType.IsFalse:
                break;
            default:
                throw new Exception($"BUG! {expression.NodeType} {expression.GetType().Name} is not considered by the switch statement.");
        }

        throw new NotImplementedException($"{expression.NodeType} {expression.GetType().Name} support has not been implemented.");
    }
}

//public IEnumerable<(string, string)> ResultColumn(Type type)
//{
//	var under = Nullable.GetUnderlyingType(type) ?? type;
//	if (under.Name.StartsWith("ValueTuple`"))
//	{
//		return under.GetFields().SelectMany(f => IsPrimitive(f.FieldType)
//			? new[] { (f.Name, f.Name) }
//			: ResultColumn(f.FieldType).Select((tuple) => (f.Name + "." + tuple.Item1, tuple.Item2)));
//	}
//	else if (IsPrimitive(under))
//	{
//		throw new NotSupportedException();
//	}
//	else
//	{
//		return under.GetProperties().Select(prop => (prop.Name, prop.PropertyType.Name + "." + prop.Name));
//	}
//}

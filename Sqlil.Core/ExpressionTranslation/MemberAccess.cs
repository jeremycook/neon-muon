using Sqlil.Core.Syntax;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlil.Core.ExpressionTranslation;

internal class MemberAccess {
    internal static object Translate(MemberExpression expression, TranslationContext context) {
        if (expression.Member is PropertyInfo property) {

            if (AnExpression.IsType(property.PropertyType, typeof(IQueryable<>)) is Type iqueryable) {
                // TODO: Get schema
                Type tableType = iqueryable.GetGenericArguments()[0];
                return TableOrSubqueryTable.Create(tableType.Name, TableAlias: context.ParameterName);
            }

            else if (expression.Expression is ParameterExpression parameter) {
                // parameterName.PropertyName
                var prefix = context.ParameterName ?? Identifier.Create(AnExpression.GetParameterName(parameter));
                return ExprColumn.Create(prefix, Identifier.Create(property.Name));
            }

            else if (expression.Expression is MemberExpression member) {
                var result = AnExpression.Translate(member, context);
                if (result is Identifier identifier) {
                    // parameterName.MemberIdentifier
                    return ExprColumn.Create(Identifier.Create(property.Name), identifier);
                }
                else {
                    throw new ExpressionNotSupportedException(expression);
                    // var query = SqlQuery.Create(result);
                    // return SqlAlias.Create(query, property.Name);
                }
            }

            else {
                throw new ExpressionNotSupportedException(expression);
            }
        }

        else if (expression.Expression is not null) {
            var result = AnExpression.Translate(expression.Expression, context);
            return result;
        }

        else {
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
using Sqlil.Core.Syntax;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    public virtual object MemberAccess(MemberExpression expression, TranslationContext context) {
        if (expression.Member is PropertyInfo property) {

            if (IsType(property.PropertyType, typeof(IQueryable<>)) is Type iqueryable) {
                // TODO? Get schema
                Type tableType = iqueryable.GetGenericArguments()[0];
                return TableOrSubqueryTable.Create(TableName.Create(tableType.Name, tableType), TableAlias: context.ParameterName);
            }

            else if (expression.Expression is ParameterExpression parameter) {
                var prefix = context.ParameterName ?? GetParameterName(parameter);
                return ExprColumn.Create(prefix, ColumnName.Create(property.Name, property.PropertyType));
            }

            else if (expression.Expression is MemberExpression member) {
                var result = Translate(member, context);
                if (result is ColumnName identifier) {
                    return ExprColumn.Create(TableName.Create(property.Name, property.PropertyType), identifier);
                }
                else {
                    throw new ExpressionNotSupportedException(expression);
                }
            }

            else {
                throw new ExpressionNotSupportedException(expression);
            }
        }

        else if (expression.Member is FieldInfo fieldInfo) {

            if (IsType(fieldInfo.FieldType, typeof(IQueryable<>)) is Type iqueryable) {
                // TODO? Get schema
                Type tableType = iqueryable.GetGenericArguments()[0];
                return TableOrSubqueryTable.Create(TableName.Create(tableType.Name, tableType), TableAlias: context.ParameterName);
            }

            else if (expression.Expression is ParameterExpression parameter) {
                var prefix = context.ParameterName ?? GetParameterName(parameter);
                return ExprColumn.Create(prefix, ColumnName.Create(fieldInfo.Name, fieldInfo.FieldType));
            }

            else if (expression.Expression is MemberExpression member) {
                var result = Translate(member, context);
                if (result is ColumnName identifier) {
                    return ExprColumn.Create(TableName.Create(fieldInfo.Name, fieldInfo.FieldType), identifier);
                }
                else {
                    throw new ExpressionNotSupportedException(expression);
                }
            }

            else if (expression.Expression is ConstantExpression constant) {
                // TODO: Add fieldInfo.Name
                var value = fieldInfo.GetValue(constant.Value);
                var result = ExprBindConstant.Create(fieldInfo.FieldType, value);
                return result;
            }

            else {
                throw new ExpressionNotSupportedException(expression);
            }
        }

        // else if (expression.Expression is not null) {
        //     var result = AnExpression.Translate(expression.Expression, context);
        //     return result;
        // }

        else {
            throw new ExpressionNotSupportedException($"The {expression.Member} member is not supported.", expression);
        }
    }
}
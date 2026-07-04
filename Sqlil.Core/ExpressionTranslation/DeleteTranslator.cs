using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    protected virtual DeleteStmt Delete(MethodCallExpression expression, TranslationContext context) {
        // IQueryable<T>.Delete<T>(source)
        var source = Translate(expression.Arguments[0], context);

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
                throw new ExpressionNotSupportedException($"Delete source not supported: {source.GetType()}.");
        }

        // Strip table qualifiers from WHERE clause (SQLite DELETE doesn't support table aliases)
        if (where != null) {
            where = StripTableQualifiers(where);
        }

        return DeleteStmt.Create(tableName, where);
    }
}

using static Sqlil.Core.Syntax.ParameterizedSqlHelpers;

namespace Sqlil.Core.Syntax;

public readonly record struct ParameterizedSql(StableList<SqlSegment> Segments) {
    public ParameterizedSql(SqlSegment SqlSegment) : this(StableList.Create<SqlSegment>(SqlSegment)) { }
    public ParameterizedSql(string CommandText) : this(new SqlText(CommandText)) { }
    public ParameterizedSql(IEnumerable<SqlSegment> Segments) : this(Segments.Select(x => x switch {
        SqlSegment sqlSegment => sqlSegment,
        // string text => new SqlText(text) as SqlSegment,
        _ => throw new NotSupportedException(x?.ToString()),
    }).ToStableList()) { }

    public static ParameterizedSql Empty { get; } = new(string.Empty);
}

public interface SqlSegment {
}

public readonly record struct SqlText(string Text) : SqlSegment {
}

public readonly record struct SqlInput(Type Type, string SuggestedName) : SqlSegment {
}

public readonly record struct SqlOutput(Type Type, string SuggestedName) : SqlSegment {
}

public static class ParameterizedSqlHelpers {

    public static ParameterizedSql Join(string separator, params object[] items) {
        return Join(items, separator);
    }

    public static ParameterizedSql Join(this IEnumerable<ParameterizedSql> items, string separator) {
        var source = items
            .Where(item => item != Empty);

        var segments = new List<SqlSegment>(2 * source.Count() - 1);

        segments.AddRange(source.Take(1).SelectMany(x => x.Segments));
        foreach (var item in source.Skip(1)) {
            segments.Add(new SqlText(separator));
            segments.AddRange(item.Segments);
        }

        return new(segments.ToStableList());
    }

    public static ParameterizedSql Join(this IEnumerable<object> items, string separator) {
        var source = items
            .Select(item => item switch {
                string text => text == string.Empty ? Empty : new ParameterizedSql(text),
                ParameterizedSql sql => sql,
                IEnumerable<object> sqls => sqls.Join(separator),
                _ => throw new NotSupportedException(item?.ToString())
            })
            .Where(item => item != Empty);

        var segments = new List<SqlSegment>(2 * source.Count() - 1);

        segments.AddRange(source.Take(1).SelectMany(x => x.Segments));
        foreach (var item in source.Skip(1)) {
            segments.Add(new SqlText(separator));
            segments.AddRange(item.Segments);
        }

        return new(segments.ToStableList());
    }

    public static ParameterizedSql Empty { get; } = ParameterizedSql.Empty;
}

public class SqliteComposer {
    public virtual ParameterizedSql Compose(object input) {
        ParameterizedSql composition = input switch {
            SelectStmt selectStmt => SelectStmt(selectStmt),
            _ => throw new NotImplementedException(),
        };
        return composition;
    }

    protected virtual ParameterizedSql SelectStmt(SelectStmt selectStmt) {
        List<ParameterizedSql> results = new();

        if (selectStmt.CommonTableExpressions.Any()) {
            var commonTableExpressions = Join(" ",
                "WITH",
                selectStmt.Recursive ? "RECURSIVE" : Empty,
                selectStmt.CommonTableExpressions.Select(CommonTableExpression)
            );
            results.Add(commonTableExpressions);
        }

        if (selectStmt.SelectCores.Any()) {
            var separator = selectStmt.CompoundOperator == CompoundOperator.UnionAll
                ? "\nUNION ALL "
                : "\n" + selectStmt.CompoundOperator.ToString().ToUpper() + " ";
            var selectCoresSql = selectStmt.SelectCores.Select(SelectCore).Join(separator);
            results.Add(selectCoresSql);
        }

        if (selectStmt.OrderingTerms.Any()) {
            var orderBySql = OrderBy(selectStmt.OrderingTerms, selectStmt.Limit, selectStmt.Offset);
            results.Add(orderBySql);
        }

        var result = results.Join("\n");
        return result;
    }

    private ParameterizedSql CommonTableExpression(CommonTableExpression commonTableExpression) {
        var selectStmtSql = SelectStmt(commonTableExpression.SelectStmt);

        var result = Join(" ",
            Identifier(commonTableExpression.TableName),
            commonTableExpression.ColumnNames.Select(Identifier).Join(" "),
            commonTableExpression.Materialized ? "MATERIALIZED" : Empty,
            "(", selectStmtSql, ")"
        );
        return result;
    }

    private ParameterizedSql Identifier(Identifier identifier) {
        return new("\"" + identifier.Name.Replace("\"\"", "\"") + "\"");
    }

    private ParameterizedSql SelectCore(SelectCore selectCore) {
        ParameterizedSql result = selectCore switch {
            SelectCoreNormal selectCoreNormal => SelectCoreNormal(selectCoreNormal),
            _ => throw new NotImplementedException(selectCore?.ToString()),
        };
        return result;
    }

    private ParameterizedSql SelectCoreNormal(SelectCoreNormal selectCoreNormal) {
        var results = new List<ParameterizedSql>();

        // ALL is implied when distinct is false
        var resultColumnsSql = selectCoreNormal.ResultColumns.Select(ResultColumn);
        results.Add(Join(" ",
            "SELECT",
            selectCoreNormal.Distinct ? "DISTINCT" : Empty,
            resultColumnsSql.Join(", ")
        ));

        // Invalid SQL will result if both TableOrSubqueries and JoinClause are set
        // TODO: Should we throw if both TableOrSubqueries and JoinClause are set?

        if (selectCoreNormal.TableOrSubqueries.Any()) {
            var tableOrSubqueriesSql = selectCoreNormal.TableOrSubqueries.Select(TableOrSubquery);
            results.Add(Join(" ", "FROM", tableOrSubqueriesSql.Join(",\n")));
        }

        if (selectCoreNormal.JoinClause != null) {
            var joinClauseSql = JoinClause(selectCoreNormal.JoinClause);
            results.Add(Join(" ", "FROM", joinClauseSql));
        }

        if (selectCoreNormal.Where != null) {
            var whereSql = Expr(selectCoreNormal.Where);
            results.Add(Join(" ", "WHERE", whereSql));
        }

        if (selectCoreNormal.GroupBys.Any()) {
            var groupBysSql = selectCoreNormal.GroupBys.Select(groupBy => GroupBy(groupBy));
            results.Add(Join(" ", "GROUP BY", groupBysSql));
        }

        if (selectCoreNormal.Having != null) {
            var havingSql = Expr(selectCoreNormal.Having);
            results.Add(Join(" ", "HAVING", havingSql));
        }

        if (selectCoreNormal.Windows.Any()) {
            // TODO: Implement WINDOW statement
            throw new NotImplementedException(selectCoreNormal.Windows.ToString());
        }

        var result = results.Join("\n");
        return result;
    }

    private ParameterizedSql ResultColumn(ResultColumn resultColumn) {
        ParameterizedSql result = resultColumn switch {
            ResultColumnAsterisk resultColumnAsterisk => ResultColumnAsterisk(resultColumnAsterisk),
            ResultColumnExpr resultColumnExpr => ResultColumnExpr(resultColumnExpr),
            ResultColumnTable resultColumnTable => ResultColumnTable(resultColumnTable),
            _ => throw new NotImplementedException(resultColumn.ToString()),
        };
        return result;
    }

    private ParameterizedSql ResultColumnAsterisk(ResultColumnAsterisk resultColumnAsterisk) {
        return new("*");
    }

    private ParameterizedSql ResultColumnExpr(ResultColumnExpr resultColumnExpr) {
        var results = new List<ParameterizedSql>();

        ParameterizedSql exprSql = Expr(resultColumnExpr.Expr);
        results.Add(exprSql);

        if (resultColumnExpr.ColumnAlias != null &&
            (resultColumnExpr.Expr is not ExprColumn exprColumn || exprColumn.ColumnName != resultColumnExpr.ColumnAlias)) {
            var columnAlias = Identifier(resultColumnExpr.ColumnAlias);
            results.Add(columnAlias);
        }

        var result = results.Join(" ");
        return result;
    }

    private ParameterizedSql ResultColumnTable(ResultColumnTable resultColumnTable) {
        return Join(".", Identifier(resultColumnTable.TableName), "*");
    }

    private ParameterizedSql TableOrSubquery(TableOrSubquery tableOrSubquery) {
        ParameterizedSql result = tableOrSubquery switch {
            TableOrSubqueryFunction tableOrSubqueryFunction => TableOrSubqueryFunction(tableOrSubqueryFunction),
            TableOrSubqueryJoin tableOrSubqueryJoin => TableOrSubqueryJoin(tableOrSubqueryJoin),
            TableOrSubquerySelectStmts tableOrSubquerySelectStmts => TableOrSubquerySelectStmts(tableOrSubquerySelectStmts),
            TableOrSubqueryTable tableOrSubqueryTable => TableOrSubqueryTable(tableOrSubqueryTable),
            TableOrSubqueryTableOrSubqueries tableOrSubqueryTableOrSubqueries => TableOrSubqueryTableOrSubqueries(tableOrSubqueryTableOrSubqueries),
            _ => throw new NotImplementedException(tableOrSubquery?.ToString())
        };
        return result;
    }

    private ParameterizedSql TableOrSubqueryFunction(TableOrSubqueryFunction tableOrSubqueryFunction) {
        throw new NotImplementedException();
    }

    private ParameterizedSql TableOrSubqueryJoin(TableOrSubqueryJoin tableOrSubqueryJoin) {
        throw new NotImplementedException();
    }

    private ParameterizedSql TableOrSubquerySelectStmts(TableOrSubquerySelectStmts tableOrSubquerySelectStmts) {
        throw new NotImplementedException();
    }

    private ParameterizedSql TableOrSubqueryTable(TableOrSubqueryTable tableOrSubqueryTable) {
        var result = Join(" ",
            Join(".",
                tableOrSubqueryTable.SchemaName != null ? Identifier(tableOrSubqueryTable.SchemaName) : Empty,
                Identifier(tableOrSubqueryTable.TableName)
            ),
            tableOrSubqueryTable.TableAlias != null ? Identifier(tableOrSubqueryTable.TableAlias) : Empty,
            tableOrSubqueryTable.IndexName != null ? Join(" ", "INDEXED BY", Identifier(tableOrSubqueryTable.IndexName)) : Empty
        );
        return result;
    }

    private ParameterizedSql TableOrSubqueryTableOrSubqueries(TableOrSubqueryTableOrSubqueries tableOrSubqueryTableOrSubqueries) {
        throw new NotImplementedException();
    }

    private ParameterizedSql JoinClause(JoinClause joinClause) {
        throw new NotImplementedException();
    }

    private ParameterizedSql GroupBy(Expr groupBy) {
        throw new NotImplementedException();
    }

    private ParameterizedSql OrderBy(StableList<OrderingTerm> orderingTerms, Expr? limit, Expr? offset) {
        var results = new List<ParameterizedSql>();

        if (orderingTerms.Any()) {
            var orderingTermsSql = orderingTerms.Select(OrderingTerm);
            results.Add(Join(" ", "ORDER BY", orderingTermsSql.Join(", ")));
        }

        // TODO: Should LIMIT only be generated if ORDER BY is?
        if (limit != null) {
            ParameterizedSql limitSql = Expr(limit);
            results.Add(Join(" ", "LIMIT", limitSql));
        }

        // TODO: Should OFFSET only be generated if LIMIT is?
        if (offset != null) {
            ParameterizedSql offsetSql = Expr(offset);
            results.Add(Join(" ", "OFFSET", offsetSql));
        }

        var result = results.Join("\n");
        return result;
    }

    private ParameterizedSql OrderingTerm(OrderingTerm orderingTerm) {
        var exprSql = Expr(orderingTerm.Expr);
        var result = Join(" ",
            exprSql,
            orderingTerm.CollationName != null ? "COLLATE " + orderingTerm.CollationName : Empty,
            orderingTerm.Desc ? "DESC" : Empty,
            orderingTerm.NullsLast ? "NULL LAST" : Empty
        );
        return result;
    }

    private ParameterizedSql Expr(Expr expr) {
        ParameterizedSql result = expr switch {
            ExprBinary exprBinary => ExprBinary(exprBinary),
            ExprBindParameter exprBindParameter => ExprBindParameter(exprBindParameter),
            ExprColumn exprColumn => ExprColumn(exprColumn),
            ExprLiteralInteger exprLiteralInteger => ExprLiteralInteger(exprLiteralInteger),
            ExprLiteralString exprLiteralString => ExprLiteralString(exprLiteralString),
            ExprUnary exprUnary => ExprUnary(exprUnary),
            _ => throw new NotImplementedException(expr.ToString()),
        };
        return result;
    }

    private ParameterizedSql ExprBinary(ExprBinary exprBinary) {
        var result = exprBinary.Operator switch {
            BinaryOperator.AndAlso or BinaryOperator.OrElse => Join("", "(", Expr(exprBinary.Left), " ", BinaryConstants.OperatorToSql[exprBinary.Operator], " ", Expr(exprBinary.Right), ")"),
            _ => Join("", Expr(exprBinary.Left), " ", BinaryConstants.OperatorToSql[exprBinary.Operator], " ", Expr(exprBinary.Right))
        };
        return result;
    }

    private ParameterizedSql ExprBindParameter(ExprBindParameter exprBindParameter) {
        var result = new ParameterizedSql(StableList.Create<SqlSegment>(new SqlInput(exprBindParameter.Type, exprBindParameter.SuggestedName ?? string.Empty)));
        return result;
    }

    private ParameterizedSql ExprColumn(ExprColumn exprColumn) {
        var result = Join(".",
            exprColumn.SchemaName != null ? Identifier(exprColumn.SchemaName) : Empty,
            exprColumn.TableName != null ? Identifier(exprColumn.TableName) : Empty,
            Identifier(exprColumn.ColumnName)
        );
        return result;
    }

    private ParameterizedSql ExprLiteralInteger(ExprLiteralInteger exprLiteralInteger) {
        return new(exprLiteralInteger.Value.ToString());
    }

    private ParameterizedSql ExprLiteralString(ExprLiteralString exprLiteralString) {
        return new("'" + exprLiteralString.Value.Replace("'", "''") + "'");
    }

    private ParameterizedSql ExprUnary(ExprUnary exprUnary) {
        var operand = Expr(exprUnary.Operand);
        return Join(" ", UnaryConstants.OperatorToString[exprUnary.Operator], operand);
    }
}

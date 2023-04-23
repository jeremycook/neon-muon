using static Sqlil.Core.Syntax.ParameterizedSqlHelpers;

namespace Sqlil.Core.Syntax;

public readonly record struct ParameterizedSql(StableList<SqlSegment> Segments) {
    public ParameterizedSql(SqlSegment SqlSegment) : this(StableList.Create<SqlSegment>(SqlSegment)) { }
    public ParameterizedSql(string CommandText) : this(new SqlRaw(CommandText)) { }
    public ParameterizedSql(IEnumerable<SqlSegment> Segments) : this(Segments.Select(x => x switch {
        SqlSegment sqlSegment => sqlSegment,
        // string text => new SqlText(text) as SqlSegment,
        _ => throw new NotSupportedException(x?.ToString()),
    }).ToStableList()) { }

    public static ParameterizedSql Empty { get; } = new(string.Empty);
}

public interface SqlSegment { }

public interface SqlRenderable : SqlSegment { }

/// <summary>
/// Raw SQL to be rendered as-is to command text.
/// </summary>
public readonly record struct SqlRaw(
    string Text
) : SqlRenderable { }

/// <summary>
/// Both a marker for passing input parameters,
/// and hints for rendering to command text.
/// </summary>
public readonly record struct SqlInputParameter(
    Type Type,
    string SuggestedName
) : SqlRenderable { }

/// <summary>
/// Both a marker for passing an input parameter,
/// and a hint for rendering to command text.
/// </summary>
public readonly record struct SqlConstantParameter(
    Type Type,
    object? Value
) : SqlRenderable { }

/// <summary>
/// This is a marker that is used when materializing results,
/// but should not render to command text.
/// </summary>
public readonly record struct SqlColumn(
    Type Type,
    string SuggestedName
) : SqlSegment { }

public static class ParameterizedSqlHelpers {

    public static ParameterizedSql Join(string separator, params object[] items) {
        return Join(items, separator);
    }

    public static ParameterizedSql Join(this IEnumerable<ParameterizedSql> items, string separator) {
        var source = items
            .Where(item => item != Empty);

        var segments = new List<SqlSegment>();

        segments.AddRange(source.Take(1).SelectMany(x => x.Segments));
        foreach (var item in source.Skip(1)) {
            if (item.Segments.Any(x => x is SqlRenderable)) {
                segments.Add(new SqlRaw(separator));
            }
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

        var segments = new List<SqlSegment>();

        segments.AddRange(source.Take(1).SelectMany(x => x.Segments));
        foreach (var item in source.Skip(1)) {
            if (item.Segments.Any(x => x is SqlRenderable)) {
                segments.Add(new SqlRaw(separator));
            }
            segments.AddRange(item.Segments);
        }

        return new(segments.ToStableList());
    }

    public static ParameterizedSql Empty { get; } = ParameterizedSql.Empty;
}

public class SqliteComposer {
    public virtual ParameterizedSql Compose(object input) {
        ParameterizedSql composition = input switch {
            SelectStmt selectStmt => SelectStmt(selectStmt, true),
            _ => throw new NotImplementedException(),
        };
        return composition;
    }

    protected virtual ParameterizedSql SelectStmt(SelectStmt selectStmt, bool topLevel) {
        List<ParameterizedSql> results = new();

        if (selectStmt.CommonTableExpressions.Any()) {
            var commonTableExpressions = Join(" ",
                "WITH",
                selectStmt.Recursive ? "RECURSIVE" : Empty,
                selectStmt.CommonTableExpressions.Select(x => CommonTableExpression(x))
            );
            results.Add(commonTableExpressions);
        }

        if (selectStmt.SelectCores.Any()) {
            var separator = selectStmt.CompoundOperator == CompoundOperator.UnionAll
                ? "\nUNION ALL "
                : "\n" + selectStmt.CompoundOperator.ToString().ToUpper() + " ";
            var selectCoresSql = selectStmt.SelectCores.Select(x => SelectCore(x, topLevel)).Join(separator);
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
        var result = Join(" ",
            Identifier(commonTableExpression.TableName),
            commonTableExpression.ColumnNames.Select(Identifier).Join(", "),
            commonTableExpression.Materialized ? "MATERIALIZED" : Empty,
            Join("", "(", SelectStmt(commonTableExpression.SelectStmt, false), ")")
        );
        return result;
    }

    private ParameterizedSql Identifier(Identifier identifier) {
        ParameterizedSql result = new("\"" + identifier.Name.Replace("\"\"", "\"") + "\"");
        return result;
    }

    private ParameterizedSql SqlOutput(TypedIdentifier typedIdentifier) {
        ParameterizedSql result = new ParameterizedSql(new SqlColumn(typedIdentifier.Type, typedIdentifier.Name));
        return result;
    }

    private ParameterizedSql SelectCore(SelectCore selectCore, bool topLevel) {
        ParameterizedSql result = selectCore switch {
            SelectCoreNormal selectCoreNormal => SelectCoreNormal(selectCoreNormal, topLevel),
            _ => throw new NotImplementedException(selectCore?.ToString()),
        };
        return result;
    }

    private ParameterizedSql SelectCoreNormal(SelectCoreNormal selectCoreNormal, bool topLevel) {
        var results = new List<ParameterizedSql>();

        // ALL is implied when distinct is false
        var resultColumnsSql = selectCoreNormal.ResultColumns.Select(x => ResultColumn(x, topLevel));
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

    private ParameterizedSql ResultColumn(ResultColumn resultColumn, bool topLevel) {
        ParameterizedSql result = resultColumn switch {
            ResultColumnAsterisk resultColumnAsterisk => ResultColumnAsterisk(resultColumnAsterisk),
            ResultColumnExpr resultColumnExpr => ResultColumnExpr(resultColumnExpr, topLevel),
            ResultColumnTable resultColumnTable => ResultColumnTable(resultColumnTable),
            _ => throw new NotImplementedException(resultColumn.ToString()),
        };
        return result;
    }

    private ParameterizedSql ResultColumnAsterisk(ResultColumnAsterisk resultColumnAsterisk) {
        return new("*");
    }

    private ParameterizedSql ResultColumnExpr(ResultColumnExpr resultColumnExpr, bool topLevel) {
        var results = new List<ParameterizedSql>();

        var exprSql = Expr(resultColumnExpr.Expr);
        results.Add(exprSql);

        if (resultColumnExpr.ColumnAlias != null) {
            if (resultColumnExpr.Expr is not ExprColumn exprColumn || exprColumn.ColumnName != resultColumnExpr.ColumnAlias) {
                var columnAlias = Identifier(resultColumnExpr.ColumnAlias);

                results.Add(columnAlias);
            }
            else {
                // The column alias is ignored since it matches the column name being provided
            }
        }

        if (topLevel) {
            // Top-level result columns get special treatment with typed SqlOutput

            if (resultColumnExpr.ColumnAlias != null) {
                // The column alias will provide the output information
                var sqlOutput = SqlOutput(resultColumnExpr.ColumnAlias);
                results.Add(sqlOutput);
            }
            else {
                // Try to infer the output information from the expression
                if (resultColumnExpr.Expr is ExprColumn exprColumn) {
                    // Base it on the column
                    var sqlOutput = SqlOutput(exprColumn.ColumnName);
                    results.Add(sqlOutput);
                }
                else {
                    // Create an alias and infer it from the expression
                    var sqlOutput = SqlOutput(new ColumnName("__so" + resultColumnExpr.Expr.GetHashCode(), resultColumnExpr.Expr.Type));
                    results.Add(sqlOutput);
                }
            }
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
            ExprBindConstant exprBindConstant => ExprBindConstant(exprBindConstant),
            ExprBindParameter exprBindParameter => ExprBindParameter(exprBindParameter),
            ExprColumn exprColumn => ExprColumn(exprColumn),
            ExprLiteralString exprLiteral => ExprLiteral(exprLiteral),
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

    private ParameterizedSql ExprBindConstant(ExprBindConstant exprBindConstant) {
        var result = new ParameterizedSql(new SqlConstantParameter(exprBindConstant.Type, exprBindConstant.Value));
        return result;
    }

    private ParameterizedSql ExprBindParameter(ExprBindParameter exprBindParameter) {
        var result = new ParameterizedSql(new SqlInputParameter(exprBindParameter.Type, exprBindParameter.SuggestedName ?? string.Empty));
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

    private ParameterizedSql ExprLiteral(ExprLiteralString exprLiteral) {
        return new("'" + exprLiteral.Value.Replace("'", "''") + "'");
    }

    private ParameterizedSql ExprUnary(ExprUnary exprUnary) {
        var operand = Expr(exprUnary.Operand);
        return Join(" ", UnaryConstants.OperatorToString[exprUnary.Operator], operand);
    }
}

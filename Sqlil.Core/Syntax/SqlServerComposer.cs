using static Sqlil.Core.Syntax.ParameterizedSqlHelpers;

namespace Sqlil.Core.Syntax;

public class SqlServerComposer : SqliteComposer
{
    protected override ParameterizedSql Identifier(Identifier identifier)
    {
        return new("[" + identifier.Name.Replace("]", "]]") + "]");
    }

    protected override ParameterizedSql SelectStmt(SelectStmt selectStmt, bool topLevel)
    {
        var hasLimit = selectStmt.Limit != null;
        var hasOffset = selectStmt.Offset != null;
        var hasOrderBy = selectStmt.OrderingTerms.Any();
        var simpleTop = hasLimit && !hasOffset && !hasOrderBy;

        if (simpleTop)
        {
            var cores = selectStmt.SelectCores.Select(core => SelectCoreWithTop(core, topLevel, selectStmt.Limit)).Join("\nUNION ALL ");
            var result = new List<ParameterizedSql>();

            if (selectStmt.CommonTableExpressions.Any())
            {
                var cte = Join(" ",
                    "WITH",
                    selectStmt.CommonTableExpressions.Select(x => CommonTableExpression(x))
                );
                result.Add(cte);
            }

            result.Add(cores);
            return result.Join("\n");
        }

        return base.SelectStmt(selectStmt, topLevel);
    }

    private ParameterizedSql SelectCoreWithTop(SelectCore core, bool topLevel, Expr? topExpr)
    {
        if (core is SelectCoreNormal normal)
        {
            var results = new List<ParameterizedSql>();
            var resultColumnsSql = normal.ResultColumns.Select(x => ResultColumn(x, topLevel)).Join(", ");

            var topSql = topExpr != null ? Expr(topExpr) : ParameterizedSql.Empty;
            results.Add(Join(" ",
                "SELECT",
                topExpr != null ? Join("", "TOP (", topSql, ")") : Empty,
                normal.Distinct ? "DISTINCT" : Empty,
                resultColumnsSql
            ));

            if (normal.TableOrSubqueries.Any())
            {
                var tablesSql = normal.TableOrSubqueries.Select(TableOrSubquery).Join(",\n");
                results.Add(Join(" ", "FROM", tablesSql));
            }

            if (normal.JoinClause != null)
            {
                results.Add(Join(" ", "FROM", JoinClause(normal.JoinClause)));
            }

            if (normal.Where != null)
            {
                results.Add(Join(" ", "WHERE", Expr(normal.Where)));
            }

            if (normal.GroupBys.Any())
            {
                var groupBysSql = normal.GroupBys.Select(GroupBy).ToList();
                results.Add(Join(" ", "GROUP BY", groupBysSql.Join(", ")));
            }

            if (normal.Having != null)
            {
                results.Add(Join(" ", "HAVING", Expr(normal.Having)));
            }

            return results.Join("\n");
        }

        return SelectCore(core, topLevel);
    }

    protected override ParameterizedSql OrderBy(StableList<OrderingTerm> orderingTerms, Expr? limit, Expr? offset)
    {
        var results = new List<ParameterizedSql>();

        if (orderingTerms.Any() || limit != null || offset != null)
        {
            if (orderingTerms.Any())
            {
                var orderingTermsSql = orderingTerms.Select(OrderingTerm);
                results.Add(Join(" ", "ORDER BY", orderingTermsSql.Join(", ")));
            }
            else if (limit != null || offset != null)
            {
                results.Add(new("ORDER BY (SELECT NULL)"));
            }

            if (offset != null)
            {
                results.Add(Join(" ", "OFFSET", Expr(offset), "ROWS"));
            }
            else if (limit != null)
            {
                results.Add(Join(" ", "OFFSET 0 ROWS"));
            }

            if (limit != null)
            {
                results.Add(Join(" ", "FETCH NEXT", Expr(limit), "ROWS ONLY"));
            }
        }

        return results.Join("\n");
    }

    protected override ParameterizedSql InsertStmt(InsertStmt insertStmt)
    {
        var columnsSql = insertStmt.ColumnNames.Select(Identifier).Join(", ");
        var valuesSql = insertStmt.Values.Select(row =>
            row.Select(Expr).Join(", ")
        ).Join("), (");

        var results = new List<ParameterizedSql>();
        results.Add(Join(" ",
            "INSERT INTO", Identifier(insertStmt.TableName),
            "(", columnsSql, ")",
            "VALUES", "(", valuesSql, ")"
        ));

        if (insertStmt.Returning.Any())
        {
            var outputColumns = insertStmt.Returning
                .Select(col => col switch
                {
                    ResultColumnExpr rce when rce.Expr is ExprColumn ec => Join(" ", "INSERTED.", Identifier(ec.ColumnName)),
                    ResultColumnExpr rce => Join(" ", "INSERTED.", Identifier(rce.ColumnAlias ?? new ColumnName("?", typeof(object)))),
                    _ => Join(" ", "INSERTED.*")
                })
                .Join(", ");
            results.Add(Join(" ", "OUTPUT", outputColumns));
        }

        return results.Join("\n");
    }

    protected override ParameterizedSql UpdateStmt(UpdateStmt updateStmt)
    {
        var setClausesSql = updateStmt.SetClauses.Select(set =>
            Join(" ", Identifier(set.ColumnName), "=", Expr(set.Value))
        ).Join(", ");

        var results = new List<ParameterizedSql>();
        results.Add(Join(" ",
            "UPDATE", Identifier(updateStmt.TableName),
            "SET", setClausesSql
        ));

        if (updateStmt.Where != null)
        {
            results.Add(Join(" ", "WHERE", Expr(updateStmt.Where)));
        }

        if (updateStmt.Returning.Any())
        {
            var outputColumns = updateStmt.Returning
                .Select(col => col switch
                {
                    ResultColumnExpr rce when rce.Expr is ExprColumn ec => Join(" ", "INSERTED.", Identifier(ec.ColumnName)),
                    ResultColumnExpr rce => Join(" ", "INSERTED.", Identifier(rce.ColumnAlias ?? new ColumnName("?", typeof(object)))),
                    _ => Join(" ", "INSERTED.*")
                })
                .Join(", ");
            results.Add(Join(" ", "OUTPUT", outputColumns));
        }

        return results.Join("\n");
    }

    protected override ParameterizedSql DeleteStmt(DeleteStmt deleteStmt)
    {
        var results = new List<ParameterizedSql>();
        results.Add(Join(" ", "DELETE FROM", Identifier(deleteStmt.TableName)));

        if (deleteStmt.Where != null)
        {
            results.Add(Join(" ", "WHERE", Expr(deleteStmt.Where)));
        }

        if (deleteStmt.Returning.Any())
        {
            var outputColumns = deleteStmt.Returning
                .Select(col => col switch
                {
                    ResultColumnExpr rce when rce.Expr is ExprColumn ec => Join(" ", "DELETED.", Identifier(ec.ColumnName)),
                    ResultColumnExpr rce => Join(" ", "DELETED.", Identifier(rce.ColumnAlias ?? new ColumnName("?", typeof(object)))),
                    _ => Join(" ", "DELETED.*")
                })
                .Join(", ");
            results.Add(Join(" ", "OUTPUT", outputColumns));
        }

        return results.Join("\n");
    }
}

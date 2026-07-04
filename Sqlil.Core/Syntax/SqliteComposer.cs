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
                IEnumerable<ParameterizedSql> sqls => sqls.Join(separator),
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
            InsertStmt insertStmt => InsertStmt(insertStmt),
            UpdateStmt updateStmt => UpdateStmt(updateStmt),
            DeleteStmt deleteStmt => DeleteStmt(deleteStmt),
            _ => throw new NotImplementedException(input?.GetType().ToString()),
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

    protected virtual ParameterizedSql CommonTableExpression(CommonTableExpression commonTableExpression) {
        var result = Join(" ",
            Identifier(commonTableExpression.TableName),
            commonTableExpression.ColumnNames.Select(Identifier).Join(", "),
            commonTableExpression.Materialized ? "MATERIALIZED" : Empty,
            Join("", "(", SelectStmt(commonTableExpression.SelectStmt, false), ")")
        );
        return result;
    }

    protected virtual ParameterizedSql Identifier(Identifier identifier) {
        ParameterizedSql result = new("\"" + identifier.Name.Replace("\"\"", "\"") + "\"");
        return result;
    }

    protected virtual ParameterizedSql SqlOutput(TypedIdentifier typedIdentifier) {
        ParameterizedSql result = new ParameterizedSql(new SqlColumn(typedIdentifier.Type, typedIdentifier.Name));
        return result;
    }

    protected virtual ParameterizedSql SelectCore(SelectCore selectCore, bool topLevel) {
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
            var groupBysSql = selectCoreNormal.GroupBys.Select(groupBy => GroupBy(groupBy)).ToList();
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

    protected virtual ParameterizedSql ResultColumn(ResultColumn resultColumn, bool topLevel) {
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

    protected virtual ParameterizedSql TableOrSubquery(TableOrSubquery tableOrSubquery) {
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
        return JoinClause(tableOrSubqueryJoin.JoinClause);
    }

    private ParameterizedSql TableOrSubquerySelectStmts(TableOrSubquerySelectStmts tableOrSubquerySelectStmts) {
        var results = new List<ParameterizedSql>();

        if (tableOrSubquerySelectStmts.SelectStmts.Count == 1) {
            results.Add(Join("", "(", SelectStmt(tableOrSubquerySelectStmts.SelectStmts[0], false), ")"));
        }
        else {
            var selectStmtsSql = tableOrSubquerySelectStmts.SelectStmts.Select(s => SelectStmt(s, false)).Join("\nUNION ALL ");
            results.Add(Join("", "(", selectStmtsSql, ")"));
        }

        if (tableOrSubquerySelectStmts.TableAlias != null) {
            results.Add(Identifier(tableOrSubquerySelectStmts.TableAlias));
        }

        return results.Join(" ");
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

    protected virtual ParameterizedSql JoinClause(JoinClause joinClause) {
        var results = new List<ParameterizedSql>();

        results.Add(TableOrSubquery(joinClause.TableOrSubquery));

        foreach (var (joinOperator, tableOrSubquery, joinConstraint) in joinClause.Joins) {
            var joinOpSql = joinOperator switch {
                JoinOperator.Comma => ",",
                JoinOperator.Left => "LEFT JOIN",
                JoinOperator.Right => "RIGHT JOIN",
                JoinOperator.Full => "FULL JOIN",
                JoinOperator.Inner => "JOIN",
                JoinOperator.Cross => "CROSS JOIN",
                _ => throw new NotImplementedException(joinOperator.ToString()),
            };

            var constraintSql = joinConstraint switch {
                JoinConstraintOn on => Join(" ", "ON", Expr(on.Expr)),
                JoinConstraintUsing usingClause => Join(" ", "USING", "(", usingClause.ColumnNames.Select(Identifier).Join(", "), ")"),
                JoinConstraintNone => ParameterizedSql.Empty,
                _ => throw new NotImplementedException(joinConstraint?.ToString()),
            };

            results.Add(Join(" ",
                joinOpSql,
                TableOrSubquery(tableOrSubquery),
                constraintSql
            ));
        }

        return results.Join("\n");
    }

    protected virtual ParameterizedSql GroupBy(Expr groupBy) {
        return Expr(groupBy);
    }

    protected virtual ParameterizedSql OrderBy(StableList<OrderingTerm> orderingTerms, Expr? limit, Expr? offset) {
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

    protected virtual ParameterizedSql OrderingTerm(OrderingTerm orderingTerm) {
        var exprSql = Expr(orderingTerm.Expr);
        var result = Join(" ",
            exprSql,
            orderingTerm.CollationName != null ? "COLLATE " + orderingTerm.CollationName : Empty,
            orderingTerm.Desc ? "DESC" : Empty,
            orderingTerm.NullsLast ? "NULL LAST" : Empty
        );
        return result;
    }

    protected virtual ParameterizedSql Expr(Expr expr) {
        ParameterizedSql result = expr switch {
            ExprBinary exprBinary => ExprBinary(exprBinary),
            ExprBindConstant exprBindConstant => ExprBindConstant(exprBindConstant),
            ExprBindParameter exprBindParameter => ExprBindParameter(exprBindParameter),
            ExprBetween exprBetween => ExprBetween(exprBetween),
            ExprCase exprCase => ExprCase(exprCase),
            ExprCast exprCast => ExprCast(exprCast),
            ExprColumn exprColumn => ExprColumn(exprColumn),
            ExprExists exprExists => ExprExists(exprExists),
            ExprFunction exprFunction => ExprFunction(exprFunction),
            ExprIn exprIn => ExprIn(exprIn),
            ExprIsNull exprIsNull => ExprIsNull(exprIsNull),
            ExprLiteralString exprLiteral => ExprLiteral(exprLiteral),
            ExprUnary exprUnary => ExprUnary(exprUnary),
            _ => throw new NotImplementedException(expr.ToString()),
        };
        return result;
    }

    private ParameterizedSql ExprCase(ExprCase exprCase) {
        var parts = new List<ParameterizedSql> { new("CASE") };
        foreach (var (when, then) in exprCase.WhenClauses) {
            parts.Add(Join(" ", "WHEN", Expr(when), "THEN", Expr(then)));
        }
        if (exprCase.Else != null) {
            parts.Add(Join(" ", "ELSE", Expr(exprCase.Else)));
        }
        parts.Add(new("END"));
        return Join(" ", parts.ToArray());
    }

    private ParameterizedSql ExprExists(ExprExists exprExists) {
        var innerSql = SelectStmt(exprExists.SelectStmt, false);
        return Join("", "EXISTS (", innerSql, ")");
    }

    private ParameterizedSql ExprIn(ExprIn exprIn) {
        var operandSql = Expr(exprIn.Operand);
        if (exprIn.Values != null) {
            var valuesSql = exprIn.Values.Select(Expr).Join(", ");
            return Join("", operandSql, " IN (", valuesSql, ")");
        }
        if (exprIn.Subquery != null) {
            var subquerySql = SelectStmt(exprIn.Subquery, false);
            return Join("", operandSql, " IN (", subquerySql, ")");
        }
        throw new NotImplementedException("ExprIn has neither Values nor Subquery");
    }

    private ParameterizedSql ExprBetween(ExprBetween exprBetween) {
        var exprSql = Expr(exprBetween.Expr);
        var lowSql = Expr(exprBetween.Low);
        var highSql = Expr(exprBetween.High);
        return Join(" ", exprSql, "BETWEEN", lowSql, "AND", highSql);
    }

    private ParameterizedSql ExprCast(ExprCast exprCast) {
        var exprSql = Expr(exprCast.Expr);
        var typeName = GetSqlTypeName(exprCast.TargetType);
        return Join("", "CAST(", exprSql, " AS ", typeName, ")");
    }

    protected static string GetSqlTypeName(Type type) {
        return type switch {
            _ when type == typeof(long) => "INTEGER",
            _ when type == typeof(int) => "INTEGER",
            _ when type == typeof(short) => "INTEGER",
            _ when type == typeof(byte) => "INTEGER",
            _ when type == typeof(double) => "REAL",
            _ when type == typeof(float) => "REAL",
            _ when type == typeof(decimal) => "REAL",
            _ when type == typeof(string) => "TEXT",
            _ when type == typeof(bool) => "INTEGER",
            _ when type == typeof(Guid) => "TEXT",
            _ when type == typeof(DateTime) => "TEXT",
            _ when type == typeof(DateOnly) => "TEXT",
            _ when type == typeof(byte[]) => "BLOB",
            _ => "TEXT",
        };
    }

    private ParameterizedSql ExprIsNull(ExprIsNull exprIsNull) {
        var operandSql = Expr(exprIsNull.Operand);
        var op = exprIsNull.IsNot ? "IS NOT NULL" : "IS NULL";
        return Join(" ", operandSql, op);
    }

    private ParameterizedSql ExprFunction(ExprFunction exprFunction) {
        var exprsSql = exprFunction.Exprs.Select(Expr);
        var result = exprFunction.FunctionName switch {
            ExprFunctionName.Lower => Join("", "LOWER", "(", exprsSql.Join(", "), ")"),
            ExprFunctionName.Upper => Join("", "UPPER", "(", exprsSql.Join(", "), ")"),
            ExprFunctionName.Count => exprFunction.Exprs.Count == 0
                ? new ParameterizedSql("COUNT(*)")
                : Join("", "COUNT", "(", exprsSql.Join(", "), ")"),
            ExprFunctionName.Sum => Join("", "SUM", "(", exprsSql.Join(", "), ")"),
            ExprFunctionName.Average => Join("", "AVG", "(", exprsSql.Join(", "), ")"),
            ExprFunctionName.Min => Join("", "MIN", "(", exprsSql.Join(", "), ")"),
            ExprFunctionName.Max => Join("", "MAX", "(", exprsSql.Join(", "), ")"),
            ExprFunctionName.Coalesce => Join("", "COALESCE", "(", exprsSql.Join(", "), ")"),
            _ => throw new NotImplementedException(exprFunction.FunctionName.ToString()),
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

    protected virtual ParameterizedSql InsertStmt(InsertStmt insertStmt) {
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

        if (insertStmt.Returning.Any()) {
            var returningSql = insertStmt.Returning.Select(col => ResultColumn(col, false)).Join(", ");
            results.Add(Join(" ", "RETURNING", returningSql));
        }

        if (insertStmt.OnConflict != null) {
            var conflictCols = insertStmt.OnConflict.ConflictColumns.Select(Identifier).Join(", ");
            results.Add(Join(" ", "ON CONFLICT", "(", conflictCols, ")"));

            if (insertStmt.OnConflict.Action is OnConflictNothing) {
                results.Add(new("DO NOTHING"));
            }
            else if (insertStmt.OnConflict.Action is OnConflictUpdate update) {
                var setSql = update.SetClauses.Select(set =>
                    Join(" ", Identifier(set.ColumnName), "=", Expr(set.Value))
                ).Join(", ");
                results.Add(Join(" ", "DO UPDATE SET", setSql));
            }
        }

        return results.Join("\n");
    }

    protected virtual ParameterizedSql UpdateStmt(UpdateStmt updateStmt) {
        var setClausesSql = updateStmt.SetClauses.Select(set =>
            Join(" ", Identifier(set.ColumnName), "=", Expr(set.Value))
        ).Join(", ");

        var results = new List<ParameterizedSql>();
        results.Add(Join(" ",
            "UPDATE", Identifier(updateStmt.TableName),
            "SET", setClausesSql
        ));

        if (updateStmt.Where != null) {
            var whereSql = Expr(updateStmt.Where);
            results.Add(Join(" ", "WHERE", whereSql));
        }

        if (updateStmt.Returning.Any()) {
            var returningSql = updateStmt.Returning.Select(col => ResultColumn(col, false)).Join(", ");
            results.Add(Join(" ", "RETURNING", returningSql));
        }

        return results.Join("\n");
    }

    protected virtual ParameterizedSql DeleteStmt(DeleteStmt deleteStmt) {
        var results = new List<ParameterizedSql>();
        results.Add(Join(" ", "DELETE FROM", Identifier(deleteStmt.TableName)));

        if (deleteStmt.Where != null) {
            var whereSql = Expr(deleteStmt.Where);
            results.Add(Join(" ", "WHERE", whereSql));
        }

        if (deleteStmt.Returning.Any()) {
            var returningSql = deleteStmt.Returning.Select(col => ResultColumn(col, false)).Join(", ");
            results.Add(Join(" ", "RETURNING", returningSql));
        }

        return results.Join("\n");
    }
}

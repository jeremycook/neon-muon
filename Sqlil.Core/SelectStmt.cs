using System.Linq.Expressions;

namespace Sqlil.Core;

/// <summary>
/// https://www.sqlite.org/lang_select.html
/// https://www.sqlite.org/syntax/factored-select-stmt.html
/// </summary>
public record class SelectStmt(
    bool Recursive,
    StableList<CommonTableExpression> CommonTableExpressions,
    StableList<SelectCore> SelectCores,
    CompoundOperator CompoundOperator,
    StableList<OrderingTerm> OrderingTerms,
    Expr? Limit,
    Expr? Offset
) {
    public static SelectStmt Create(
        SelectCore SelectCore,
        StableList<OrderingTerm>? OrderingTerms = null,
        Expr? Limit = null,
        Expr? Offset = null
    ) {
        if (SelectCore is null) {
            throw new ArgumentNullException(nameof(SelectCore));
        }

        return new(
            Recursive: default,
            CommonTableExpressions: StableList<CommonTableExpression>.Empty,
            SelectCores: StableList.Create(SelectCore),
            CompoundOperator: default,
            OrderingTerms: OrderingTerms ?? StableList<OrderingTerm>.Empty,
            Limit: Limit,
            Offset: Offset
        );
    }

    public static SelectStmt Create(
        SelectCore SelectCore,
        params OrderingTerm[] OrderingTerms
    ) {
        if (OrderingTerms is null) {
            throw new ArgumentNullException(nameof(OrderingTerms));
        }

        return Create(
            SelectCore: SelectCore,
            OrderingTerms: StableList.Create(OrderingTerms)
        );
    }

    public override string ToString() => string.Join("\n", new[] {
        CommonTableExpressions.Any()
            ? "WITH " + (Recursive ? "RECURSIVE " : string.Empty) + string.Join(",\n", CommonTableExpressions)
            : string.Empty,
        string.Join(
            $"\n{(CompoundOperator == CompoundOperator.UnionAll ? "UNION ALL" : CompoundOperator.ToString().ToUpper())} ",
            SelectCores
        ),
        OrderingTerms.Any()
            ? "ORDER BY " + string.Join(", ", OrderingTerms)
            : string.Empty,
        Limit != null
            ? "LIMIT " + Limit + (Offset != null ? " OFFSET " + Offset : string.Empty)
            : string.Empty
    }.Where(x => x != string.Empty));
}

/// <summary>
/// https://www.sqlite.org/syntax/common-table-expression.html
/// </summary>
public record class CommonTableExpression(
    Identifier TableName,
    StableList<Identifier> ColumnNames,
    bool Materialized,
    SelectStmt SelectStmt
) {
    public override string ToString() =>
        TableName + " "
        + (ColumnNames.Any() ? string.Join(",\n\t", ColumnNames) + " " : string.Empty)
        + (Materialized ? "MATERIALIZED " : string.Empty)
        + "(" + SelectStmt + ")";
}

public interface SelectCore { }

public record class SelectCoreNormal(
    bool Distinct,
    StableList<ResultColumn> ResultColumns,
    StableList<TableOrSubquery> TableOrSubqueries,
    JoinClause? JoinClause,
    Expr? Where,
    StableList<Expr> GroupBys,
    Expr? Having,
    StableList<(string WindowName, WindowDefn WindowDefn)> Windows
) : SelectCore {

    public static SelectCoreNormal Create(
        TableOrSubquery TableOrSubquery,
        Expr? Where = null
    ) =>
        new(
            Distinct: false,
            ResultColumns: StableList.Create<ResultColumn>(ResultColumnAsterisk.Create()),
            TableOrSubqueries: StableList.Create<TableOrSubquery>(TableOrSubquery),
            JoinClause: null,
            Where: Where,
            GroupBys: StableList<Expr>.Empty,
            Having: null,
            Windows: StableList<(string, WindowDefn)>.Empty
        );

    public static SelectCoreNormal Create(
        StableList<ResultColumn> ResultColumns,
        TableOrSubqueryTable Table,
        Expr? Where = null
    ) =>
        new(
            Distinct: false,
            ResultColumns: ResultColumns,
            TableOrSubqueries: StableList.Create<TableOrSubquery>(Table),
            JoinClause: null,
            Where: Where,
            GroupBys: StableList<Expr>.Empty,
            Having: null,
            Windows: StableList<(string, WindowDefn)>.Empty
        );

    public override string ToString() =>
        string.Join(
            "\n",
            values: new string[]
            {
                // ALL is implied when distinct is false
                "SELECT "
                    + (Distinct ? "DISTINCT " : string.Empty)
                    + string.Join(",\n\t", ResultColumns),
                // Invalid SQL will result if both of these are set
                TableOrSubqueries.Any()
                    ? "FROM " + string.Join(", ", TableOrSubqueries)
                    : string.Empty,
                JoinClause != null ? "FROM " + string.Join(",\n\t", JoinClause) : string.Empty,
                Where != null ? "WHERE " + Where : string.Empty,
                GroupBys.Any() ? "GROUP BY " + string.Join(",\n\t", GroupBys) : string.Empty,
                Having != null ? "HAVING " + Having : string.Empty,
                Windows.Any() ? "WINDOW " + string.Join(",\n\t", Windows) : string.Empty,
            }.Where(x => x != string.Empty)
        );
}

public record class SelectCoreValues(StableList<StableList<Expr>> Values)
    : SelectCore { }

public enum CompoundOperator {
    UnionAll = 0,
    Union = 1,
    Intersect = 2,
    Except = 3,
}

/// <summary>
/// https://www.sqlite.org/syntax/ordering-term.html
/// </summary>
public record class OrderingTerm(
    Expr Expr,
    Identifier? CollationName = null,
    bool Desc = false,
    bool NullsLast = false
) {
    public static OrderingTerm Create(
        Expr Expr,
        Identifier? CollationName = null,
        bool Desc = false,
        bool NullsLast = false
    ) => new(
        Expr: Expr,
        CollationName: CollationName,
        Desc: Desc,
        NullsLast: NullsLast
    );

    public override string ToString() {
        return Expr
        + (CollationName != null ? " COLLATE " + CollationName : string.Empty)
        + (Desc ? " DESC" : string.Empty)
        + (NullsLast ? " NULL LAST" : string.Empty);
    }
}

/// <summary>
/// https://www.sqlite.org/syntax/expr.html
/// </summary>
public interface Expr { }

public record class ExprBindParameter(
    Type Type,
    string? SuggestedName = null
) : Expr {
    public static ExprBindParameter Create(
        Type Type,
        string? SuggestedName = null
    ) => new(
        Type: Type,
        SuggestedName: SuggestedName
    );

    public override string ToString() {
        return
            "@(" +
            (SuggestedName != null ? (SuggestedName + " AS ") : string.Empty) +
            Type.Name + ")";
    }
}

public record class ExprBinary(
    string Operator,
    Expr Left,
    Expr Right,
    ExpressionType? OperatorExpressionType = null
) : Expr {
    public static ExprBinary Create(ExpressionType Operator, Expr Left, Expr Right) => new(OperatorMap[Operator], Left, Right, OperatorExpressionType: Operator);
    public static ExprBinary Create(string Operator, Expr Left, Expr Right) => new(Operator, Left, Right);

    public override string ToString() => OperatorExpressionType switch {

        ExpressionType.AndAlso or
        ExpressionType.OrElse => "(" + Left + " " + Operator + " " + Right + ")",

        _ => Left + " " + Operator + " " + Right
    };

    public static readonly Dictionary<ExpressionType, string> OperatorMap = new() {
        {ExpressionType.NotEqual, "<>"},
        {ExpressionType.Equal, "="},
        {ExpressionType.Subtract, "-"},
        {ExpressionType.Add, "+"},
        {ExpressionType.Multiply, "*"},
        {ExpressionType.Divide, "/"},
        {ExpressionType.AndAlso, "AND"},
        {ExpressionType.OrElse, "OR"},
    };
}

public record class ExprColumn(
    Identifier ColumnName,
    Identifier? TableName = null,
    Identifier? SchemaName = null
) : Expr {
    public static ExprColumn Create(
        Identifier ColumnName
    ) => new(
        ColumnName: ColumnName
    );

    public static ExprColumn Create(
        Identifier TableName,
        Identifier ColumnName
    ) => new(
        TableName: TableName,
        ColumnName: ColumnName
    );

    public static ExprColumn Create(
        Identifier SchemaName,
        Identifier TableName,
        Identifier ColumnName
    ) => new(
        SchemaName: SchemaName,
        TableName: TableName,
        ColumnName: ColumnName
    );

    public override string ToString() {
        return Identifier.Join(SchemaName, TableName, ColumnName);
    }
}

public record class ExprLiteralInteger(
    int Value
) : Expr {
    public static ExprLiteralInteger Create(int Value) => new(Value);

    public override string ToString() => Value.ToString();
}

public record class ExprLiteralString(
    string Value
) : Expr {
    public static ExprLiteralString Create(string Value) => new(Value);

    public override string ToString() => "'" + Value.Replace("'", "''") + "'";
}

public record class ExprUnary(
    ExpressionType Operator,
    Expr Operand
) : Expr {
    public static ExprUnary Create(ExpressionType Operator, Expr Operand) => new(Operator, Operand);

    public override string ToString() => OperatorMap[Operator] + " " + Operand;

    public static readonly Dictionary<ExpressionType, string> OperatorMap = new() {
        {ExpressionType.Not, "NOT"},
    };
}

/// <summary>
/// https://www.sqlite.org/syntax/result-column.html
/// </summary>
public interface ResultColumn { }

public record class ResultColumnAsterisk() : ResultColumn {
    public static ResultColumnAsterisk Create() => new();

    public override string ToString() => "*";
}

public record class ResultColumnExpr(
    Expr Expr,
    Identifier? ColumnAlias = null
) : ResultColumn {
    public static ResultColumnExpr Create(Expr Expr, Identifier? ColumnAlias = null) =>
        new(Expr: Expr, ColumnAlias: ColumnAlias);

    public override string ToString() =>
        Expr.ToString()
        + (ColumnAlias != null ? " " + ColumnAlias : string.Empty);
}

public record class ResultColumnTable(
    Identifier TableName
) : ResultColumn {
    public static ResultColumnTable Create(Identifier TableName) =>
        new(TableName: TableName);

    public override string ToString() => $"{TableName}.*";
}

/// <summary>
/// https://www.sqlite.org/syntax/table-or-subquery.html
/// </summary>
public interface TableOrSubquery { }

public record class TableOrSubqueryTable(
    Identifier TableName,
    Identifier? SchemaName = null,
    Identifier? TableAlias = null,
    Identifier? IndexName = null
) : TableOrSubquery {
    public static TableOrSubqueryTable Create(
        Identifier TableName,
        Identifier? SchemaName = null,
        Identifier? TableAlias = null,
        Identifier? IndexName = null
    ) => new(
        TableName: TableName,
        SchemaName: SchemaName,
        TableAlias: TableAlias,
        IndexName: IndexName
    );

    public override string ToString() =>
        Identifier.Join(SchemaName, TableName)
        + (TableAlias != null ? " " + TableAlias : string.Empty)
        + (IndexName != null ? " INDEXED BY " + TableAlias : string.Empty);
}

public record class TableOrSubqueryFunction(
    Identifier TableFunctionName,
    StableList<Expr> Arguments,
    Identifier? SchemaName = null,
    Identifier? TableAlias = null
) : TableOrSubquery { }

public record class TableOrSubquerySelectStmts(
    StableList<SelectStmt> SelectStmts,
    Identifier? TableAlias = null
) : TableOrSubquery { }

public record class TableOrSubqueryTableOrSubqueries(
    StableList<TableOrSubquery> TableOrSubqueries
) : TableOrSubquery { }

public record class TableOrSubqueryJoin(JoinClause JoinClause) : TableOrSubquery { }

public record class JoinClause(
    TableOrSubquery TableOrSubquery,
    StableList<(
        JoinOperator JoinOperator,
        TableOrSubquery TableOrSubquery,
        JoinConstraint JoinConstraint
    )> Joins
) { }

public enum JoinOperator {
    Comma = 0,
    Left = 1,
    Right = 2,
    Full = 3,
    Inner = 4,
    Cross = 5,
}

public interface JoinConstraint { }

public record class JoinConstraintNone() : JoinConstraint { }

public record class JoinConstraintOn(Expr Expr) : JoinConstraint { }

public record class JoinConstraintUsing(StableList<Identifier> ColumnName)
    : JoinConstraint { }

public record class WindowDefn { }

public record class Identifier(string Name) {
    public static implicit operator Identifier(string Name) => new(Name);

    public static Identifier Create(string Name) => new(Name);

    public override string ToString() => "\"" + Name.Replace("\"\"", "\"") + "\"";

    public static string Join(params Identifier?[] args) {
        return string.Join('.', args.SkipWhile(x => x == null));
    }
}

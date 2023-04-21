using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Sqlil.Core.Syntax;

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

    public object[] ToSqlSegments() {
        var commonTableExpressions = CommonTableExpressions.Any()
            ? SyntaxHelpers.Concat(
                "WITH ",
                Recursive ? "RECURSIVE " : SyntaxHelpers.Empty,
                SyntaxHelpers.Join(",\n", CommonTableExpressions.Select(cte => cte.ToSqlSegments()))
            )
            : SyntaxHelpers.Empty;

        var selectCores = SyntaxHelpers.Join(
            CompoundOperator == CompoundOperator.UnionAll
                ? "\nUNION ALL "
                : "\n" + CompoundOperator.ToString().ToUpper() + " ",
            SelectCores.Select(sc => sc.ToSqlSegments())
        );

        var orderingTerms = OrderingTerms.Any()
            ? SyntaxHelpers.Concat("ORDER BY ", SyntaxHelpers.Join(", ", OrderingTerms.Select(ot => ot.ToSqlSegments())))
            : SyntaxHelpers.Empty;

        var limit = Limit != null
            ? SyntaxHelpers.Concat("LIMIT ", Limit.ToSqlSegments(), (Offset != null ? SyntaxHelpers.Concat(" OFFSET ", Offset.ToSqlSegments()) : SyntaxHelpers.Empty))
            : SyntaxHelpers.Empty;

        return SyntaxHelpers.Join("\n",
            commonTableExpressions,
            selectCores,
            orderingTerms,
            limit
        );
    }
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
    public object[] ToSqlSegments() => SyntaxHelpers.Join(" ",
        TableName,
        (ColumnNames.Any() ? SyntaxHelpers.Join(",\n", ColumnNames) : SyntaxHelpers.Empty),
        Materialized ? "MATERIALIZED" : string.Empty,
        "(", SelectStmt, ")"
    );
}

public interface SelectCore {
    object[] ToSqlSegments();
}

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
        TableOrSubquery TableOrSubquery,
        Expr? Where = null
    ) =>
        new(
            Distinct: false,
            ResultColumns: ResultColumns,
            TableOrSubqueries: StableList.Create<TableOrSubquery>(TableOrSubquery),
            JoinClause: null,
            Where: Where,
            GroupBys: StableList<Expr>.Empty,
            Having: null,
            Windows: StableList<(string, WindowDefn)>.Empty
        );

    public object[] ToSqlSegments() {
        // ALL is implied when distinct is false
        var select = "SELECT" + (Distinct ? " DISTINCT" : string.Empty);

        var columns = SyntaxHelpers.Join(",\n", ResultColumns.Select(rc => rc.ToSqlSegments()));

        // Invalid SQL will result if both TableOrSubqueries and JoinClause are set
        var tableOrSubqueries = TableOrSubqueries.Any()
            ? SyntaxHelpers.Concat("FROM ", SyntaxHelpers.Join(", ", TableOrSubqueries.Select(tos => tos.ToSqlSegments())))
            : SyntaxHelpers.Empty;

        var joinClause = JoinClause != null
            ? SyntaxHelpers.Concat("FROM ", JoinClause.ToSqlSegments())
            : SyntaxHelpers.Empty;

        var where = Where != null
            ? SyntaxHelpers.Concat("WHERE ", Where.ToSqlSegments())
            : SyntaxHelpers.Empty;

        var groupBys = GroupBys.Any()
            ? SyntaxHelpers.Concat("GROUP BY ", SyntaxHelpers.Join(",\n", GroupBys.Select(x => x.ToSqlSegments())))
            : SyntaxHelpers.Empty;

        var having = Having != null
            ? SyntaxHelpers.Concat("HAVING ", Having.ToSqlSegments())
            : SyntaxHelpers.Empty;

        var windows = Windows.Any()
            ? throw new NotImplementedException("WINDOW")
            : SyntaxHelpers.Empty;

        var result = SyntaxHelpers.Join("\n",
            select,
            columns,
            tableOrSubqueries,
            joinClause,
            where,
            groupBys,
            having,
            windows
        );
        return result;
    }
}

public record class SelectCoreValues(StableList<StableList<Expr>> Values)
    : SelectCore {

    public object[] ToSqlSegments() {
        throw new NotImplementedException();
    }
}

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

    public object[] ToSqlSegments() => SyntaxHelpers.Join(" ",
        Expr.ToSqlSegments(),
        CollationName != null ? SyntaxHelpers.Concat("COLLATE ", CollationName) : string.Empty,
        Desc ? "DESC" : string.Empty,
        NullsLast ? "NULL LAST" : string.Empty
    );
}

/// <summary>
/// https://www.sqlite.org/syntax/expr.html
/// </summary>
public interface Expr {
    object[] ToSqlSegments();
}

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

    public object[] ToSqlSegments() {
        return SyntaxHelpers.Concat(this);
    }
}

public record class ExprBinary(
    BinaryOperator Operator,
    Expr Left,
    Expr Right
) : Expr {

    public static ExprBinary Create(BinaryOperator Operator, Expr Left, Expr Right)
        => new(Operator, Left, Right);

    public static ExprBinary Create(ExpressionType ExpressionOperator, Expr Left, Expr Right)
        => new(ExpressionToOperator[ExpressionOperator], Left, Right);

    public object[] ToSqlSegments() => Operator switch {

        BinaryOperator.AndAlso or
        BinaryOperator.OrElse => SyntaxHelpers.Concat("(", Left.ToSqlSegments(), " ", OperatorToSql[Operator], " ", Right.ToSqlSegments(), ")"),

        _ => SyntaxHelpers.Concat(Left.ToSqlSegments(), " ", OperatorToSql[Operator], " ", Right.ToSqlSegments())
    };

    private static readonly Dictionary<BinaryOperator, string> OperatorToSql = new() {
        {BinaryOperator.AndAlso, "AND"},
        {BinaryOperator.OrElse, "OR"},

        {BinaryOperator.NotEqual, "<>"},
        {BinaryOperator.Equal, "="},
        {BinaryOperator.LessThan, "<"},
        {BinaryOperator.LessThanOrEqual, "<="},
        {BinaryOperator.GreaterThan, ">"},
        {BinaryOperator.GreaterThanOrEqual, ">="},

        {BinaryOperator.Add, "+"},
        {BinaryOperator.Subtract, "-"},
        {BinaryOperator.Multiply, "*"},
        {BinaryOperator.Divide, "/"},

        {BinaryOperator.Like, "LIKE"},
    };

    private static readonly Dictionary<ExpressionType, BinaryOperator> ExpressionToOperator = new() {
        {ExpressionType.AndAlso, BinaryOperator.AndAlso},
        {ExpressionType.OrElse, BinaryOperator.OrElse},

        {ExpressionType.NotEqual, BinaryOperator.AndAlso},
        {ExpressionType.Equal, BinaryOperator.Equal},
        {ExpressionType.LessThan, BinaryOperator.LessThan},
        {ExpressionType.LessThanOrEqual, BinaryOperator.LessThanOrEqual},
        {ExpressionType.GreaterThan, BinaryOperator.GreaterThan},
        {ExpressionType.GreaterThanOrEqual, BinaryOperator.GreaterThanOrEqual},

        {ExpressionType.Add, BinaryOperator.Add},
        {ExpressionType.Subtract, BinaryOperator.Subtract},
        {ExpressionType.Multiply, BinaryOperator.Multiply},
        {ExpressionType.Divide, BinaryOperator.Divide},
    };
}

public enum BinaryOperator {
    AndAlso,
    OrElse,

    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,

    Add,
    Subtract,
    Multiply,
    Divide,

    Like,
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

    public object[] ToSqlSegments() {
        return Identifier.Join(SchemaName, TableName, ColumnName);
    }
}

public record class ExprLiteralInteger(
    int Value
) : Expr {
    public static ExprLiteralInteger Create(int Value) => new(Value);

    public object[] ToSqlSegments() {
        return SyntaxHelpers.Concat(Value.ToString());
    }
}

public record class ExprLiteralString(
    string Value
) : Expr {
    public static ExprLiteralString Create(string Value) => new(Value);

    public object[] ToSqlSegments() {
        return SyntaxHelpers.Concat("'" + Value.Replace("'", "''") + "'");
    }
}

public record class ExprUnary(
    UnaryOperator Operator,
    Expr Operand
) : Expr {
    public static ExprUnary Create(UnaryOperator Operator, Expr Operand)
        => new(Operator, Operand);
    public static ExprUnary Create(ExpressionType Operator, Expr Operand)
        => new(ExpressionTypeToOperator[Operator], Operand);

    public object[] ToSqlSegments() {
        return SyntaxHelpers.Concat(OperatorToString[Operator], " ", Operand.ToSqlSegments());
    }

    private static readonly Dictionary<UnaryOperator, string> OperatorToString = new() {
        {UnaryOperator.Not, "NOT"},
    };

    private static readonly Dictionary<ExpressionType, UnaryOperator> ExpressionTypeToOperator = new() {
        {ExpressionType.Not, UnaryOperator.Not},
    };
}

public enum UnaryOperator {
    Not,
}

/// <summary>
/// https://www.sqlite.org/syntax/result-column.html
/// </summary>
public interface ResultColumn {
    object[] ToSqlSegments();
}

public record class ResultColumnAsterisk() : ResultColumn {
    public static ResultColumnAsterisk Create() => new();

    public object[] ToSqlSegments() {
        return new[] { "*" };
    }
}

public record class ResultColumnExpr(
    Expr Expr,
    Identifier? ColumnAlias = null
) : ResultColumn {
    public static ResultColumnExpr Create(Expr Expr, Identifier? ColumnAlias = null) =>
        new(Expr: Expr, ColumnAlias: ColumnAlias);

    public object[] ToSqlSegments() => SyntaxHelpers.Concat(
        Expr.ToSqlSegments(),
        ColumnAlias != null && (Expr is not ExprColumn exprColumn || exprColumn.ColumnName != ColumnAlias)
            ? SyntaxHelpers.Concat(" ", ColumnAlias)
            : SyntaxHelpers.Empty
    );
}

public record class ResultColumnTable(
    Identifier TableName
) : ResultColumn {
    public static ResultColumnTable Create(Identifier TableName) =>
        new(TableName: TableName);

    public object[] ToSqlSegments() => SyntaxHelpers.Concat(TableName, ".*");
}

/// <summary>
/// https://www.sqlite.org/syntax/table-or-subquery.html
/// </summary>
public interface TableOrSubquery {
    object[] ToSqlSegments();
}

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

    public object[] ToSqlSegments() => SyntaxHelpers.Join(" ",
        Identifier.Join(SchemaName, TableName),
        TableAlias != null ? TableAlias : SyntaxHelpers.Empty,
        IndexName != null ? SyntaxHelpers.Concat("INDEXED BY ", IndexName) : SyntaxHelpers.Empty
    );
}

public record class TableOrSubqueryFunction(
    Identifier TableFunctionName,
    StableList<Expr> Arguments,
    Identifier? SchemaName = null,
    Identifier? TableAlias = null
) : TableOrSubquery {
    public object[] ToSqlSegments() {
        throw new NotImplementedException();
    }
}

public record class TableOrSubquerySelectStmts(
    StableList<SelectStmt> SelectStmts,
    Identifier? TableAlias = null
) : TableOrSubquery {
    public object[] ToSqlSegments() {
        throw new NotImplementedException();
    }
}

public record class TableOrSubqueryTableOrSubqueries(
    StableList<TableOrSubquery> TableOrSubqueries
) : TableOrSubquery {
    public object[] ToSqlSegments() {
        throw new NotImplementedException();
    }
}

public record class TableOrSubqueryJoin(JoinClause JoinClause) : TableOrSubquery {
    public object[] ToSqlSegments() {
        throw new NotImplementedException();
    }
}

public record class JoinClause(
    TableOrSubquery TableOrSubquery,
    StableList<(
        JoinOperator JoinOperator,
        TableOrSubquery TableOrSubquery,
        JoinConstraint JoinConstraint
    )> Joins
) {
    internal object[] ToSqlSegments() {
        throw new NotImplementedException();
    }
}

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

    [return: NotNullIfNotNull(nameof(Name))]
    public static implicit operator Identifier?(string? Name) => Name != null ? new(Name) : null;

    public static Identifier Create(string Name) => new(Name);

    public override string ToString() {
        return "\"" + Name.Replace("\"\"", "\"") + "\"";
    }

    public static object[] Join(params Identifier?[] args) {
        return SyntaxHelpers.Join(".", args.Select(x => x?.ToString()));
    }
}

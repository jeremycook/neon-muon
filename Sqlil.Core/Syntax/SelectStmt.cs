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
}

/// <summary>
/// https://www.sqlite.org/syntax/common-table-expression.html
/// </summary>
public record class CommonTableExpression(
    TableName TableName,
    StableList<ColumnName> ColumnNames,
    bool Materialized,
    SelectStmt SelectStmt
) { }

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
}

public record class SelectCoreValues(
    StableList<StableList<Expr>> Values
) : SelectCore { }

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
    CollationName? CollationName = null,
    bool Desc = false,
    bool NullsLast = false
) {
    public static OrderingTerm Create(
        Expr Expr,
        CollationName? CollationName = null,
        bool Desc = false,
        bool NullsLast = false
    ) => new(
        Expr: Expr,
        CollationName: CollationName,
        Desc: Desc,
        NullsLast: NullsLast
    );
}

/// <summary>
/// https://www.sqlite.org/syntax/expr.html
/// </summary>
public interface Expr {
    Type Type { get; }
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
}

public record class ExprBinary(
    BinaryOperator Operator,
    Expr Left,
    Expr Right
) : Expr {
    public Type Type => typeof(bool);

    public static ExprBinary Create(BinaryOperator Operator, Expr Left, Expr Right)
        => new(Operator, Left, Right);

    public static ExprBinary Create(ExpressionType ExpressionOperator, Expr Left, Expr Right)
        => new(BinaryConstants.ExpressionToOperator[ExpressionOperator], Left, Right);
}

public static class BinaryConstants {
    public static readonly Dictionary<BinaryOperator, string> OperatorToSql = new() {
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

    public static readonly Dictionary<ExpressionType, BinaryOperator> ExpressionToOperator = new() {
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
    ColumnName ColumnName,
    TableName? TableName = null,
    SchemaName? SchemaName = null
) : Expr {
    public Type Type => ColumnName.Type;

    public static ExprColumn Create(
        ColumnName ColumnName
    ) => new(
        ColumnName: ColumnName
    );

    public static ExprColumn Create(
        TableName TableName,
        ColumnName ColumnName
    ) => new(
        TableName: TableName,
        ColumnName: ColumnName
    );

    public static ExprColumn Create(
        SchemaName SchemaName,
        TableName TableName,
        ColumnName ColumnName
    ) => new(
        SchemaName: SchemaName,
        TableName: TableName,
        ColumnName: ColumnName
    );
}

public record class ExprLiteralInteger(
    int Value
) : Expr {
    public Type Type => typeof(int);

    public static ExprLiteralInteger Create(int Value) => new(Value);
}

public record class ExprLiteralString(
    string Value
) : Expr {
    public Type Type => typeof(string);

    public static ExprLiteralString Create(string Value) => new(Value);
}

public record class ExprUnary(
    UnaryOperator Operator,
    Expr Operand
) : Expr {
    public Type Type => Operand.Type;

    public static ExprUnary Create(UnaryOperator Operator, Expr Operand)
        => new(Operator, Operand);
    public static ExprUnary Create(ExpressionType Operator, Expr Operand)
        => new(UnaryConstants.ExpressionTypeToOperator[Operator], Operand);
}

public static class UnaryConstants {
    public static readonly Dictionary<UnaryOperator, string> OperatorToString = new() {
        {UnaryOperator.Not, "NOT"},
    };

    public static readonly Dictionary<ExpressionType, UnaryOperator> ExpressionTypeToOperator = new() {
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
}

// TODO: Ask for and require TableType
public record class ResultColumnAsterisk() : ResultColumn {
    public static ResultColumnAsterisk Create() => new();
}

public record class ResultColumnExpr(
    Expr Expr,
    ColumnName? ColumnAlias = null
) : ResultColumn {
    public static ResultColumnExpr Create(Expr Expr, ColumnName? ColumnAlias = null) =>
        new(Expr: Expr, ColumnAlias: ColumnAlias);
}

public record class ResultColumnTable(
    TableName TableName
) : ResultColumn {
    public static ResultColumnTable Create(TableName TableName) =>
        new(TableName: TableName);
}

/// <summary>
/// https://www.sqlite.org/syntax/table-or-subquery.html
/// </summary>
public interface TableOrSubquery { }

public record class TableOrSubqueryTable(
    SchemaName? SchemaName,
    TableName TableName,
    TableName? TableAlias = null,
    ColumnName? IndexName = null
) : TableOrSubquery {

    public static TableOrSubqueryTable Create(
        SchemaName SchemaName,
        TableName TableName,
        TableName? TableAlias
    ) => new(
        SchemaName: SchemaName,
        TableName: TableName,
        TableAlias: TableAlias,
        IndexName: null
    );

    public static TableOrSubqueryTable Create(
        TableName TableName,
        TableName? TableAlias
    ) => new(
        SchemaName: null,
        TableName: TableName,
        TableAlias: TableAlias,
        IndexName: null
    );

    public static TableOrSubqueryTable Create(
        TableName TableName
    ) => new(
        SchemaName: null,
        TableName: TableName,
        TableAlias: null,
        IndexName: null
    );
}

public record class TableOrSubqueryFunction(
    FunctionName TableFunctionName,
    StableList<Expr> Arguments,
    SchemaName? SchemaName = null,
    TableName? TableAlias = null
) : TableOrSubquery {
}

public record class TableOrSubquerySelectStmts(
    StableList<SelectStmt> SelectStmts,
    TableName? TableAlias = null
) : TableOrSubquery { }

public record class TableOrSubqueryTableOrSubqueries(
    StableList<TableOrSubquery> TableOrSubqueries
) : TableOrSubquery { }

public record class TableOrSubqueryJoin(
    JoinClause JoinClause
) : TableOrSubquery { }

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

public record class JoinConstraintUsing(StableList<ColumnName> ColumnNames)
    : JoinConstraint { }

public record class WindowDefn { }

public interface Identifier {
    string Name { get; }
}

public interface TypedIdentifier : Identifier {
    Type Type { get; }
}

public record CollationName(
    string Name
) : Identifier {
    public static CollationName Create(string Name) => new(Name);
}

public record SchemaName(
    string Name
) : Identifier {
    public static SchemaName Create(string Name) => new(Name);
}

public record TableName(
    string Name,
    Type Type
) : TypedIdentifier {
    public static TableName Create(string Name, Type Type) => new(Name, Type);
}

public record ColumnName(
    string Name,
    Type Type
) : TypedIdentifier {
    public static ColumnName Create(string Name, Type Type) => new(Name, Type);
}

public record FunctionName(
    string Name
) : Identifier {
    public static FunctionName Create(string Name) => new(Name);
}

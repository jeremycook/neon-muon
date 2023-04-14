using Sqlil.Core;

namespace Sqlil.Scratch;

public static class SqlCases {

    public static SelectStmt SimpleSelectStmt { get; } = SelectStmt.Create(
         SelectCore: SelectCoreNormal.Create(
             StableList.Create<ResultColumn>(
                 ResultColumnExpr.Create(
                     ExprColumn.Create("SomeColumn", "SomeAlias"),
                     Identifier.Create("SomeColumnAlias")
                 )
             ),
             TableOrSubqueryTable.Create("SomeTable", "SomeSchema", "SomeAlias")
         ),
         OrderingTerms: StableList.Create(OrderingTerm.Create(ExprColumn.Create("SomeColumn"))),
         Limit: ExprLiteralInteger.Create(Value: 50),
         Offset: ExprLiteralInteger.Create(Value: 100)
     );
}

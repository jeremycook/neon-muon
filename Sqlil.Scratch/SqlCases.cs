using Sqlil.Core.Syntax;

namespace Sqlil.Scratch;

public static class SqlCases {

    public static SelectStmt SimpleSelectStmt { get; } = SelectStmt.Create(
         SelectCore: SelectCoreNormal.Create(
             StableList.Create<ResultColumn>(
                 ResultColumnExpr.Create(
                     ExprColumn.Create(new("TableAlias", typeof(object)), new("MyColumn", typeof(string))),
                     ColumnName.Create("ColumnAlias", typeof(string))
                 )
             ),
             TableOrSubqueryTable.Create(SchemaName.Create("MySchema"), TableName.Create("MyTable", typeof(object)), TableName.Create("TableAlias", typeof(object)))
         ),
         OrderingTerms: StableList.Create(OrderingTerm.Create(ExprColumn.Create(ColumnName.Create("MyColumn", typeof(string))))),
         Limit: ExprLiteralInteger.Create(Value: 50),
         Offset: ExprLiteralInteger.Create(Value: 100)
     );
}

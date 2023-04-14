using Sqlil.Core.ExpressionTranslation;

namespace Sqlil.Scratch;

internal class Program {
    private static void Main(string[] args) {

        // new[] {
        //     new { Desc = "Simple", Sql = SelectStmt.Create(
        //         SelectCore: SelectCoreNormal.Create(
        //             StableList.Create<ResultColumn>(
        //                 ResultColumnExpr.Create(
        //                     ExprColumn.Create("SomeColumn", "SomeAlias"),
        //                     Identifier.Create("SomeColumnAlias")
        //                 )
        //             ),
        //             TableOrSubqueryTable.Create("SomeTable", "SomeSchema", "SomeAlias")
        //         ),
        //         OrderingTerms: StableList.Create(OrderingTerm.Create(ExprColumn.Create("SomeColumn"))),
        //         Limit: ExprLiteralInteger.Create(Value: 50),
        //         Offset: ExprLiteralInteger.Create(Value: 100)
        //     )},
        // }.Dump();

        // Lambda.Translate(() => UserContext
        //     .Users
        //     .OrderByDescending(u => u.Birthday)
        //     .Select(user => user.Username)
        //     .Skip(100)
        //     .Take(50)
        // , default).Dump();

        // Lambda.Translate(() => UserContext
        //     .Users
        //     .OrderBy(u => u.Birthday)
        //     .Select(user => new { user.Username, user.Birthday })
        //     .Skip(100)
        //     .Take(50)
        // , default).Dump();

        // Lambda.Translate((int number) => (1 + number) * 3, default).Dump();

        Lambda.Translate((bool isActive) => UserContext
            .Users
            .Where(us => us.IsActive == isActive && (
                us.Username == "Jeremy" ||
                us.Username.StartsWith("J") ||
                us.Username.Contains("erem") ||
                us.Username.EndsWith("y")
            ))
            .OrderByDescending(u => u.Birthday)
            .Select(user => new { user.Username, Disabled = !user.IsActive })
            .Skip(100)
            .Take(50)
        , default).Dump();
    }
}
using System.Linq.Expressions;

namespace Sqlil.Tests;

public class SqlilBuilderTests {
    [Theory]
    [MemberData(nameof(Maps))]
    [MemberData(nameof(Joins))]
    public void Test(string name, LambdaExpression expression) {

        var builder = new SqlilBuilder();
        var sqlil = builder.Build(expression);

        var sql = string.Join(" ", sqlil);
        Console.WriteLine(sql);
    }

    public static IEnumerable<object[]> Maps { get; } = new object[][] {
        new object[] { nameof(Shared.mapProperty), Shared.mapProperty },
        new object[] { nameof(Shared.mapAnon), Shared.mapAnon },
        new object[] { nameof(Shared.mapIdentity), Shared.mapIdentity },
        new object[] { nameof(Shared.mapTuple), Shared.mapTuple },
    };

    public static IEnumerable<object[]> Joins { get; } = new object[][] {
        new object[] { nameof(Shared.joinAnon), Shared.joinAnon },
        new object[] { nameof(Shared.joinProp), Shared.joinProp },
        new object[] { nameof(Shared.joinTuple), Shared.joinTuple },
        new object[] { nameof(Shared.multiJoinAnon), Shared.multiJoinAnon },
        //new object[] { nameof(Shared.groupJoinM), Shared.groupJoinM },
    };
}
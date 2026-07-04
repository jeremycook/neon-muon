using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Sqlil.Tests;

public class SqlilBuilderTests {
    private static readonly SqlilBuilder builder = new();

    [Theory]
    [MemberData(nameof(Shared))]
    public void Test(string name, LambdaExpression expression) {
        var sqlil = builder.Build(expression);
    }

    public static ImmutableArray<object[]> Shared { get; } = typeof(Shared).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
        .Select(f => new object[] { f.Name, f.GetValue(null)! })
        .ToImmutableArray();
}
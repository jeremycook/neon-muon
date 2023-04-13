using Spectre.Console;
using Sqlil.Core;

var examples = new[] {
    new { Desc = "Simple", Sql = SelectStmt.Create(
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
    )},
};

examples.Dump();

(StableList.Create(1, 2, 3) == StableList.Create(1, 2, 3)).Dump();
(StableList.Create(1, 2, 3) == StableList.Create(1, 2, 3, 4)).Dump("Not Equal");

// (ImmutableList.Create(OrderingTerm.Create(ExprColumn.Create("SomeColumn"))) == ImmutableList.Create(OrderingTerm.Create(ExprColumn.Create("SomeColumn")))).Dump("Equal");

public static class RenderableExtensions {

    public static T Dump<T>(this T item, string? label = null) {
        Spectre.Console.Rendering.IRenderable content;
        if (item == null) {
            content = new Text("NULL", new Style(decoration: Decoration.Italic));
        }
        else if (item is Spectre.Console.Rendering.IRenderable r) {
            content = r;
        }
        else if (typeof(T).IsArray) {
            content = ToRenderableGrid(item as dynamic);
        }
        else {
            content = new Text(item?.ToString() ?? string.Empty);
        }

        var panel = new Panel(content);
        if (label != null) {
            panel.Header = new(label);
        }

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        return item;
    }

    public static Grid ToRenderableGrid<T>(this IEnumerable<T> items) {
        Type type = typeof(T);

        var properties =
            type.GetProperties().Select(o => new { o.Name, GetValue = (Func<T, object?>)(x => o.GetValue(x)) })
            .Concat(type.GetFields().Select(o => new { o.Name, GetValue = (Func<T, object?>)(x => o.GetValue(x)) }))
            .ToArray();

        var grid = new Grid();
        grid.AddColumns(properties.Length);
        if (type.Name.StartsWith("ValueTuple`") == true) {
        }
        else {
            grid.AddRow(properties.Select(p => p.Name).ToArray());
        }

        foreach (var item in items) {
            grid.AddRow(properties.Select(p => p.GetValue(item)?.ToString() ?? string.Empty).ToArray());
        }

        return grid;
    }

    public static Table ToRenderableTable<T>(this IEnumerable<T> items) {
        Type type = typeof(T);

        var properties =
            type.GetProperties().Select(o => new { o.Name, GetValue = (Func<T, object?>)(x => o.GetValue(x)) })
            .Concat(type.GetFields().Select(o => new { o.Name, GetValue = (Func<T, object?>)(x => o.GetValue(x)) }))
            .ToArray();

        var grid = new Table();

        grid.AddColumns(properties.Select(p => p.Name).ToArray());

        foreach (var item in items) {
            grid.AddRow(properties.Select(p => p.GetValue(item)?.ToString() ?? string.Empty).ToArray());
        }

        return grid;
    }
}

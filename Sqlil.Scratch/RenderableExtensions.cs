using Spectre.Console;

namespace Sqlil.Scratch;

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

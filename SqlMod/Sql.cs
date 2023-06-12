using System.Collections.Immutable;

namespace SqlMod;

public readonly struct Sql {
    public static implicit operator Sql(FormattableString sql) {
        return Interpolate(sql);
    }

    public string Format { get; }
    public IReadOnlyCollection<object?> Arguments { get; }

    public Sql(string format, IEnumerable<object?> arguments) {
        Format = format;
        Arguments = arguments.ToArray();
    }

    public string Preview() {
        return string.Format(Format, args: Arguments.Select(a => a switch {
            Sql sql => sql.Preview(),
            SqlIdentifier id => (!string.IsNullOrEmpty(id.Prefix) ? id.Prefix + "." : string.Empty) + id.Value,
            SqlLiteral lit => lit.Value,
            _ => a?.ToString(),
        }).ToArray());
    }

    public override string ToString() {
        var commandText = ParameterizeSql(this);
        return $"{base.ToString()}: {commandText}";
    }

    private static string Quote(SqlIdentifier sqlIdentifier) {
        return
            (!string.IsNullOrEmpty(sqlIdentifier.Prefix) ? "\"" + sqlIdentifier.Prefix.Replace("\"", "\"\"") + "\"." : string.Empty) +
            (sqlIdentifier.Value == "*" ? "*" : "\"" + sqlIdentifier.Value.Replace("\"", "\"\"") + "\"");
    }

    private static string Quote(SqlLiteral sqlLiteral) {
        return "'" + sqlLiteral.Value.Replace("'", "''") + "'";
    }

    private static string ParameterizeSql(Sql sql) {
        var tempValues = new List<object>();
        var formatArgs = new List<string>(sql.Arguments.Count);

        foreach (var arg in sql.Arguments) {
            switch (arg) {
                case SqlIdentifier sqlIdentifier:
                    formatArgs.Add(Quote(sqlIdentifier));
                    break;

                case SqlLiteral sqlLiteral:
                    formatArgs.Add(Quote(sqlLiteral));
                    break;

                case Sql innerSql:
                    formatArgs.Add(GetParameterizedSql(innerSql, ref tempValues));
                    break;

                default:
                    formatArgs.Add($"${tempValues.Count + 1}");
                    tempValues.Add(arg ?? DBNull.Value);
                    break;
            }
        }

        string commandText = string.Format(sql.Format, args: formatArgs.ToArray());
        return commandText;
    }

    private static string GetParameterizedSql(Sql sql, ref List<object> parameterValues) {
        var formatArgs = new List<string>(sql.Arguments.Count);

        foreach (var arg in sql.Arguments) {
            switch (arg) {
                case SqlIdentifier sqlIdentifier:
                    formatArgs.Add(Quote(sqlIdentifier));
                    break;

                case SqlLiteral sqlLiteral:
                    formatArgs.Add(Quote(sqlLiteral));
                    break;

                case Sql innerSql:
                    formatArgs.Add(GetParameterizedSql(innerSql, ref parameterValues));
                    break;

                default:
                    formatArgs.Add($"${parameterValues.Count + 1}");
                    parameterValues.Add(arg ?? DBNull.Value);
                    break;
            }
        }

        return string.Format(sql.Format, args: formatArgs.ToArray());
    }

    public static Sql Empty { get; } = Raw(string.Empty);
    public static IEnumerable<Sql> EmptyEnumerable { get; } = new[] { Empty }.ToImmutableArray();

    public static Sql Raw(string text) {
        return new(text, Array.Empty<object?>());
    }

    public static Sql Join(string separator, IEnumerable<Sql> values) {
        return new(
            string.Join(separator, values.Select((c, i) => $"{{{i}}}")),
            values.Cast<object>());
    }

    public static Sql Join(string separator, IEnumerable<object?> values) {
        return new(
            string.Join(separator, values.Select((c, i) => $"{{{i}}}")),
            values);
    }

    public static Sql Interpolate(FormattableString formattableString) {
        return new(formattableString.Format, formattableString.GetArguments());
    }

    public static Sql Value(object? value) {
        return Interpolate($"{value}");
    }

    public static Sql Identifier(string text) {
        return Interpolate($"{new SqlIdentifier(text)}");
    }

    /// <summary>
    /// Returns "<paramref name="prefix"/>"."<paramref name="text"/>".
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public static Sql Identifier(string? prefix, string text) {
        return Interpolate($"{new SqlIdentifier(prefix, text)}");
    }

    /// <summary>
    /// Comma separated list of sanitized identifiers.
    /// </summary>
    /// <param name="texts"></param>
    /// <returns></returns>
    public static Sql IdentifierList(params string[] texts) {
        return IdentifierList((IEnumerable<string>)texts);
    }

    /// <summary>
    /// Comma separated list of sanitized identifiers.
    /// </summary>
    /// <param name="identifiers"></param>
    /// <returns></returns>
    public static Sql IdentifierList(IEnumerable<string> texts) {
        return Join(", ", texts.Select(t => new SqlIdentifier(t) as object));
    }

    public static Sql Literal(string text) {
        return Interpolate($"{new SqlIdentifier(text)}");
    }
}

public readonly struct SqlIdentifier {
    public SqlIdentifier(string value) {
        Value = value;
    }
    public SqlIdentifier(string? prefix, string value) {
        Prefix = prefix;
        Value = value;
    }

    public string? Prefix { get; init; }
    public string Value { get; init; }
}

public readonly struct SqlLiteral {
    public SqlLiteral(string value) {
        Value = value;
    }

    public string Value { get; init; }
}

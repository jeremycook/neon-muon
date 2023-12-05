namespace NeonMS.DataAccess;

public static class Quote
{
    private const char quote = '\'';
    private static readonly char[] escapedQuote = new[] { quote, quote };

    private const char backslash = '\\';
    private static readonly char[] escapedBackslash = new[] { backslash, backslash };

    private const string doubleQuote = "\"";
    private const string escapedDoubleQuote = "\"\"";

    public static string Identifier(string part)
    {
        return doubleQuote + part.Replace(doubleQuote, escapedDoubleQuote) + doubleQuote;
    }

    public static string Identifier(string separator, params string[] parts)
    {
        return string.Join(separator, values: parts.SkipWhile(p => p == string.Empty).Select(Identifier));
    }

    public static string Literal(string text)
    {
        return quote + string.Concat(text.SelectMany(ch =>
            ch == '\'' ? escapedQuote :
            ch == '\\' ? escapedBackslash :
            new[] { ch })
        ) + quote;
    }

    public static string Literal(DateTime moment)
    {
        if (moment.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"The {moment} argument must be in UTC.", nameof(moment));

        return Literal(moment.ToString("o"));
    }
}
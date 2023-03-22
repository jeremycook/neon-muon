namespace DataMod;

public readonly struct Sql
{
    public static implicit operator Sql(FormattableString sql)
    {
        return Interpolate(sql);
    }

    //	public static implicit operator NpgsqlBatchCommand(Sql sql)
    //	{
    //		NpgsqlBatchCommand batchCommand = new(sql.ToParameterizedSql(out var parameterValues));
    //		foreach (var value in parameterValues)
    //		{
    //			batchCommand.Parameters.Add(value);
    //		}
    //
    //		return batchCommand;
    //	}

    public string Format { get; }
    public IReadOnlyCollection<object?> Arguments { get; }

    public Sql(string format, IEnumerable<object?> arguments)
    {
        Format = format;
        Arguments = arguments.ToArray();
    }

    public string Preview()
    {
        return string.Format(Format, args: Arguments.Select(a => a switch
        {
            Sql sql => sql.Preview(),
            SqlIdentifier id => (id.Prefix is not null ? id.Prefix + "." : string.Empty) + id.Value,
            SqlLiteral lit => lit.Value,
            _ => a?.ToString(),
        }).ToArray());
    }

    //public override string ToString()
    //{
    //	return ToParameterizedSql(out _);
    //}
    //
    //	public string ToParameterizedSql(out NpgsqlParameter[] parameterValues)
    //	{
    //		var tempValues = new List<object>();
    //		var formatArgs = new List<string>(arguments.Length);
    //
    //		foreach (var arg in arguments)
    //		{
    //			switch (arg)
    //			{
    //				case Sql sql:
    //					formatArgs.Add(sql.GetParameterizedSql(ref tempValues));
    //					break;
    //
    //				default:
    //					formatArgs.Add($"${tempValues.Count + 1}");
    //					tempValues.Add(arg ?? DBNull.Value);
    //					break;
    //			}
    //		}
    //
    //		parameterValues = tempValues
    //			.Select(val => val switch
    //			{
    //				char[] charArray => new NpgsqlParameter() { Value = charArray, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.InternalChar },
    //				_ => new NpgsqlParameter() { Value = val },
    //			})
    //			.ToArray();
    //		return string.Format(format, args: formatArgs.ToArray());
    //	}
    //
    //	private string GetParameterizedSql(ref List<object> parameterValues)
    //	{
    //		var formatArgs = new List<string>(arguments.Length);
    //
    //		foreach (var arg in arguments)
    //		{
    //			switch (arg)
    //			{
    //				case Sql sql:
    //					formatArgs.Add(sql.GetParameterizedSql(ref parameterValues));
    //					break;
    //
    //				default:
    //					formatArgs.Add($"${parameterValues.Count + 1}");
    //					parameterValues.Add(arg ?? DBNull.Value);
    //					break;
    //			}
    //		}
    //
    //		return string.Format(format, args: formatArgs.ToArray());
    //	}

    public static Sql Empty { get; } = Raw(string.Empty);

    public static Sql Raw(string text)
    {
        return new(text, Array.Empty<object?>());
    }

    public static Sql Join(string separator, IEnumerable<Sql> values)
    {
        return new(
            string.Join(separator, values.Select((c, i) => $"{{{i}}}")),
            values.Cast<object>());
    }

    public static Sql Join(string separator, IEnumerable<object?> values)
    {
        return new(
            string.Join(separator, values.Select((c, i) => $"{{{i}}}")),
            values);
    }

    public static Sql Interpolate(FormattableString formattableString)
    {
        return new(formattableString.Format, formattableString.GetArguments());
    }

    public static Sql Value(object? value)
    {
        return Interpolate($"{value}");
    }

    public static Sql Identifier(string text)
    {
        return Interpolate($"{new SqlIdentifier(text)}");
    }

    /// <summary>
    /// Returns "<paramref name="prefix"/>"."<paramref name="text"/>".
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public static Sql Identifier(string? prefix, string text)
    {
        return Interpolate($"{new SqlIdentifier(prefix, text)}");
    }

    /// <summary>
    /// Comma separated list of sanitized identifiers.
    /// </summary>
    /// <param name="texts"></param>
    /// <returns></returns>
    public static Sql IdentifierList(params string[] texts)
    {
        return IdentifierList((IEnumerable<string>)texts);
    }

    /// <summary>
    /// Comma separated list of sanitized identifiers.
    /// </summary>
    /// <param name="identifiers"></param>
    /// <returns></returns>
    public static Sql IdentifierList(IEnumerable<string> texts)
    {
        return Join(", ", texts.Select(t => new SqlLiteral(t) as object));
    }

    public static Sql Literal(string text)
    {
        return Interpolate($"{new SqlIdentifier(text)}");
    }
}

public readonly struct SqlIdentifier
{
    public SqlIdentifier(string value)
    {
        Value = value;
    }
    public SqlIdentifier(string? prefix, string value)
    {
        Prefix = prefix;
        Value = value;
    }

    public string? Prefix { get; init; }
    public string Value { get; init; }
}

public readonly struct SqlLiteral
{
    public SqlLiteral(string value)
    {
        Value = value;
    }

    public string Value { get; init; }
}

using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using NeonMS.Authentication;
using NeonMS.Mvc;
using Npgsql;
using Npgsql.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NeonMS.DataAccess.InformationSchema;

[ApiController]
[Route(MvcConstants.StandardApiRoute)]
public class QueryController : ControllerBase
{
    /// <summary>
    /// Issues a batch of queries that is always rolled back.
    /// </summary>
    /// <param name="currentUser"></param>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut]
    [ActionName("Batch")]
    public async Task<ActionResult>
    BatchPUT(
        DB DB,
        CurrentUser currentUser,
        QueryInput input,
        CancellationToken cancellationToken
    )
    {
        using var con = await DB.OpenConnection(currentUser.Credential, input.Database, cancellationToken);
        using var tx = await con.BeginTransactionAsync(cancellationToken);

        List<IReadOnlyCollection<QueryColumn>> headers;
        List<IReadOnlyCollection<object?[]>> results;
        try
        {
            (headers, results) = await QueryAsync(con, tx, input, cancellationToken);
        }
        finally
        {
            await tx.RollbackAsync(cancellationToken);
        }

        return Ok(new
        {
            Headers = headers,
            Results = results,
        });
    }

    /// <summary>
    /// Issues a batch of queries that will be committed if all succeed.
    /// </summary>
    /// <param name="currentUser"></param>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [ActionName("Batch")]
    public async Task<ActionResult>
    BatchPOST(
        DB DB,
        CurrentUser currentUser,
        QueryInput input,
        CancellationToken cancellationToken
    )
    {
        using var con = await DB.OpenConnection(currentUser.Credential, input.Database, cancellationToken);
        using var tx = await con.BeginTransactionAsync(cancellationToken);

        var (headers, results) = await QueryAsync(con, tx, input, cancellationToken);

        await tx.CommitAsync(cancellationToken);

        return Ok(new
        {
            Headers = headers,
            Results = results,
        });
    }

    private static async Task<(List<IReadOnlyCollection<QueryColumn>>, List<IReadOnlyCollection<object?[]>>)>
    QueryAsync(
        NpgsqlConnection con,
        NpgsqlTransaction tx,
        QueryInput input,
        CancellationToken cancellationToken
    )
    {
        var headers = new List<IReadOnlyCollection<QueryColumn>>();
        var results = new List<IReadOnlyCollection<object?[]>>();

        foreach (var action in input.Actions)
        {
            if (action.IsSelect())
            {
                using var cmd = con.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = action.Sql;
                foreach (var parameter in action.Parameters)
                {
                    cmd.Parameters.Add(new() { Value = parameter });
                }

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                if (action.Headers)
                {
                    IReadOnlyCollection<NpgsqlDbColumn> schema = await reader.GetColumnSchemaAsync(cancellationToken);
                    var header = schema.Select(col => new QueryColumn(col)).ToArray();
                    headers.Add(header);
                }
                else
                {
                    headers.Add(Array.Empty<QueryColumn>());
                }

                var list = new List<object?[]>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    object?[] record = new object?[reader.FieldCount];
                    reader.GetValues(record!);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (record[i] == DBNull.Value)
                        {
                            record[i] = null;
                        }
                        else if (record[i] is string text && reader.GetDataTypeName(i) == "json")
                        {
                            record[i] = JsonDocument.Parse(text);
                        }
                    }
                    list.Add(record);
                }
                results.Add(list);
            }
            else
            {
                using var cmd = con.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = action.Sql;
                foreach (var parameter in action.Parameters)
                {
                    cmd.Parameters.Add(new() { Value = parameter.GetString() });
                }

                var affectedRows = await cmd.ExecuteNonQueryAsync(cancellationToken);

                headers.Add(Array.Empty<QueryColumn>());
                results.Add(new[] { new object?[] { affectedRows } });
            }
        }

        return (headers, results);
    }

    private static async Task<(List<IReadOnlyCollection<QueryColumn>>, List<IReadOnlyCollection<object?[]>>)>
    QueryAsync(
        NpgsqlBatch batch,
        QueryInput input,
        CancellationToken cancellationToken
    )
    {
        var headers = new List<IReadOnlyCollection<QueryColumn>>();
        var results = new List<IReadOnlyCollection<object?[]>>();

        using var reader = await batch.ExecuteReaderAsync(cancellationToken);

        foreach (var action in input.Actions)
        {
            if (action.Headers)
            {
                IReadOnlyCollection<NpgsqlDbColumn> schema = await reader.GetColumnSchemaAsync(cancellationToken);
                var header = schema.Select(col => new QueryColumn(col)).ToArray();
                headers.Add(header);
            }
            else
            {
                headers.Add(Array.Empty<QueryColumn>());
            }

            var list = new List<object?[]>();
            while (await reader.ReadAsync(cancellationToken))
            {
                object?[] record = new object?[reader.FieldCount];
                reader.GetValues(record!);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (record[i] == DBNull.Value)
                    {
                        record[i] = null;
                    }
                }
                list.Add(record);
            }
            results.Add(list);

            await reader.NextResultAsync(cancellationToken);
        }

        return (headers, results);
    }
}

public class QueryColumn
{
    public QueryColumn(string dataTypeName, string columnName)
    {
        DataTypeName = dataTypeName;
        ColumnName = columnName;
    }

    public QueryColumn(NpgsqlDbColumn col)
    {
        DataTypeName = col.DataTypeName;
        ColumnName = col.ColumnName;
        ColumnOrdinal = col.ColumnOrdinal;
        ColumnSize = col.ColumnSize;
        AllowDBNull = col.AllowDBNull;
        IsAutoIncrement = col.IsAutoIncrement;
        IsIdentity = col.IsIdentity;
        IsKey = col.IsKey;
        IsReadOnly = col.IsReadOnly;
        IsUnique = col.IsUnique;
    }

    public string DataTypeName { get; }
    public string ColumnName { get; }
    public int? ColumnOrdinal { get; }
    public int? ColumnSize { get; }
    public bool? AllowDBNull { get; }
    public bool? IsAutoIncrement { get; }
    public bool? IsIdentity { get; }
    public bool? IsKey { get; }
    public bool? IsReadOnly { get; }
    public bool? IsUnique { get; }
}

public class QueryInput
{
    [Required] public string Database { get; set; } = string.Empty;
    [Required, MinLength(1)] public QueryAction[] Actions { get; set; } = [];
}

public class QueryAction
{
    public QueryActionType Type { get; set; }
    public bool Headers { get; set; } = false;
    public int? Page { get; set; }
    public int? Range { get; set; }
    public string Sql { get; set; } = null!;
    public JsonElement[] Parameters { get; set; } = [];

    public bool IsSelect()
    {
        return Sql.TrimStart().StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase);
    }
}

public enum QueryActionType
{
    List = 0,
    Scalar = 1,
    Execute = 2,
}

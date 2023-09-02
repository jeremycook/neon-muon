using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using NeonMS.Authentication;
using Npgsql;
using Npgsql.Schema;
using System.ComponentModel.DataAnnotations;

namespace NeonMS.DataAccess.InformationSchema;

[ApiController]
[Route("[controller]/[action]")]
public class QueryController : ControllerBase
{
    [HttpPut]
    [ActionName("Batch")]
    public async Task<ActionResult> BatchPUT(
        CancellationToken ct,
        CurrentUser currentUser,
        QueryInput input
    )
    {
        using var batch = new NpgsqlBatch();
        foreach (var action in input.Actions)
        {
            var cmd = new NpgsqlBatchCommand(action.Sql);
            cmd.Parameters.AddRange(action.Parameters);

            batch.BatchCommands.Add(cmd);
        }

        using var con = DB.Connection(currentUser.Credential, input.Database);
        await con.OpenAsync(ct);
        using var tx = await con.BeginTransactionAsync(ct);

        batch.Connection = con;
        var (headers, results) = await QueryAsync(batch, input, ct);

        return Ok(new
        {
            Headers = headers,
            Results = results,
        });
    }

    [HttpPost]
    [ActionName("Batch")]
    public async Task<ActionResult> BatchPOST(
        CancellationToken ct,
        CurrentUser currentUser,
        QueryInput input
    )
    {
        using var batch = new NpgsqlBatch();
        foreach (var action in input.Actions)
        {
            var cmd = new NpgsqlBatchCommand(action.Sql);
            cmd.Parameters.AddRange(action.Parameters);

            batch.BatchCommands.Add(cmd);
        }

        using var con = DB.Connection(currentUser.Credential, input.Database);
        await con.OpenAsync(ct);
        batch.Connection = con;

        var (headers, results) = await QueryAsync(batch, input, ct);

        return Ok(new
        {
            Headers = headers,
            Results = results,
        });
    }

    private static async Task<(List<IReadOnlyCollection<QueryColumn>>, List<IReadOnlyCollection<object?[]>>)> QueryAsync(NpgsqlBatch batch, QueryInput input, CancellationToken ct)
    {
        var headers = new List<IReadOnlyCollection<QueryColumn>>();
        var results = new List<IReadOnlyCollection<object?[]>>();

        using (var reader = await batch.ExecuteReaderAsync(ct))
        {
            foreach (var action in input.Actions)
            {
                if (action.Headers)
                {
                    IReadOnlyCollection<NpgsqlDbColumn> schema = await reader.GetColumnSchemaAsync();
                    var header = schema.Select(col => new QueryColumn(col)).ToArray();
                    headers.Add(header);
                }
                else
                {
                    headers.Add(Array.Empty<QueryColumn>());
                }

                var list = new List<object?[]>();
                while (await reader.ReadAsync(ct))
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

                await reader.NextResultAsync(ct);
            }
        }

        return (headers, results);
    }
}

public class QueryColumn
{
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
    [Required] public string Database { get; set; } = null!;
    [Required, MinLength(1)] public QueryAction[] Actions { get; set; } = Array.Empty<QueryAction>();
}

public class QueryAction
{
    public QueryActionType Type { get; set; }
    public bool Headers { get; set; } = false;
    public int? Page { get; set; }
    public int? Range { get; set; }
    public string Sql { get; set; } = null!;
    public object?[] Parameters { get; set; } = Array.Empty<object?>();
}

public enum QueryActionType
{
    List = 0,
    Scalar = 1,
    Execute = 2,
}

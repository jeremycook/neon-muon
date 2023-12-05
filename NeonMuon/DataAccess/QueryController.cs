using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using NeonMuon.Authentication;
using NeonMuon.Mvc;
using Npgsql;
using Npgsql.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NeonMuon.DataAccess.InformationSchema;

[ApiController]
[Route(MvcConstants.StandardApiRoute)]
public class QueryController(
    DataServers DataServers,
    DB DB,
    CurrentUser CurrentUser
) : ControllerBase
{
    /// <summary>
    /// Issues a batch of queries that is always rolled back.
    /// </summary>
    [HttpPut]
    [ActionName("Batch")]
    public async Task<ActionResult<List<QueryResult>>>
    BatchPUT(
        QueryInput input,
        CancellationToken cancellationToken
    )
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        if (!DataServers.ContainsKey(input.Server))
        {
            ModelState.AddModelError("Server", "The Server was not found.");
            return ValidationProblem();
        }

        try
        {
            using var con = await DB.OpenConnection(CurrentUser.Credential(input.Server), input.Database, cancellationToken);
            using var tx = await con.BeginTransactionAsync(cancellationToken);

            try
            {
                var batchQueryResult = await QueryAsync(con, tx, input, cancellationToken);

                return Ok(batchQueryResult);
            }
            finally
            {
                // Never persist any changes
                await tx.RollbackAsync(cancellationToken);
            }
        }
        catch (PostgresException ex)
        {
            Log.SuppressedWarn<QueryController>(ex);

            var statusCode =
                ex.SqlState == "28P01" ? StatusCodes.Status401Unauthorized :
                StatusCodes.Status400BadRequest;
            ModelState.AddModelError("", $"PostgreSQL error: {ex.MessageText} ({ex.SqlState})");

            return StatusCode(statusCode, new ValidationProblemDetails(ModelState));
        }
    }

    /// <summary>
    /// Issues a batch of queries that will be committed if all succeed.
    /// </summary>
    [HttpPost]
    [ActionName("Batch")]
    public async Task<ActionResult<List<QueryResult>>>
    BatchPOST(
        QueryInput input,
        CancellationToken cancellationToken
    )
    {
        if (!DataServers.ContainsKey(input.Server))
        {
            return NotFound();
        }

        try
        {
            using var con = await DB.OpenConnection(CurrentUser.Credential(input.Server), input.Database, cancellationToken);
            using var tx = await con.BeginTransactionAsync(cancellationToken);

            var batchQueryResult = await QueryAsync(con, tx, input, cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return Ok(batchQueryResult);
        }
        catch (PostgresException ex)
        {
            Log.SuppressedWarn<QueryController>(ex);
            ModelState.AddModelError("", $"PostgreSQL error: {ex.MessageText} ({ex.SqlState})");
            return ValidationProblem();
        }
    }

    private static async Task<List<QueryResult>>
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

                var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                if (action.Columns)
                {
                    IReadOnlyCollection<NpgsqlDbColumn> schema = await reader.GetColumnSchemaAsync(cancellationToken);
                    var header = schema.Select(col => new QueryColumn(col)).ToArray();
                    headers.Add(header);
                }
                else
                {
                    headers.Add(Array.Empty<QueryColumn>());
                }

                var list = await reader.ListAsync(cancellationToken);
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

        var batch = headers.Zip(results)
            .Select(x => new QueryResult()
            {
                Columns = x.First,
                Rows = x.Second,
            })
            .ToList();

        return batch;
    }
}

public class QueryResult
{
    public required IReadOnlyCollection<QueryColumn> Columns { get; set; }
    public required IReadOnlyCollection<object?[]> Rows { get; set; }
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
    [Required] public string Server { get; set; } = "Main";
    [Required] public string Database { get; set; } = string.Empty;
    [Required, MinLength(1)] public QueryAction[] Actions { get; set; } = [];
}

public class QueryAction
{
    [Required] public required string Sql { get; set; }
    public bool Columns { get; set; } = false;
    public int? Page { get; set; }
    public int? Range { get; set; }
    public JsonElement[] Parameters { get; set; } = [];

    public bool IsSelect()
    {
        return Sql.TrimStart().StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase);
    }
}
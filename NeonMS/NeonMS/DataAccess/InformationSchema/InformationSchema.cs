using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Mvc;
using NeonMS.Authentication;

namespace NeonMS.DataAccess.InformationSchema;

[ApiController]
[Route("[controller]/[action]")]
public class InformationSchemaController : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<Columns>> Columns(
        CancellationToken cancellationToken,
        CurrentUser currentUser,
        string database
    )
    {
        using var dc = await DB.OpenDataConnection(currentUser.Credential, database, cancellationToken);

        var data = await dc
            .GetTable<Columns>()
            .ToArrayAsync();

        return data;
    }
}

[Table(Schema = "information_schema", Name = "columns")]
public class Columns
{
    [Column]
    public string table_catalog { get; set; } = null!;
    [Column]
    public string table_schema { get; set; } = null!;
    [Column]
    public string table_name { get; set; } = null!;
    [Column]
    public string column_name { get; set; } = null!;
}

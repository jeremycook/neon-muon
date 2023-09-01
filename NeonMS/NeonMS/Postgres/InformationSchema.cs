using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Mvc;
using NeonMS.Authentication;

namespace NeonMS.Postgres;

[ApiController]
[Route("[controller]/[action]")]
public class InformationSchemaController : ControllerBase
{
    public async Task<IReadOnlyList<information_schema.columns>> Columns()
    {
        using var dc = new DataConnection("Main");

        var data = await dc
            .GetTable<information_schema.columns>()
            .ToArrayAsync();

        return data;
    }
}

public static class information_schema
{
    [Table(Schema = nameof(information_schema), Name = nameof(columns))]
    public class columns
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
}

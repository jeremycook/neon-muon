using DatabaseMod.Models;
using Microsoft.Data.Sqlite;

namespace DataMod.Sqlite;

public static class SqliteDatabaseHelpers
{
    public static async ValueTask ContributeSqliteAsync(this Database database, SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        var sql = Sql.Raw("""
            SELECT
                m.name as TableName,
                m.type as TableType,
                c.cid as ColumnPosition,
                c.name as ColumnName,
                c.type as ColumnType,
                not c."notnull" as IsNullable,
                c.dflt_value as DefaultValueSql,
                c.pk as IsPrimaryKey
            FROM
                sqlite_master AS m
            JOIN
                pragma_table_info(m.name) AS c
            ORDER BY
                m.name,
                c.cid
            """);

        var tableColumnsList = await connection.ListAsync<TableColumns>(sql, cancellationToken);

        var schema = new Schema("*");
        database.Schemas.Add(schema);

        foreach (var tableMapping in tableColumnsList.GroupBy(o => o.TableName))
        {
            var table = schema.Tables.GetOrAdd(new Table(tableMapping.Key));

            foreach (var columnMapping in tableMapping)
            {
                table.Columns.GetOrAdd(new Column(
                    name: columnMapping.ColumnName,
                    storeType: columnMapping.ColumnType,
                    isNullable: columnMapping.IsNullable,
                    defaultValueSql: columnMapping.DefaultValueSql,
                    computedColumnSql: null)
                {
                    Position = columnMapping.ColumnPosition,
                });
            }

            //foreach (var tableIndex in tableMapping.Table.Indexes)
            //{
            //    var index = table.Indexes.GetOrAdd(new TableIndex(tableIndex.Name, tableIndex.IsUnique ? TableIndexType.UniqueConstraint : TableIndexType.Index)
            //    {
            //        Columns = tableIndex.Columns.OrderBy(o => o.Order).Select(c => c.Name).ToList(),
            //    });
            //}

            var primaryKeyColumns = tableMapping.Where(o => o.IsPrimaryKey);
            table.Indexes.GetOrAdd(new TableIndex("PK_" + table.Name, TableIndexType.PrimaryKey)
            {
                Columns = primaryKeyColumns.Select(c => c.ColumnName).ToList(),
            });
        }
    }

    private class TableColumns
    {
        public string TableName { get; set; }
        public string TableType { get; set; }
        public int ColumnPosition { get; set; }
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValueSql { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}

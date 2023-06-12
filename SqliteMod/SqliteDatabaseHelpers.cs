using DatabaseMod.Models;
using Microsoft.Data.Sqlite;
using SqlMod;
using static SqlMod.Sql;

namespace SqliteMod;

public static class SqliteDatabaseHelpers {

    // See: https://www.sqlite.org/datatype3.html#determination_of_column_affinity
    public static Sql StoreTypeToSqliteType(StoreType storeType) {
        return storeType switch {
            StoreType.Text => Raw("TEXT"),
            StoreType.Blob => Raw("BLOB"),
            StoreType.Currency => Raw("CURRENCY NUMERIC"),
            StoreType.Boolean => Raw("BOOLEAN INTEGER"),
            StoreType.Real => Raw("REAL"),
            StoreType.Uuid => Raw("UUID TEXT"),
            StoreType.Integer => Raw("INTEGER"),
            StoreType.Date => Raw("DATE TEXT"),
            StoreType.Time => Raw("TIME TEXT"),
            StoreType.Timestamp => Raw("TIMESTAMP TEXT"),
            _ => Raw(storeType.ToString().ToUpperInvariant() + " TEXT"),
        }; ; ;
    }

    public static StoreType DatabaseTypeToStoreType(string sqliteType) {
        var text = sqliteType.Split(' ')[0];
        var storeType = Enum.Parse<StoreType>(text, ignoreCase: true);
        return storeType;
    }

    public static void ContributeSqlite(this Database database, SqliteConnection connection) {
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

        var tableColumnsList = connection.List<TableColumns>(sql);

        if (database.Schemas.FirstOrDefault(o => o.Name == Schema.DefaultName) is not Schema schema) {
            schema = new();
            database.Schemas.Add(schema);
        }

        foreach (var tableMapping in tableColumnsList.GroupBy(o => o.TableName)) {
            var table = schema.Tables.GetOrAdd(new Table(tableMapping.Key));

            foreach (var columnMapping in tableMapping) {
                table.Columns.GetOrAdd(new Column(
                    name: columnMapping.ColumnName,
                    storeType: DatabaseTypeToStoreType(columnMapping.ColumnType),
                    isNullable: columnMapping.IsNullable,
                    defaultValueSql: columnMapping.DefaultValueSql,
                    computedColumnSql: null) {
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
            table.Indexes.GetOrAdd(new TableIndex(
                "pk_" + table.Name,
                TableIndexType.PrimaryKey,
                primaryKeyColumns.Select(c => c.ColumnName)
            ));
        }
    }

    private class TableColumns {
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

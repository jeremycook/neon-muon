using DatabaseMod.Models;
using Microsoft.Data.Sqlite;
using SqlMod;
using System.Text.Json;
using static SqlMod.Sql;

namespace SqliteMod;

public static class SqliteDatabaseHelpers {

    // See: https://www.sqlite.org/datatype3.html#determination_of_column_affinity
    public static Sql StoreTypeToSqliteType(StoreType storeType) {
        return storeType switch {
            StoreType.Text => Raw("TEXT"),
            StoreType.Blob => Raw("BLOB"),
            StoreType.Numeric => Raw("NUMERIC"),
            StoreType.Boolean => Raw("BOOLEAN INTEGER"),
            StoreType.Real => Raw("REAL"),
            StoreType.Uuid => Raw("UUID TEXT"),
            StoreType.Integer => Raw("INTEGER"),
            StoreType.Date => Raw("DATE TEXT"),
            StoreType.Time => Raw("TIME TEXT"),
            StoreType.Timestamp => Raw("TIMESTAMP TEXT"),
            _ => Raw(storeType.ToString().ToUpperInvariant() + " TEXT"),
        };
    }

    public static StoreType DatabaseTypeToStoreType(string sqliteType) {
        var text = sqliteType.Split(' ')[0];
        var storeType = Enum.Parse<StoreType>(text, ignoreCase: true);
        return storeType;
    }

    public static object? ConvertDatabaseValueToStoreValue(object databaseValue, StoreType storeType) {
        if (databaseValue == DBNull.Value) {
            return null;
        }

        return storeType switch {
            StoreType.Text => (string)databaseValue,
            StoreType.Blob => (byte[])databaseValue,
            StoreType.Numeric => (decimal)databaseValue,
            StoreType.Boolean => (bool)databaseValue,
            StoreType.Real => (double)databaseValue,
            StoreType.Uuid => Guid.Parse((string)databaseValue),
            StoreType.Integer => (long)databaseValue,
            StoreType.Date => DateOnly.Parse((string)databaseValue),
            StoreType.Time => TimeOnly.Parse((string)databaseValue),
            StoreType.Timestamp => DateTime.Parse((string)databaseValue),
            _ => throw new NotImplementedException(storeType.ToString()),
        };
    }

    public static object? ConvertJsonElementToStoreValue(JsonElement? jsonElement, StoreType storeType) {
        object? convertedValue = storeType switch {
            StoreType.Text => jsonElement?.GetString(),
            StoreType.Uuid => jsonElement?.GetGuid(),
            StoreType.Timestamp => jsonElement?.GetDateTime(),
            StoreType.Time => jsonElement?.GetDateTime() is DateTime dateTime ? TimeOnly.FromDateTime(dateTime) : null,
            StoreType.Real => jsonElement?.GetDouble(),
            StoreType.Integer => jsonElement?.GetInt64(),
            StoreType.Date => jsonElement?.GetDateTime() is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : null,
            StoreType.Numeric => jsonElement?.GetDecimal(),
            StoreType.Boolean => jsonElement?.GetBoolean(),
            _ => throw new NotImplementedException(storeType.ToString()),
        };
        return convertedValue;
    }

    public static void ContributeSqlite(this Database database, SqliteConnection connection) {
        var tableColumns = connection
            .List<ColumnRecord>(columnSql)
            .GroupBy(o => o.TableName);
        var tableIndexes = connection
            .List<IndexRecord>(indexSql)
            .GroupBy(g => g.TableName)
            .ToDictionary(g => g.Key, tableGroup => tableGroup
                .GroupBy(indexGroup => new { indexGroup.IndexName, indexGroup.IsNonUnique, indexGroup.IsUnique, indexGroup.IsPrimaryKey, indexGroup.Partial })
                .ToDictionary(x => x.Key, indexGroup => indexGroup.Select(y => new { y.ColumnName, y.SequenceInIndex }).ToArray())
            );

        if (database.Schemas.FirstOrDefault(o => o.Name == Schema.DefaultName) is not Schema schema) {
            schema = new();
            database.Schemas.Add(schema);
        }

        foreach (var tableMapping in tableColumns) {
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

            if (tableIndexes.TryGetValue(table.Name, out var indexes)) {
                foreach (var dbIndex in indexes) {
                    var index = table.Indexes.GetOrAdd(new TableIndex(
                        dbIndex.Key.IndexName,
                        dbIndex.Key.IsPrimaryKey ? TableIndexType.PrimaryKey
                            : dbIndex.Key.IsUnique ? TableIndexType.UniqueConstraint
                            : TableIndexType.Index,
                        dbIndex.Value.OrderBy(o => o.SequenceInIndex).Select(c => c.ColumnName).ToList()
                    ));
                }
            }

            if (!table.Indexes.Any(o => o.IndexType == TableIndexType.PrimaryKey)) {
                var primaryKeyColumns = tableMapping
                    .Where(o => o.PrimaryKey > 0)
                    .OrderBy(o => o.PrimaryKey);
                if (primaryKeyColumns.Any()) {
                    // In SQLite this this kind of primary key does not show up in the list of indexes
                    // We are making up the name of the primary key index
                    table.Indexes.GetOrAdd(new TableIndex(
                        "pk_" + table.Name,
                        TableIndexType.PrimaryKey,
                        primaryKeyColumns.Select(c => c.ColumnName)
                    ));
                }
            }
        }
    }

    private class ColumnRecord {
        public string TableName { get; set; } = null!;
        public string TableType { get; set; } = null!;
        public int ColumnPosition { get; set; }
        public string ColumnName { get; set; } = null!;
        public string ColumnType { get; set; } = null!;
        public bool IsNullable { get; set; }
        public string DefaultValueSql { get; set; } = null!;
        public int PrimaryKey { get; set; }
    }
    private static readonly Sql columnSql = Raw("""
        SELECT
            m.name as TableName,
            m.type as TableType,
            c.cid as ColumnPosition,
            c.name as ColumnName,
            c.type as ColumnType,
            not c."notnull" as IsNullable,
            c.dflt_value as DefaultValueSql,
            c.pk as PrimaryKey
        FROM
            sqlite_master AS m
        JOIN
            pragma_table_info(m.name) AS c
        WHERE m.name NOT IN ('sqlite_sequence')
        ORDER BY
            m.name,
            c.cid
    """);

    private class IndexRecord {
        public string TableName { get; set; } = null!;
        public string IndexName { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public bool IsPrimaryKey { get; set; }
        public bool IsNonUnique { get; set; }
        public bool IsUnique { get; set; }
        public int Partial { get; set; }
        public int SequenceInIndex { get; set; }
    }
    private static readonly Sql indexSql = Raw("""
        SELECT 
            m.tbl_name as TableName,
            il.name as IndexName,
            ii.name as ColumnName,
            CASE il.origin when 'pk' then 1 else 0 END as IsPrimaryKey,
            CASE il.[unique] when 1 then 0 else 1 END as IsNonUnique,
            il.[unique] as IsUnique,
            il.partial as Partial,
            il.seq as SequenceInIndex
        FROM sqlite_master AS m,
            pragma_index_list(m.name) AS il,
            pragma_index_info(il.name) AS ii
        GROUP BY
            m.tbl_name,
            il.name,
            ii.name,
            il.origin,
            il.partial,
            il.seq
        ORDER BY
            m.tbl_name,
            il.seq
    """);
}

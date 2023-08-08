using DatabaseMod.Models;
using Microsoft.Data.Sqlite;
using SqlMod;
using System.Text.Json;
using static SqlMod.Sql;

namespace SqliteMod;

public static class SqliteDatabaseHelpers
{
    // See: https://www.sqlite.org/datatype3.html#determination_of_column_affinity
    public static Sql StoreTypeToSqliteType(StoreType storeType)
    {
        return storeType switch
        {
            StoreType.General => Raw("GENERAL"),
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

    public static StoreType DatabaseTypeToStoreType(string sqliteType)
    {
        var text = sqliteType.Split(' ')[0];
        if (Enum.TryParse<StoreType>(text, ignoreCase: true, out var storeType))
        {
            return storeType;
        }
        else
        {
            return StoreType.General;
        }
    }

    public static object? ConvertDatabaseValueToStoreValue(object databaseValue, StoreType storeType)
    {
        if (databaseValue == DBNull.Value)
        {
            return null;
        }

        return storeType switch
        {
            StoreType.General => databaseValue,
            StoreType.Text => (string)databaseValue,
            StoreType.Blob => (byte[])databaseValue,
            StoreType.Numeric => databaseValue,
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

    public static object? ConvertJsonElementToStoreValue(JsonElement? jsonElement, StoreType storeType)
    {
        object? convertedValue = storeType switch
        {
            StoreType.General => jsonElement?.GetString(),
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

    public static void ContributeSqlite(this Database database, SqliteConnection connection)
    {
        var tableColumns = connection
            .List<ColumnRecord>(columnSql)
            .OrderBy(o => o.TableName).ThenBy(o => o.ColumnPosition).ThenBy(o => o.ColumnName)
            .GroupBy(o => o.TableName);
        var tableIndexes = connection
            .List<IndexRecord>(indexSql)
            .OrderBy(o => o.TableName).ThenBy(o => o.IndexName).ThenBy(o => o.SequenceInIndex)
            .GroupBy(g => g.TableName)
            .ToDictionary(g => g.Key, tableGroup => tableGroup
                .GroupBy(indexGroup => new { indexGroup.IndexName, indexGroup.IsNonUnique, indexGroup.IsUnique, indexGroup.IsPrimaryKey, indexGroup.Partial })
                .ToDictionary(x => x.Key, indexGroup => indexGroup.Select(y => new { y.ColumnName, y.SequenceInIndex }).ToArray())
            );

        if (database.Schemas.FirstOrDefault(o => o.Name == Schema.DefaultName) is not Schema schema)
        {
            schema = new();
            database.Schemas.Add(schema);
        }

        foreach (var tableMapping in tableColumns)
        {
            var table = schema.Tables.GetOrAdd(new Table(tableMapping.Key));

            foreach (var columnMapping in tableMapping)
            {
                table.Columns.GetOrAdd(new Column(
                    name: columnMapping.ColumnName,
                    storeType: DatabaseTypeToStoreType(columnMapping.ColumnType),
                    isNullable: columnMapping.IsNullable,
                    defaultValueSql: columnMapping.DefaultValueSql,
                    computedColumnSql: null)
                {
                    Position = columnMapping.ColumnPosition,
                });
            }

            if (tableIndexes.TryGetValue(table.Name, out var indexes))
            {
                foreach (var dbIndex in indexes)
                {
                    var index = table.Indexes.GetOrAdd(new TableIndex(
                        dbIndex.Key.IndexName,
                        dbIndex.Key.IsPrimaryKey ? TableIndexType.PrimaryKey
                            : dbIndex.Key.IsUnique ? TableIndexType.UniqueConstraint
                            : TableIndexType.Index,
                        dbIndex.Value.OrderBy(o => o.SequenceInIndex).Select(c => c.ColumnName).ToList()
                    ));
                }
            }

            if (!table.Indexes.Any(o => o.IndexType == TableIndexType.PrimaryKey))
            {
                var primaryKeyColumns = tableMapping
                    .Where(o => o.PrimaryKey > 0)
                    .OrderBy(o => o.PrimaryKey);
                if (primaryKeyColumns.Any())
                {
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

    private class ColumnRecord
    {
        public string TableName { get; set; } = null!;
        public string TableType { get; set; } = null!;
        public int ColumnPosition { get; set; }
        public string ColumnName { get; set; } = null!;
        public string ColumnType { get; set; } = null!;
        public bool IsNullable { get; set; }
        public string DefaultValueSql { get; set; } = null!;
        public int PrimaryKey { get; set; }
    }
    private static readonly Sql columnSql = Raw(
    """
    SELECT m.name AS TableName,
        m.type AS TableType,
        c.cid AS ColumnPosition,
        c.name AS ColumnName,
        c.type AS ColumnType,
        NOT c."notnull" AS IsNullable,
        c.dflt_value AS DefaultValueSql,
        c.pk AS PrimaryKey
    FROM sqlite_master AS m,
        pragma_table_info(m.name) AS c
    WHERE m.type = 'table' AND 
        m.name NOT IN ('sqlite_sequence');
    """);

    private class IndexRecord
    {
        public string TableName { get; set; } = null!;
        public string IndexName { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public bool IsPrimaryKey { get; set; }
        public bool IsNonUnique { get; set; }
        public bool IsUnique { get; set; }
        public int Partial { get; set; }
        public int SequenceInIndex { get; set; }
    }
    private static readonly Sql indexSql = Raw(
    """
    SELECT m.tbl_name AS TableName,
        il.name AS IndexName,
        ii.name AS ColumnName,
        CASE il.origin WHEN 'pk' THEN 1 ELSE 0 END AS IsPrimaryKey,
        CASE il."unique" WHEN 1 THEN 0 ELSE 1 END AS IsNonUnique,
        il."unique" AS IsUnique,
        il.partial AS Partial,
        il.seq AS SequenceInIndex
    FROM sqlite_master AS m,
        pragma_index_list(m.name) AS il,
        pragma_index_info(il.name) AS ii
    WHERE m.type = 'table'
    GROUP BY m.tbl_name,
            il.name,
            ii.name,
            il.origin,
            il.partial,
            il.seq;
    """);
}

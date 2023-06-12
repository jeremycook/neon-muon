using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using LogMod;
using Microsoft.Extensions.Logging;
using SqlMod;
using System.Data;
using static SqlMod.Sql;

namespace SqliteMod;

public static class SqliteDatabaseScripter {
    private static Sql ScriptColumnDefinition(IReadOnlyColumn column, string tableName, IReadOnlyCollection<string> primaryKey, bool creatingTable) {
        var isPrimaryKey = primaryKey.Contains(column.Name);
        return Join(" ", new Sql[]
        {
            Interpolate($"{Identifier(column.Name)} {ScriptStoreType(column.StoreType)}"),
            Raw(column.IsNullable ? "NULL" : "NOT NULL"),
            !isPrimaryKey ? (creatingTable ? ScriptCreateTableColumnDefault(column) : ScriptAddColumnDefault(column)) : Empty,
            !isPrimaryKey && !string.IsNullOrWhiteSpace(column.ComputedColumnSql) ? Interpolate($"GENERATED ALWAYS AS ({column.ComputedColumnSql}) STORED") : Empty,
            isPrimaryKey && primaryKey.Count == 1 && column.StoreType == StoreType.Integer ? Interpolate($"CONSTRAINT {Identifier("pk_" + tableName)} PRIMARY KEY AUTOINCREMENT") : Empty,
        }.Except(EmptyEnumerable));
    }

    private static Sql ScriptStoreType(StoreType storeType) {
        return SqliteDatabaseHelpers.StoreTypeToSqliteType(storeType);
    }

    // Sqlite cannot add a column to an existing table with a non-constant default.
    // However column defaults can be non-constant when creating the table.
    private static readonly Dictionary<StoreType, string> AddColumnDefaultValueSqlMap = new()
    {
        { StoreType.Boolean, "0" },
        { StoreType.Numeric, "0" },
        { StoreType.Integer, "0" },
        { StoreType.Real, "0" },
        { StoreType.Text, "''" },
    };

    private static Sql ScriptAddColumnDefault(IReadOnlyColumn column) {
        if (!string.IsNullOrWhiteSpace(column.DefaultValueSql)) {
            return Interpolate($"DEFAULT ({Raw(column.DefaultValueSql)})");
        }
        else if (!column.IsNullable && AddColumnDefaultValueSqlMap.TryGetValue(column.StoreType, out var defaultValueSql)) {
            return Interpolate($"DEFAULT ({Raw(defaultValueSql)})");
        }
        else {
            return Empty;
        }
    }

    private static readonly Dictionary<StoreType, string> CreateTableColumnDefaultValueSqlMap = new(AddColumnDefaultValueSqlMap)
    {
        { StoreType.Date, "date()" },
        { StoreType.Time, "time()" },
        { StoreType.Timestamp, "current_timestamp" },
        { StoreType.Uuid, "uuid()" },
    };

    private static Sql ScriptCreateTableColumnDefault(IReadOnlyColumn column) {
        if (!string.IsNullOrWhiteSpace(column.DefaultValueSql)) {
            return Interpolate($"DEFAULT ({Raw(column.DefaultValueSql)})");
        }
        else if (!column.IsNullable && CreateTableColumnDefaultValueSqlMap.TryGetValue(column.StoreType, out var defaultValueSql)) {
            return Interpolate($"DEFAULT ({Raw(defaultValueSql)})");
        }
        else {
            return Empty;
        }
    }


    public static List<Sql> ScriptAlterations(IEnumerable<DatabaseAlteration> alterations, bool suppressNotSupportedExceptions = false) {
        var script = new List<Sql>();

        foreach (var alteration in alterations) {
            try {
                Sql sql = ScriptAlteration(alteration);
                script.Add(sql);
            }
            catch (NotSupportedException ex) when (suppressNotSupportedExceptions) {
                Log.CreateLogger(typeof(SqliteDatabaseScripter)).LogInformation("Suppressed {Exception} to {Alteration} alteration", ex, alteration);
                script.Add(Raw("-- " + ex.Message));
            }
        }

        return script;
    }

    private static Sql ScriptAlteration(DatabaseAlteration alteration) {
        return alteration switch {
            CreateSchema createSchema => ScriptCreateSchema(createSchema),
            CreateTable createTable => ScriptCreateTable(createTable),
            DropTable dropTable => ScriptDropTable(dropTable),
            RenameTable renameTable => ScriptRenameTable(renameTable),
            ChangeTableOwner changeTableOwner => ScriptChangeTableOwner(changeTableOwner),
            CreateColumn addColumn => ScriptAddColumn(addColumn),
            AlterColumn alterColumn => ScriptAlterColumn(alterColumn),
            RenameColumn renameColumn => ScriptRenameColumn(renameColumn),
            DropColumn dropColumn => ScriptDropColumn(dropColumn),
            CreateIndex addIndex => ScriptCreateIndex(addIndex),
            AlterIndex alterIndex => ScriptAlterIndex(alterIndex),
            DropIndex dropIndex => ScriptDropIndex(dropIndex),
            _ => throw new NotImplementedException(alteration.GetType().AssemblyQualifiedName),
        };
    }


    private static Sql ScriptCreateSchema(CreateSchema _) {
        throw new NotSupportedException("Cannot create schemas");
    }


    private static Sql ScriptCreateTable(CreateTable change) {
        var columns = change.Columns.Select(column => ScriptColumnDefinition(column, change.TableName, change.PrimaryKey, creatingTable: true));

        if (change.PrimaryKey.Length == 1 &&
            change.Columns.First(c => c.Name == change.PrimaryKey[0]) is var pkColumn &&
            pkColumn.StoreType == StoreType.Integer) {
            // Skip because PK index is handled by ScriptColumnDefinition
        }
        else if (change.PrimaryKey.Any()) {
            columns = columns.Append(Interpolate($"CONSTRAINT {Identifier(change.SchemaName, "pk_" + change.TableName)} PRIMARY KEY ({Join(", ", change.PrimaryKey.Select(Identifier))})"));
        }

        return Interpolate($"CREATE TABLE {Identifier(change.SchemaName, change.TableName)} ({Join(", ", columns)});");
    }

    private static Sql ScriptDropTable(DropTable change) {
        return Interpolate($"DROP TABLE {Identifier(change.SchemaName, change.TableName)};");
    }

    private static Sql ScriptRenameTable(RenameTable change) {
        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} RENAME TO {Identifier(change.NewTableName)};");
    }

    private static Sql ScriptChangeTableOwner(ChangeTableOwner _) {
        throw new NotSupportedException("Cannot change table owner");
    }


    private static Sql ScriptAddColumn(CreateColumn change) {
        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} ADD COLUMN {ScriptColumnDefinition(change.Column, change.TableName, Array.Empty<string>(), creatingTable: false)};");
    }

    private static Sql ScriptRenameColumn(RenameColumn change) {
        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} RENAME COLUMN {Identifier(change.ColumnName)} TO {Identifier(change.NewColumnName)};");
    }

    private static Sql ScriptAlterColumn(AlterColumn change) {
        var commands = new List<Sql>();

        if (change.Modifications.Contains(AlterColumnModification.Default)) {
            if (!string.IsNullOrWhiteSpace(change.Column.DefaultValueSql)) {
                commands.Add(Interpolate($"ALTER COLUMN {Identifier(change.Column.Name)} SET DEFAULT ({change.Column.DefaultValueSql})"));
            }
            else {
                commands.Add(Interpolate($"ALTER COLUMN {Identifier(change.Column.Name)} DROP DEFAULT"));
            }
        }
        if (change.Modifications.Contains(AlterColumnModification.Nullability)) {
            commands.Add(Interpolate($"ALTER COLUMN {Identifier(change.Column.Name)} {(change.Column.IsNullable ? "DROP NOT NULL" : "SET NOT NULL")}"));
        }
        if (change.Modifications.Contains(AlterColumnModification.Type)) {
            throw new NotSupportedException("Altering a column's type is not currently supported.");
        }
        if (change.Modifications.Contains(AlterColumnModification.Generated)) {
            if (!string.IsNullOrWhiteSpace(change.Column.ComputedColumnSql)) {
                throw new NotSupportedException("Altering a generated column is not currently supported.");
            }
            else {
                commands.Add(Interpolate($"ALTER COLUMN {Identifier(change.Column.Name)} DROP EXPRESSION"));
            }
        }

        return commands.Any()
            ? Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} {Join(", ", commands)};")
            : Empty;
    }

    private static Sql ScriptDropColumn(DropColumn change) {
        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} DROP COLUMN {Identifier(change.ColumnName)};");
    }

    private static Sql ScriptCreateIndex(CreateIndex change) {
        if (change.Index.IndexType == TableIndexType.PrimaryKey) {
            throw new NotSupportedException("Cannot create primary key indexes");
        }

        var index = change.Index;
        return Interpolate($"CREATE {Raw(index.IndexType == TableIndexType.UniqueConstraint ? "UNIQUE INDEX" : "INDEX")} {Identifier(index.GetName(change.TableName))} ON {Identifier(change.TableName)} ({Join(", ", index.Columns.Select(Identifier))});");
    }

    private static Sql ScriptAlterIndex(AlterIndex change) {
        if (change.Index.IndexType == TableIndexType.PrimaryKey) {
            throw new NotSupportedException("Cannot alter primary key indexes");
        }

        var index = change.Index;
        return Interpolate($"""
DROP INDEX IF EXISTS {Identifier(index.GetName(change.TableName))};
CREATE {Raw(index.IndexType == TableIndexType.UniqueConstraint ? "UNIQUE INDEX" : "INDEX")} {Identifier(index.GetName(change.TableName))} ON {Identifier(change.TableName)} ({Join(", ", index.Columns.Select(Identifier))});
""");
    }

    private static Sql ScriptDropIndex(DropIndex change) {
        if (change.Index.IndexType == TableIndexType.PrimaryKey) {
            throw new NotSupportedException("Cannot drop primary key indexes");
        }

        var index = change.Index;
        return Interpolate($"DROP INDEX IF EXISTS {Identifier(index.GetName(change.TableName))};");
    }
}

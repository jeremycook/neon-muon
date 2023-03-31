using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;
using DataCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.RegularExpressions;
using static DataCore.Sql;

namespace DataMod.Sqlite;

public static class SqliteDatabaseScripter {
    /// <summary>
    /// Script missing objects. Useful when developing.
    /// </summary>
    /// <param name="database"></param>
    /// <returns></returns>
    public static List<Sql> ScriptIfNotExists(Database database) {
        var script = new List<Sql>();

        foreach (var schema in database.Schemas) {
            if (!string.IsNullOrEmpty(schema.Owner)) {
                script.Add(Interpolate($"SET ROLE {Identifier(schema.Owner)};"));
            }

            // Create missing schema
            // https://www.postgresql.org/docs/current/sql-createschema.html
            script.Add(Interpolate($"CREATE SCHEMA IF NOT EXISTS {Identifier(schema.Name)};"));
            script.Add(Empty);

            // Apply privileges
            foreach (var privilege in schema.Privileges) {
                script.Add(Interpolate($"GRANT {privilege.Privileges} ON SCHEMA {Identifier(schema.Name)} TO {Identifier(privilege.Grantee)};"));
            }
            script.Add(Empty);

            // Apply default privileges
            foreach (var privilege in schema.DefaultPrivileges) {
                script.Add(Interpolate($"ALTER DEFAULT PRIVILEGES IN SCHEMA {Identifier(schema.Name)} GRANT {privilege.Privileges} ON {privilege.ObjectType} TO {Identifier(privilege.Grantee)};"));
            }
            script.Add(Empty);

            foreach (var table in schema.Tables) {
                if (!string.IsNullOrEmpty(table.Owner)) {
                    script.Add(Interpolate($"SET ROLE {Identifier(table.Owner)};"));
                }
                else if (!string.IsNullOrEmpty(schema.Owner)) {
                    script.Add(Interpolate($"SET ROLE {Identifier(schema.Owner)};"));
                }

                // Create missing table
                // https://www.postgresql.org/docs/current/sql-createtable.html
                IEnumerable<string> tableParts;
                if (table.Indexes.FirstOrDefault(uc => uc.IndexType == TableIndexType.PrimaryKey) is TableIndex primaryKey) {
                    tableParts =
                        // Columns
                        table.Columns.Select(column =>
                            ScriptAddColumnDefinition(column) +
                            // Assume that integer primary keys are identity columns.
                            // TODO: Drive this from the Database model.
                            (primaryKey.Columns.Contains(column.Name) && column.StoreType == StoreType.Integer ? " GENERATED ALWAYS AS IDENTITY" : "")
                        )
                        // Primary key constraint
                        .Append($"CONSTRAINT {Identifier(primaryKey.GetName(table))} PRIMARY KEY ({Join(", ", primaryKey.Columns.Select(Identifier))})");
                }
                else {
                    tableParts = Enumerable.Empty<string>();
                }
                script.Add(Interpolate($"CREATE TABLE IF NOT EXISTS {Identifier(schema.Name, table.Name)} ({Join(", ", tableParts)});"));
                script.Add(Empty);

                // Add missing columns
                // https://www.postgresql.org/docs/current/sql-altertable.html
                foreach (var column in table.Columns) {
                    script.Add(Interpolate($"ALTER TABLE {Identifier(schema.Name, table.Name)} ADD COLUMN IF NOT EXISTS {ScriptAddColumnDefinition(column)};"));
                }
                script.Add(Empty);

                // Add missing unique constraints
                // https://www.postgresql.org/docs/current/sql-altertable.html
                var uniqueConstraints = table.Indexes.Where(uc => uc.IndexType != TableIndexType.Index);
                foreach (var constraint in uniqueConstraints) {
                    string indexType = constraint.IndexType == TableIndexType.PrimaryKey ? "PRIMARY KEY" : "UNIQUE";
                    script.Add(Interpolate($"""
IF NOT EXISTS (SELECT NULL FROM information_schema.table_constraints WHERE constraint_schema = {Literal(schema.Name)} AND constraint_name = {Literal(constraint.GetName(table))})
THEN
    ALTER TABLE {Identifier(schema.Name, table.Name)} ADD CONSTRAINT {Identifier(constraint.GetName(table))} {indexType} ({Join(", ", constraint.Columns.Select(Identifier))});
END IF;
"""));
                }
                if (uniqueConstraints.Any()) script.Add(Empty);

                // Add missing indexes
                // https://www.postgresql.org/docs/current/sql-createindex.html
                foreach (var index in table.Indexes.Where(o => o.IndexType == TableIndexType.Index)) {
                    script.Add(Interpolate($"CREATE INDEX IF NOT EXISTS {Identifier(index.GetName(table))} ON {Identifier(schema.Name, table.Name)} ({Join(", ", index.Columns.Select(Identifier))});"));
                }
                if (table.Indexes.Any()) script.Add(Empty);
            }

            script.Add(Empty);
        }

        return script;
    }

    private static Sql ScriptAddColumnDefinition(Column column) {
        return Join("", new object[]
        {
            Interpolate($"{Identifier(column.Name)} {ScriptStoreType(column.StoreType)} {Raw(column.IsNullable ? "NULL" : "NOT NULL")}"),
            ScriptColumnDefault(column),
            !string.IsNullOrEmpty(column.ComputedColumnSql) ? Interpolate($" GENERATED ALWAYS AS ({column.ComputedColumnSql}) STORED") : Empty,
        });
    }

    private static Sql ScriptStoreType(StoreType storeType) {
        return storeType switch {
            StoreType.Text => Raw("TEXT"),
            StoreType.Blob => Raw("BLOB"),
            StoreType.Boolean => Raw("INTEGER"),
            StoreType.Double => Raw("REAL"),
            StoreType.Guid => Raw("TEXT"),
            StoreType.Integer => Raw("INTEGER"),
            StoreType.Timestamp => Raw("TEXT"),
            _ => throw new NotImplementedException(storeType.ToString()),
        };
    }

    private static readonly Dictionary<StoreType, string> DefaultValueSqlMap = new()
    {
        { StoreType.Boolean, "false" },
        { StoreType.Integer, "0" },
        { StoreType.Guid, "gen_random_uuid()" },
        { StoreType.Text, "''" },
        { StoreType.Timestamp, "(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')" },
    };

    private static Sql ScriptColumnDefault(Column column) {
        if (!string.IsNullOrWhiteSpace(column.DefaultValueSql)) {
            if (column.DefaultValueSql != string.Empty) {
                return Interpolate($" DEFAULT {column.DefaultValueSql}");
            }
            else if (DefaultValueSqlMap.TryGetValue(column.StoreType, out var defaultValueSql)) {
                return Interpolate($" DEFAULT {Raw(defaultValueSql)}");
            }
        }

        return Empty;
    }

    public static List<Sql> ScriptAlterations(IEnumerable<DatabaseAlteration> alterations) {
        var script = new List<Sql>();

        foreach (var alteration in alterations) {
            switch (alteration) {
                case CreateSchema createSchema:
                    script.Add(ScriptCreateSchema(createSchema));
                    break;

                case CreateTable createTable:
                    script.Add(ScriptCreateTable(createTable));
                    break;
                case RenameTable renameTable:
                    script.Add(ScriptRenameTable(renameTable));
                    break;
                case ChangeTableOwner changeTableOwner:
                    script.Add(ScriptChangeTableOwner(changeTableOwner));
                    break;

                case CreateColumn addColumn:
                    script.Add(ScriptAddColumn(addColumn));
                    break;
                case AlterColumn alterColumn:
                    script.Add(ScriptAlterColumn(alterColumn));
                    break;
                case RenameColumn renameColumn:
                    script.Add(ScriptRenameColumn(renameColumn));
                    break;
                case DropColumn dropColumn:
                    script.Add(ScriptDropColumn(dropColumn));
                    break;

                case CreateIndex addIndex:
                    script.Add(ScriptCreateIndex(addIndex));
                    break;
                case AlterIndex alterIndex:
                    script.Add(ScriptAlterIndex(alterIndex));
                    break;
                case DropIndex dropIndex:
                    script.Add(ScriptDropIndex(dropIndex));
                    break;

                default:
                    throw new NotImplementedException(alteration.GetType().AssemblyQualifiedName);
            }
        }

        return script;
    }


    private static Sql ScriptCreateSchema(CreateSchema _) {
        return Raw("-- Cannot create schemas");
    }


    private static Sql ScriptCreateTable(CreateTable change) {
        var columns = change.Columns.Select(ScriptAddColumnDefinition);

        if (change.PrimaryKey.Any()) {
            columns = columns.Append(Interpolate($"CONSTRAINT {Identifier(change.SchemaName, "PK_" + change.TableName)} PRIMARY KEY({Join(", ", change.PrimaryKey.Select(Identifier))})"));
        }

        return Interpolate($"CREATE TABLE {Identifier(change.SchemaName, change.TableName)} ({Join(", ", columns)});");
    }

    private static Sql ScriptRenameTable(RenameTable change) {
        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} RENAME TO {Identifier(change.NewTableName)};");
    }

    private static Sql ScriptChangeTableOwner(ChangeTableOwner _) {
        return Raw("-- Cannot change table owner");
    }


    private static Sql ScriptAddColumn(CreateColumn change) {
        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} ADD COLUMN {ScriptAddColumnDefinition(change.Column)};");
    }

    private static Sql ScriptRenameColumn(RenameColumn change) {
        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} RENAME COLUMN {Identifier(change.ColumnName)} TO {Identifier(change.NewColumnName)};");
    }

    private static Sql ScriptAlterColumn(AlterColumn change) {
        var commands = new List<Sql>();

        if (change.Modifications.Contains(AlterColumnModification.Default)) {
            if (!string.IsNullOrEmpty(change.Column.DefaultValueSql)) {
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
            commands.Add(Interpolate($"ALTER COLUMN {Identifier(change.Column.Name)} TYPE {change.Column.StoreType}"));
        }
        if (change.Modifications.Contains(AlterColumnModification.Generated)) {
            if (!string.IsNullOrEmpty(change.Column.ComputedColumnSql)) {
                throw new ValidationException("Modifying a generated column is not currently supported because it requires dropping the existing column and then recreating it with the new expression. Please manually drop and recreate the generated column if needed.");
            }
            else {
                commands.Add(Interpolate($"ALTER COLUMN {Identifier(change.Column.Name)} DROP EXPRESSION"));
            }
        }

        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} {Join(", ", commands)};");
    }

    private static Sql ScriptDropColumn(DropColumn change) {
        return Interpolate($"ALTER TABLE {Identifier(change.SchemaName, change.TableName)} DROP COLUMN {Identifier(change.ColumnName)};");
    }

    private static Sql ScriptCreateIndex(CreateIndex change) {
        if (change.Index.IndexType == TableIndexType.PrimaryKey) {
            return Raw("-- Cannot create primary keys");
        }

        var index = change.Index;
        return Interpolate($"CREATE {Raw(index.IndexType == TableIndexType.UniqueConstraint ? "UNIQUE INDEX" : "INDEX")} {Identifier(index.GetName(change.TableName))} ON {Identifier(change.TableName)} ({Join(", ", index.Columns.Select(Identifier))});");
    }

    private static Sql ScriptAlterIndex(AlterIndex change) {
        if (change.Index.IndexType == TableIndexType.PrimaryKey) {
            return Raw("-- Cannot alter primary keys");
        }

        var index = change.Index;
        return Interpolate($"""
DROP INDEX IF EXISTS {Identifier(index.GetName(change.TableName))};
CREATE {Raw(index.IndexType == TableIndexType.UniqueConstraint ? "UNIQUE INDEX" : "INDEX")} {Identifier(index.GetName(change.TableName))} ON {Identifier(change.TableName)} ({Join(", ", index.Columns.Select(Identifier))});
""");
    }

    private static Sql ScriptDropIndex(DropIndex change) {
        if (change.Index.IndexType == TableIndexType.PrimaryKey) {
            return Raw("-- Cannot drop primary keys");
        }

        var index = change.Index;
        return Interpolate($"DROP INDEX IF EXISTS {Identifier(index.GetName(change.TableName))};");
    }
}

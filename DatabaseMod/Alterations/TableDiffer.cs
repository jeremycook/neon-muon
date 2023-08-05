using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;

namespace DatabaseMod.Alterations;

public static class TableDiffer {
    public static List<DatabaseAlteration> DiffTables(string schemaName, Table? current, Table goal) {
        var changes = new List<DatabaseAlteration>();

        string tableName = goal.Name;

        if (current is null) {
            var primaryKey =
                goal.Indexes.FirstOrDefault(o => o.IndexType == TableIndexType.PrimaryKey)?.Columns ??
                goal.Columns.Take(1).Select(o => o.Name).ToList();

            // Create the table
            changes.Add(new CreateTable(schemaName, tableName, goal.Columns.Cast<Column>().ToArray(), indexes: goal.Indexes.ToArray(), foreignKeys: goal.ForeignKeys.ToArray(), owner: goal.Owner));

            // Return early
            return changes;
        }

        // Table changes

        if (current.Name != goal.Name) {
            changes.Add(new RenameTable(schemaName, current.Name, goal.Name));
        }

        if (!string.IsNullOrEmpty(goal.Owner) && current.Owner != goal.Owner) {
            changes.Add(new ChangeTableOwner(schemaName, tableName, goal.Owner));
        }

        // Column changes

        var newColumns = goal.Columns
            .Where(targetColumn => !current.Columns.Any(column => column.Name == targetColumn.Name))
            .ToArray();
        changes.AddRange(newColumns.Select(newColumn => new CreateColumn(schemaName, tableName, (Column)newColumn)));

        var alteredColumns = goal.Columns.Except(newColumns)
            .Where(targetColumn => !targetColumn.Same(current.Columns.Single(column => column.Name == targetColumn.Name)));
        foreach (var alteredColumn in alteredColumns) {
            var currentColumn = current.Columns.Single(column => column.Name == alteredColumn.Name);

            var modifications = new List<AlterColumnModification>();
            if (alteredColumn.IsNullable != currentColumn.IsNullable) {
                modifications.Add(AlterColumnModification.Nullability);
            }
            if (alteredColumn.DefaultValueSql != currentColumn.DefaultValueSql) {
                modifications.Add(AlterColumnModification.Default);
            }
            if (alteredColumn.StoreType != currentColumn.StoreType) {
                modifications.Add(AlterColumnModification.Type);
            }
            if (alteredColumn.ComputedColumnSql != currentColumn.ComputedColumnSql) {
                modifications.Add(AlterColumnModification.Generated);
            }

            if (modifications.Any()) {
                changes.Add(new AlterColumn(schemaName, tableName, alteredColumn, modifications));
            }
        }

        var droppedColumns = current.Columns
            .Where(column => !goal.Columns.Any(targetColumn => targetColumn.Name == column.Name));
        changes.AddRange(droppedColumns.Select(c => new DropColumn(schemaName, tableName, columnName: c.Name)));

        // Index changes

        var newIndexes = goal.Indexes
            .Where(targetIndex => !current.Indexes.Any(index => index.Name == targetIndex.Name))
            .ToArray();
        changes.AddRange(newIndexes.Select(index => new CreateIndex(schemaName, tableName, index)));

        var alteredIndexes = goal.Indexes.Except(newIndexes)
            .Where(targetIndex => !targetIndex.Same(current.Indexes.Single(index => index.Name == targetIndex.Name)));
        changes.AddRange(alteredIndexes.Select(index => new AlterIndex(schemaName, tableName, index)));

        var droppedIndexes = current.Indexes
            .Where(index => !goal.Indexes.Any(targetIndex => targetIndex.Name == index.Name));
        changes.AddRange(droppedIndexes.Select(index => new DropIndex(schemaName, tableName, index)));

        return changes;
    }
}
using DatabaseMod.Alterations.Models;
using DatabaseMod.Models;

namespace DatabaseMod.Alterations;

public static class TableDiffer
{
    public static List<DatabaseAlteration> DiffTables(string schemaName, Table current, Table target)
    {
        string tableName = target.Name;

        var changes = new List<DatabaseAlteration>();

        // Table changes

        if (current.Name != target.Name)
        {
            changes.Add(new RenameTable(schemaName, current.Name, target.Name));
        }

        if (!string.IsNullOrEmpty(target.Owner) && current.Owner != target.Owner)
        {
            changes.Add(new ChangeTableOwner(schemaName, tableName, target.Owner));
        }

        // Column changes

        var newColumns = target.Columns
            .Where(targetColumn => !current.Columns.Any(column => column.Name == targetColumn.Name))
            .ToArray();
        changes.AddRange(newColumns.Select(newColumn => new CreateColumn(schemaName, tableName, newColumn)));

        var alteredColumns = target.Columns.Except(newColumns)
            .Where(targetColumn => !targetColumn.Same(current.Columns.Single(column => column.Name == targetColumn.Name)));
        foreach (var alteredColumn in alteredColumns)
        {
            var currentColumn = current.Columns.Single(column => column.Name == alteredColumn.Name);

            var modifications = new List<AlterColumnModification>();
            if (alteredColumn.IsNullable != currentColumn.IsNullable)
            {
                modifications.Add(AlterColumnModification.Nullability);
            }
            if (alteredColumn.DefaultValueSql != currentColumn.DefaultValueSql)
            {
                modifications.Add(AlterColumnModification.Default);
            }
            if (alteredColumn.StoreType != currentColumn.StoreType)
            {
                modifications.Add(AlterColumnModification.Type);
            }
            if (alteredColumn.ComputedColumnSql != currentColumn.ComputedColumnSql)
            {
                modifications.Add(AlterColumnModification.Generated);
            }

            changes.Add(new AlterColumn(schemaName, tableName, alteredColumn, modifications));
        }

        var droppedColumns = current.Columns
            .Where(column => !target.Columns.Any(targetColumn => targetColumn.Name == column.Name));
        changes.AddRange(droppedColumns.Select(c => new DropColumn(schemaName, tableName, columnName: c.Name)));

        // Index changes

        var newIndexes = target.Indexes
            .Where(targetIndex => !current.Indexes.Any(index => index.Name == targetIndex.Name))
            .ToArray();
        changes.AddRange(newIndexes.Select(index => new CreateIndex(schemaName, tableName, index)));

        var alteredIndexes = target.Indexes.Except(newIndexes)
            .Where(targetIndex => !targetIndex.Same(current.Indexes.Single(index => index.Name == targetIndex.Name)));
        changes.AddRange(alteredIndexes.Select(index => new AlterIndex(schemaName, tableName, index)));

        var droppedIndexes = current.Indexes
            .Where(index => !target.Indexes.Any(targetIndex => targetIndex.Name == index.Name));
        changes.AddRange(droppedIndexes.Select(index => new DropIndex(schemaName, tableName, index)));

        return changes;
    }
}
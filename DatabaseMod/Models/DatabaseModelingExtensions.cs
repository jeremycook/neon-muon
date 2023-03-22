namespace DatabaseMod.Models;

public static class DatabaseModelingExtensions
{
    public static Table GetOrAdd(this ICollection<Table> collection, Table newItem)
    {
        var item = collection.FirstOrDefault(o => o.Name == newItem.Name);

        if (item is null)
        {
            item = newItem;
            collection.Add(item);
        }

        return item;
    }

    public static Column GetOrAdd(this ICollection<Column> collection, Column newItem)
    {
        var item = collection.FirstOrDefault(o => o.Name == newItem.Name);

        if (item is null)
        {
            item = newItem;
            collection.Add(item);
        }

        return item;
    }

    public static TableIndex GetOrAdd(this ICollection<TableIndex> collection, TableIndex newItem)
    {
        var item = collection.FirstOrDefault(o => o.Name == newItem.Name);

        if (item is null)
        {
            item = newItem;
            collection.Add(item);
        }

        return item;
    }
}

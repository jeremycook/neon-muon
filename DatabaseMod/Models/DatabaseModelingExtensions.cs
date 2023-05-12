using DataCore.Annotations;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace DatabaseMod.Models;

public static class DatabaseModelingExtensions {
    public static void ContributeQueryableContext<TDb>(this Database<TDb> database) {
        database.ContributeQueryableContext(typeof(TDb));
    }

    public static void ContributeQueryableContext(this Database database, Type queryContextType) {
        var defaultSchema = string.Empty;

        var tableTypes = queryContextType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IQueryable<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToList();

        foreach (var schemaGroup in tableTypes.GroupBy(t => t.GetCustomAttribute<TableAttribute>()?.Schema ?? defaultSchema)) {
            if (database.Schemas.FirstOrDefault(o => o.Name == schemaGroup.Key) is not Schema schema) {
                schema = new(schemaGroup.Key);
                database.Schemas.Add(schema);
            }

            foreach (var tableType in schemaGroup) {
                var propertyMap = tableType.GetProperties()
                    .OrderBy(p => p.GetCustomAttribute<ColumnAttribute>()?.Order ?? 0)
                    .ToImmutableSortedDictionary(p => p.Name, p => p);
                var properties = propertyMap.Values;

                var table = schema.Tables.GetOrAdd(new Table(tableType.GetCustomAttribute<TableAttribute>()?.Name ?? tableType.Name));

                // Primary key
                var keyProperties = tableType.GetCustomAttribute<PrimaryKeyAttribute>()?.Columns
                    .Select(c => propertyMap[c])
                    .ToArray().AsReadOnly();
                keyProperties ??= properties
                    .Where(o => o.GetCustomAttribute<KeyAttribute>() is not null)
                    .ToArray().AsReadOnly();
                if (!keyProperties.Any()) {
                    if (propertyMap.TryGetValue("Id", out var idProp)) {
                        keyProperties = new[] { idProp }.AsReadOnly();
                    }
                    else if (propertyMap.TryGetValue(tableType.Name + "Id", out var classNameIdProp)) {
                        keyProperties = new[] { classNameIdProp }.AsReadOnly();
                    }
                    else {
                        throw new NotSupportedException($"The {tableType} does not have a primary key.");
                    }
                }

                foreach (var (prop, i) in properties.Select((prop, i) => (prop, i))) {
                    var column = table.Columns.GetOrAdd(new Column(
                        name: prop.Name,
                        storeType: StoreTypeHelpers.ConvertClrTypeToStoreType(prop.PropertyType),
                        isNullable: !keyProperties.Contains(prop),
                        defaultValueSql: null,
                        computedColumnSql: null) {
                        Position = i + 1,
                    });
                }

                var index = table.Indexes.GetOrAdd(new TableIndex("pk_" + table.Name, TableIndexType.PrimaryKey, keyProperties.Select(c => c.Name)));
            }
        }
    }

    public static Table GetOrAdd(this ICollection<Table> collection, Table newItem) {
        var item = collection.FirstOrDefault(o => o.Name == newItem.Name);

        if (item is null) {
            item = newItem;
            collection.Add(item);
        }

        return item;
    }

    public static Column GetOrAdd(this ICollection<Column> collection, Column newItem) {
        var item = collection.FirstOrDefault(o => o.Name == newItem.Name);

        if (item is null) {
            item = newItem;
            collection.Add(item);
        }

        return item;
    }

    public static TableIndex GetOrAdd(this ICollection<TableIndex> collection, TableIndex newItem) {
        var item = collection.FirstOrDefault(o => o.Name == newItem.Name);

        if (item is null) {
            item = newItem;
            collection.Add(item);
        }

        return item;
    }
}

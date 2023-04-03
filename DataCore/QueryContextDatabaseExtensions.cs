using DatabaseMod.Models;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace DataCore;

public static class QueryContextDatabaseExtensions {
    public static void ContributeQueryContext<TDb>(this Database<TDb> database) {
        database.ContributeQueryContext(typeof(TDb));
    }

    public static void ContributeQueryContext(this Database database, Type queryContextType) {
        var defaultSchema = string.Empty;

        var tableTypes = queryContextType.GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IQuery<,>))
            .Select(p => p.PropertyType.GetGenericArguments()[1])
            .ToList();

        foreach (var schemaGroup in tableTypes.GroupBy(t => t.GetCustomAttribute<TableAttribute>()?.Schema ?? defaultSchema)) {
            if (database.Schemas.FirstOrDefault(o => o.Name == schemaGroup.Key) is not Schema schema) {
                schema = new(schemaGroup.Key);
                database.Schemas.Add(schema);
            }

            foreach (var tableType in schemaGroup) {
                var properties = tableType.GetProperties()
                    .OrderBy(p => p.GetCustomAttribute<ColumnAttribute>()?.Order ?? 0)
                    .ToImmutableArray();

                var table = schema.Tables.GetOrAdd(new Table(tableType.GetCustomAttribute<TableAttribute>()?.Name ?? tableType.Name));

                var keyProperties = properties
                    .Where(o => o.GetCustomAttribute<KeyAttribute>() is not null)
                    .ToList();

                if (!keyProperties.Any()) {
                    var matches = properties.Where(o => o.Name == "Id" || o.Name == tableType.Name + "Id");
                    if (matches.Count() == 1) {
                        keyProperties.Add(matches.ElementAt(0));
                    }
                    // TODO: Throw?
                }

                foreach (var (prop, i) in properties.Select((prop, i) => (prop, i))) {
                    var column = table.Columns.GetOrAdd(new Column(
                        name: prop.Name,
                        storeType: StoreTypeHelpers.ConvertClrTypeToStoreType(prop.PropertyType),
                        isNullable: keyProperties.Contains(prop) ? false : true,
                        defaultValueSql: null,
                        computedColumnSql: null) {
                        Position = i + 1,
                    });
                }

                var index = table.Indexes.GetOrAdd(new TableIndex("PK_" + table.Name, TableIndexType.PrimaryKey) {
                    Columns = keyProperties.Select(c => c.Name).ToList(),
                });
            }
        }
    }
}

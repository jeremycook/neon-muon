namespace DatabaseMod.Models;

public interface IReadOnlyDatabase {
    IReadOnlyList<IReadOnlySchema> Schemas { get; }
    IReadOnlyTable GetTable(string tableSchema, string tableName);
}

public interface IReadOnlyDatabase<TDb> : IReadOnlyDatabase { }

public class Database : IReadOnlyDatabase {
    public List<Schema> Schemas { get; } = new();

    IReadOnlyList<IReadOnlySchema> IReadOnlyDatabase.Schemas => Schemas;

    public Table GetTable(string tableSchema, string tableName) {
        return Schemas.Single(schema => schema.Name == tableSchema)
            .Tables.Single(t => t.Name == tableName);
    }

    IReadOnlyTable IReadOnlyDatabase.GetTable(string tableSchema, string tableName) {
        return GetTable(tableSchema, tableName);
    }
}

public class Database<TDb> : Database, IReadOnlyDatabase<TDb> { }

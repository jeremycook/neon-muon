namespace DatabaseMod.Models;

public interface IReadOnlyDatabase {
    IReadOnlyList<IReadOnlySchema> Schemas { get; }
}

public interface IReadOnlyDatabase<TDb> : IReadOnlyDatabase { }

public class Database : IReadOnlyDatabase {
    public List<Schema> Schemas { get; } = new();

    IReadOnlyList<IReadOnlySchema> IReadOnlyDatabase.Schemas => Schemas;
}

public class Database<TDb> : Database, IReadOnlyDatabase<TDb> { }

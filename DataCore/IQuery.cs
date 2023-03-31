namespace DataCore;

public interface IQuery<TDb> {
    QueryType Type { get; }
}

public interface IQuery<TDb, T1> : IQuery<TDb> {
}

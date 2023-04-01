namespace DataCore;

public interface IQuery<TDb> {
    QueryType QueryType { get; }
}

public interface IQuery<TDb, T1> : IQuery<TDb> {
}

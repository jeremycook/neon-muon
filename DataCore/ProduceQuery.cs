namespace DataCore;

public struct ProduceQuery<TDb, T1> : IQuery<TDb, T1> {
    public ProduceQuery(IQuery<TDb, T1> query) {
        Query = query;
    }

    public IQuery<TDb, T1> Query { get; }
}
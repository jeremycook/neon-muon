namespace DataCore;

public static class ProduceQuery {
    public static ProduceQuery<TDb, T1> Create<TDb, T1>(IQuery<TDb, T1> query) => new(query);
}

public readonly struct ProduceQuery<TDb, T1> : IQuery<TDb, T1> {
    public ProduceQuery(IQuery<TDb, T1> query) {
        Query = query;
    }

    public QueryType QueryType => QueryType.Produce;
    public IQuery<TDb, T1> Query { get; }
}
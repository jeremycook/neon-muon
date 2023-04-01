namespace DataCore;

public static class InsertQuery {
    public static InsertQuery<TDb, T1> Create<TDb, T1>(IQuery<TDb, T1> query, IReadOnlyList<T1> items) => new(query, items);
    public static InsertQuery<TDb, T1> Create<TDb, T1>(IQuery<TDb, T1> query, T1 item) => new(query, item);
}

public readonly struct InsertQuery<TDb, T1> : IQuery<TDb, T1> {
    public InsertQuery(IQuery<TDb, T1> query, IEnumerable<T1> items) {
        Query = query;
        Items = items.ToList();
    }
    public InsertQuery(IQuery<TDb, T1> query, T1 item)
        : this(query, new[] { item }) { }

    public QueryType QueryType => QueryType.Insert;
    public IQuery<TDb, T1> Query { get; }
    public IReadOnlyList<T1> Items { get; }
}
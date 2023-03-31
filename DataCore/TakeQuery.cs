namespace DataCore;

public readonly struct TakeQuery<TDb, T1> : IQuery<TDb, T1> {
    public TakeQuery(IQuery<TDb, T1> query, int take) {
        Query = query;
        Take = take;
    }

    public QueryType Type => QueryType.Take;
    public IQuery<TDb, T1> Query { get; }
    public int Take { get; }
}
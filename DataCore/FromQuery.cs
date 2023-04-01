namespace DataCore;

public readonly struct FromQuery<TDb, T1> : IQuery<TDb, T1> {
    public FromQuery() {
        T1Type = typeof(T1);
    }

    public QueryType QueryType => QueryType.From;

    public Type T1Type { get; }
}

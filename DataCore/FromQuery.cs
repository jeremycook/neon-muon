namespace DataCore;

public readonly struct FromQuery<TDb, T> : IQuery<TDb, T> {
    public FromQuery() {
        Type = typeof(T);
    }

    public Type Type { get; }
}

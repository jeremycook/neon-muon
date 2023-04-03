namespace DataCore;

public interface IQueryRunner<TDb> {

    int Execute(IQuery<TDb> query);
    ValueTask<int> Execute(IQuery<TDb> query, CancellationToken cancellationToken = default);
    ValueTask<List<T1>> List<T1>(IQuery<TDb, T1> query, CancellationToken cancellationToken = default);
    ValueTask<T1?> Nullable<T1>(IQuery<TDb, T1> query, CancellationToken cancellationToken = default)
        where T1 : struct;
}

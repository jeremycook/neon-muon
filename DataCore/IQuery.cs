using System.Linq.Expressions;

namespace DataCore;

public interface IQuery<TDb, T1>
{
    IQuery<TDb, (T1, T2)> Join<T2>(Expression<Func<TDb, IQuery<TDb, T2>>> query, Expression<Func<T1, T2, bool>> predicate);

    IQuery<TDb, T1> Filter(Expression<Func<T1, bool>> predicate);

    IQuery<TDb, T1> Asc<TProperty>(Expression<Func<T1, TProperty>> sort)
        where TProperty : IComparable<TProperty>;
    IQuery<TDb, T1> Desc<TProperty>(Expression<Func<T1, TProperty>> sort)
        where TProperty : IComparable<TProperty>;

    IQuery<TDb, T1> ThenAsc<TProperty>(Expression<Func<T1, TProperty>> sort)
        where TProperty : IComparable<TProperty>;
    IQuery<TDb, T1> ThenDesc<TProperty>(Expression<Func<T1, TProperty>> sort)
        where TProperty : IComparable<TProperty>;

    IQuery<TDb, TProjected> Map<TProjected>(Expression<Func<T1, TProjected>> projection);

    Task<T1> ToItemAsync(CancellationToken cancellationToken = default);
    Task<T1?> ToOptionalItemAsync(CancellationToken cancellationToken = default);
    ValueTask<List<T1>> ToListAsync(CancellationToken cancellationToken = default);


    IQuery<TDb, T1> Insert(T1 item);
    IQuery<TDb, T1> Update(Expression<Func<T1, T1>> update);

    ValueTask<int> ExecuteAsync(CancellationToken cancellationToken = default);
    ValueTask ExecuteAsync(int modifications, CancellationToken cancellationToken = default);
}

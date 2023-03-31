using System.Linq.Expressions;

namespace DataCore;

public readonly struct JoinQuery<TDb, T1, T2> : IQuery<TDb, (T1, T2)>
{
    public JoinQuery(IQuery<TDb, T1> query1, IQuery<TDb, T2> query2, Expression<Func<T1, T2, bool>> condition)
    {
        Query1 = query1;
        Query2 = query2;
        Condition = condition;
    }

    public IQuery<TDb, T1> Query1 { get; }
    public IQuery<TDb, T2> Query2 { get; }
    public Expression<Func<T1, T2, bool>> Condition { get; }
}
using System.Linq.Expressions;

namespace DataCore;

public readonly struct JoinQuery<TDb, T1, T2> : IQuery<TDb, (T1, T2)> {
    public JoinQuery(FromQuery<TDb, T1> leftQuery, FromQuery<TDb, T2> query2, Expression<Func<T1, T2, bool>> condition) {
        LeftQuery = leftQuery;
        RightQuery = query2;
        Condition = condition;
    }

    public QueryType QueryType => QueryType.Join;
    public FromQuery<TDb, T1> LeftQuery { get; }
    public FromQuery<TDb, T2> RightQuery { get; }
    public Expression<Func<T1, T2, bool>> Condition { get; }
}

public readonly struct JoinQuery<TDb, T1, T2, T3> : IQuery<TDb, (T1, T2, T3)> {
    public JoinQuery(JoinQuery<TDb, T1, T2> leftQuery, FromQuery<TDb, T3> rightQuery, Expression<Func<T1, T2, T3, bool>> condition) {
        LeftQuery = leftQuery;
        RightQuery = rightQuery;
        Condition = condition;
    }

    public QueryType QueryType => QueryType.Join;
    public JoinQuery<TDb, T1, T2> LeftQuery { get; }
    public FromQuery<TDb, T3> RightQuery { get; }
    public Expression<Func<T1, T2, T3, bool>> Condition { get; }
}

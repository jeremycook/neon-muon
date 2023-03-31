namespace DataCore;

public interface IQueryContext<TDb>
{
    IQuery<TDb, T> From<T>();
}
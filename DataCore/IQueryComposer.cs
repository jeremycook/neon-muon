namespace DataCore;

public interface IQueryComposer<TDb> {
    IReadOnlyCollection<IQueryCommand<object>> Compose(IQuery<TDb> query);
}

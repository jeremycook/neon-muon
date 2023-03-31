namespace DataCore;

public interface IQueryOrchestrator<TDb> {
    IReadOnlyCollection<IQueryCommand<object>> Compose<T1>(IQuery<TDb> query);
}

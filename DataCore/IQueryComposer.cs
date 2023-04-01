namespace DataCore;

public interface IQueryComposer<TDb> {
    IReadOnlyCollection<IQueryCommand> Compose(IQuery<TDb> query);
}

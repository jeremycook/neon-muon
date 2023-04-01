namespace DataCore;

public interface IQueryCommand {
    ValueTask ExecuteAsync(CancellationToken cancellationToken = default);
    object? Response { get; }
}
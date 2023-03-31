namespace DataCore;

public interface IQueryCommand<out TResponse> {
    ValueTask ExecuteAsync(CancellationToken cancellationToken = default);
    TResponse? Response { get; }
}
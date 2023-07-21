using System.Collections.Concurrent;

namespace WebHooks;

public class JobQueue {
    private readonly ConcurrentQueue<Func<IServiceScopeFactory, CancellationToken, Task>> jobs = new();
    private readonly SemaphoreSlim signal = new(0);

    public void EnqueueTask(Func<IServiceScopeFactory, CancellationToken, Task> job) {
        if (job == null) {
            throw new ArgumentNullException(nameof(job));
        }

        jobs.Enqueue(job);
        signal.Release();
    }

    public async Task<Func<IServiceScopeFactory, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken) {

        // Wait for task to become available
        await signal.WaitAsync(cancellationToken);

        jobs.TryDequeue(out var task);
        return task!;
    }
}

public class JobQueueHostedService : BackgroundService {
    private readonly JobQueue taskQueue;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<JobQueueHostedService> logger;

    public JobQueueHostedService(JobQueue taskQueue, IServiceScopeFactory serviceScopeFactory, ILogger<JobQueueHostedService> logger) {
        this.taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        this.serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {

        // Dequeue and execute tasks until the application is stopped
        while (!cancellationToken.IsCancellationRequested) {

            // Get next task
            // This waits until a task becomes available
            var task = await taskQueue.DequeueAsync(cancellationToken);

            try {
                await task(serviceScopeFactory, cancellationToken);
            }
            catch (OperationCanceledException) {
                // Ignore
            }
            catch (Exception ex) {
                logger.LogError(ex, "An error occurred during execution of a background task");
            }
        }
    }
}

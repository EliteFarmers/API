namespace EliteAPI.Services.Background; 

public interface IBackgroundTaskQueue
{
    ValueTask EnqueueAsync(Func<IServiceScope, CancellationToken, ValueTask> workItem);

    ValueTask<Func<IServiceScope, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
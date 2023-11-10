using System.Threading.Channels;

namespace EliteAPI.Services.Background; 

public class BackgroundTaskQueue : IBackgroundTaskQueue {
    private const int DefaultQueueCapacity = 100;
    private readonly Channel<Func<IServiceScope, CancellationToken, ValueTask>> _queue;

    public BackgroundTaskQueue() : this(DefaultQueueCapacity) { }
    public BackgroundTaskQueue(int capacity) {
        var options = new BoundedChannelOptions(capacity) {
            FullMode = BoundedChannelFullMode.Wait
        };
        
        _queue = Channel.CreateBounded<Func<IServiceScope, CancellationToken, ValueTask>>(options);
    }
    
    public async ValueTask EnqueueAsync(Func<ValueTask> workItem) {
        await EnqueueAsync((_, _) => workItem());
    }
    
    public async ValueTask EnqueueAsync(Func<CancellationToken, ValueTask> workItem) {
        await EnqueueAsync((_, ct) => workItem(ct));
    }
    
    public async ValueTask EnqueueAsync(Func<IServiceScope, CancellationToken, ValueTask> workItem) {
        if (workItem is null) {
            throw new ArgumentNullException(nameof(workItem));
        }
        
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<IServiceScope, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
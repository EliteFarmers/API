namespace EliteAPI.Services.Background; 

public class BackgroundQueueWorker(
    IBackgroundTaskQueue queue, 
    IServiceProvider provider, 
    ILogger<BackgroundQueueWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await Task.Yield();
        
        logger.LogInformation("Background Task Queue Running");
        
        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await queue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = provider.CreateScope();
                await workItem(scope, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing {WorkItem}", nameof(workItem));
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Queued Processor is stopping");

        await base.StopAsync(stoppingToken);
    }
}
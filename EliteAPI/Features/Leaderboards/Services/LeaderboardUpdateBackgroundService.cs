using System.Diagnostics;
using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Services;

public class LeaderboardUpdateBackgroundService : BackgroundService {
	private readonly ILeaderboardUpdateQueue _queue;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<LeaderboardUpdateBackgroundService> _logger;
	private readonly ILeaderboardCacheMetrics _metrics;
	private readonly TimeSpan _batchInterval;
	private readonly int _maxBatchSize;
	private readonly bool _enabled;

	public LeaderboardUpdateBackgroundService(
		ILeaderboardUpdateQueue queue,
		IServiceScopeFactory scopeFactory,
		ILogger<LeaderboardUpdateBackgroundService> logger,
		ILeaderboardCacheMetrics metrics,
		IConfiguration configuration) {
		_queue = queue;
		_scopeFactory = scopeFactory;
		_logger = logger;
		_metrics = metrics;
		
		var intervalSeconds = configuration.GetValue("Leaderboards:BatchIntervalSeconds", 5);
		_batchInterval = TimeSpan.FromSeconds(intervalSeconds);
		_maxBatchSize = configuration.GetValue("Leaderboards:MaxBatchSize", 1000);
		_enabled = configuration.GetValue("Leaderboards:EnableAsyncUpdates", true);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		if (!_enabled) {
			_logger.LogInformation("Leaderboard async updates are disabled, background service will not run");
			return;
		}
		
		_logger.LogInformation(
			"Leaderboard update background service started with interval={Interval}s, maxBatchSize={MaxBatchSize}",
			_batchInterval.TotalSeconds, _maxBatchSize);

		using var timer = new PeriodicTimer(_batchInterval);
		
		while (!stoppingToken.IsCancellationRequested) {
			try {
				await timer.WaitForNextTickAsync(stoppingToken);
				await ProcessBatchAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
				// Graceful shutdown
				break;
			}
			catch (Exception ex) {
				_logger.LogError(ex, "Error in leaderboard update background service");
			}
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken) {
		_logger.LogInformation("Leaderboard update background service stopping, processing remaining items...");
		
		// Process any remaining items before shutdown
		try {
			await ProcessBatchAsync(CancellationToken.None);
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Error processing remaining leaderboard updates during shutdown");
		}
		
		await base.StopAsync(cancellationToken);
	}

	private async Task ProcessBatchAsync(CancellationToken ct) {
		var pendingCount = _queue.PendingCount;
		_metrics.RecordQueueDepth(pendingCount);
		
		if (pendingCount == 0) return;
		
		var stopwatch = Stopwatch.StartNew();
		
		try {
			// Drain all currently available items (snapshot of what's in the queue right now)
			var updates = DrainQueueSync();
			
			if (updates.Count == 0) return;
			
			_logger.LogDebug("Processing batch of {Count} leaderboard updates", updates.Count);
			
			// Process in chunks if batch is too large
			for (var i = 0; i < updates.Count; i += _maxBatchSize) {
				if (ct.IsCancellationRequested) break;
				
				var chunk = updates.Skip(i).Take(_maxBatchSize).ToList();
				await ProcessChunkAsync(chunk, ct);
			}
			
			stopwatch.Stop();
			_metrics.RecordBatchProcessed(updates.Count, stopwatch.Elapsed.TotalMilliseconds);
			
			_logger.LogInformation(
				"Processed {Count} leaderboard updates in {Duration}ms",
				updates.Count, stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested) {
			throw;
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Error processing leaderboard update batch");
		}
	}

	/// <summary>
	/// Drain all currently available items from the queue synchronously.
	/// </summary>
	private List<LeaderboardUpdateEntry> DrainQueueSync() {
		var items = new List<LeaderboardUpdateEntry>();
		
		while (_queue.TryDequeue(out var item) && item is not null) {
			items.Add(item);
		}
		
		return items;
	}

	private async Task ProcessChunkAsync(List<LeaderboardUpdateEntry> updates, CancellationToken ct) {
		await using var scope = _scopeFactory.CreateAsyncScope();
		var lbService = scope.ServiceProvider.GetRequiredService<ILbService>();
		
		await lbService.ProcessLeaderboardUpdatesAsync(updates, ct);
	}
}

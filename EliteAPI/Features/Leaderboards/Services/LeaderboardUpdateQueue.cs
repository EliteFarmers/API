using System.Threading.Channels;
using EliteAPI.Features.Leaderboards.Models;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILeaderboardUpdateQueue {
	/// <summary>
	/// Enqueue a single update. Returns false if the queue is full.
	/// </summary>
	bool Enqueue(LeaderboardUpdateEntry update);
	
	/// <summary>
	/// Enqueue multiple updates. Returns the number of updates successfully enqueued.
	/// </summary>
	int EnqueueBatch(IEnumerable<LeaderboardUpdateEntry> updates);
	
	/// <summary>
	/// Try to dequeue a single item without blocking. Returns false if the queue is empty.
	/// </summary>
	bool TryDequeue(out LeaderboardUpdateEntry? item);
	
	/// <summary>
	/// Drain all currently available items from the queue.
	/// </summary>
	Task<List<LeaderboardUpdateEntry>> DequeueAllAsync(CancellationToken ct);
	
	/// <summary>
	/// Get the current number of pending updates in the queue.
	/// </summary>
	int PendingCount { get; }
}

[RegisterService<ILeaderboardUpdateQueue>(LifeTime.Singleton)]
public class LeaderboardUpdateQueue : ILeaderboardUpdateQueue {
	private readonly Channel<LeaderboardUpdateEntry> _channel;
	private readonly ILogger<LeaderboardUpdateQueue> _logger;
	private readonly ILeaderboardCacheMetrics _metrics;
	
	public LeaderboardUpdateQueue(
		ILogger<LeaderboardUpdateQueue> logger,
		ILeaderboardCacheMetrics metrics,
		IConfiguration configuration) {
		_logger = logger;
		_metrics = metrics;
		
		var capacity = configuration.GetValue("Leaderboards:QueueCapacity", 10_000);
		
		_channel = Channel.CreateBounded<LeaderboardUpdateEntry>(new BoundedChannelOptions(capacity) {
			FullMode = BoundedChannelFullMode.DropWrite,
			SingleReader = true,
			SingleWriter = false
		});
	}
	
	public bool Enqueue(LeaderboardUpdateEntry update) {
		if (_channel.Writer.TryWrite(update)) {
			return true;
		}
		
		_metrics.RecordUpdateDropped();
		_logger.LogWarning("Leaderboard update queue is full, dropping update for LeaderboardId={LeaderboardId}", 
			update.LeaderboardId);
		return false;
	}
	
	public int EnqueueBatch(IEnumerable<LeaderboardUpdateEntry> updates) {
		var count = 0;
		foreach (var update in updates) {
			if (Enqueue(update)) {
				count++;
			}
		}
		return count;
	}
	
	public bool TryDequeue(out LeaderboardUpdateEntry? item) {
		return _channel.Reader.TryRead(out item);
	}
	
	public async Task<List<LeaderboardUpdateEntry>> DequeueAllAsync(CancellationToken ct) {
		var items = new List<LeaderboardUpdateEntry>();
		
		while (await _channel.Reader.WaitToReadAsync(ct)) {
			// Drain all available items
			while (_channel.Reader.TryRead(out var item)) {
				items.Add(item);
			}
			
			break;
		}
		
		return items;
	}
	
	public int PendingCount => _channel.Reader.Count;
}

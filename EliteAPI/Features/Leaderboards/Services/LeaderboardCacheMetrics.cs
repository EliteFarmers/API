using System.Diagnostics.Metrics;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILeaderboardCacheMetrics
{
	void RecordBatchProcessed(int count, double durationMs);
	void RecordQueueDepth(int depth);
	void RecordUpdateDropped();
}

public class LeaderboardCacheMetrics : ILeaderboardCacheMetrics
{
	private readonly Counter<long> _batchesProcessed;
	private readonly Histogram<double> _batchDuration;
	private readonly Counter<long> _entriesProcessed;
	private readonly Counter<long> _updatesDropped;
	private int _currentQueueDepth;

	public LeaderboardCacheMetrics(IMeterFactory meterFactory) {
		var meter = meterFactory.Create("eliteapi.leaderboard");

		_batchesProcessed = meter.CreateCounter<long>(
			"eliteapi.leaderboard.batches_processed",
			description: "Number of leaderboard update batches processed");
		
		_batchDuration = meter.CreateHistogram<double>(
			"eliteapi.leaderboard.batch_duration_ms",
			unit: "ms",
			description: "Duration of leaderboard batch processing in milliseconds");
		
		_entriesProcessed = meter.CreateCounter<long>(
			"eliteapi.leaderboard.entries_processed",
			description: "Number of leaderboard entries processed in batches");
		
		_updatesDropped = meter.CreateCounter<long>(
			"eliteapi.leaderboard.updates_dropped",
			description: "Number of leaderboard updates dropped due to full queue");
		
		meter.CreateObservableGauge(
			"eliteapi.leaderboard.queue_depth",
			() => _currentQueueDepth,
			description: "Current number of pending leaderboard updates in the queue");
	}

	public void RecordBatchProcessed(int count, double durationMs) {
		_batchesProcessed.Add(1);
		_batchDuration.Record(durationMs);
		_entriesProcessed.Add(count);
	}

	public void RecordQueueDepth(int depth) {
		_currentQueueDepth = depth;
	}

	public void RecordUpdateDropped() {
		_updatesDropped.Add(1);
	}
}

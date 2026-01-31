using System.Diagnostics.Metrics;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILeaderboardCacheMetrics
{
	void RecordAnchorCacheHit(string leaderboardId, int rankTier);
	void RecordAnchorCacheMiss(string leaderboardId, int rankTier);
	void RecordUpcomingCacheHit(string leaderboardId, int rankTier);
	void RecordUpcomingCacheMiss(string leaderboardId, int rankTier);
	void RecordBatchProcessed(int count, double durationMs);
	void RecordQueueDepth(int depth);
	void RecordUpdateDropped();
}

public class LeaderboardCacheMetrics : ILeaderboardCacheMetrics
{
	private readonly Counter<long> _anchorCacheHits;
	private readonly Counter<long> _anchorCacheMisses;
	private readonly Counter<long> _upcomingCacheHits;
	private readonly Counter<long> _upcomingCacheMisses;
	private readonly Counter<long> _batchesProcessed;
	private readonly Histogram<double> _batchDuration;
	private readonly Counter<long> _entriesProcessed;
	private readonly Counter<long> _updatesDropped;
	private int _currentQueueDepth;

	public LeaderboardCacheMetrics(IMeterFactory meterFactory) {
		var meter = meterFactory.Create("eliteapi.leaderboard");
		
		_anchorCacheHits = meter.CreateCounter<long>(
			"eliteapi.leaderboard.anchor_cache_hits",
			description: "Number of anchor cache hits for leaderboard rank lookups");
		
		_anchorCacheMisses = meter.CreateCounter<long>(
			"eliteapi.leaderboard.anchor_cache_misses", 
			description: "Number of anchor cache misses for leaderboard rank lookups");
		
		_upcomingCacheHits = meter.CreateCounter<long>(
			"eliteapi.leaderboard.upcoming_cache_hits",
			description: "Number of upcoming players cache hits");
		
		_upcomingCacheMisses = meter.CreateCounter<long>(
			"eliteapi.leaderboard.upcoming_cache_misses",
			description: "Number of upcoming players cache misses");
		
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

	public void RecordAnchorCacheHit(string leaderboardId, int rankTier) {
		_anchorCacheHits.Add(1, 
			new KeyValuePair<string, object?>("leaderboard", leaderboardId),
			new KeyValuePair<string, object?>("rank_tier", GetRankTierLabel(rankTier)));
	}

	public void RecordAnchorCacheMiss(string leaderboardId, int rankTier) {
		_anchorCacheMisses.Add(1,
			new KeyValuePair<string, object?>("leaderboard", leaderboardId),
			new KeyValuePair<string, object?>("rank_tier", GetRankTierLabel(rankTier)));
	}

	public void RecordUpcomingCacheHit(string leaderboardId, int rankTier) {
		_upcomingCacheHits.Add(1,
			new KeyValuePair<string, object?>("leaderboard", leaderboardId),
			new KeyValuePair<string, object?>("rank_tier", GetRankTierLabel(rankTier)));
	}

	public void RecordUpcomingCacheMiss(string leaderboardId, int rankTier) {
		_upcomingCacheMisses.Add(1,
			new KeyValuePair<string, object?>("leaderboard", leaderboardId),
			new KeyValuePair<string, object?>("rank_tier", GetRankTierLabel(rankTier)));
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

	private static string GetRankTierLabel(int rank) {
		return rank switch {
			<= 1000 => "top_1k",
			<= 5000 => "top_5k",
			<= 25000 => "top_25k",
			<= 50000 => "top_50k",
			<= 100000 => "top_100k",
			_ => "100k_plus"
		};
	}
}

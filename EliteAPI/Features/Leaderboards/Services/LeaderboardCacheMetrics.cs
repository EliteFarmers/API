using System.Diagnostics.Metrics;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILeaderboardCacheMetrics
{
	void RecordAnchorCacheHit(string leaderboardId, int rankTier);
	void RecordAnchorCacheMiss(string leaderboardId, int rankTier);
	void RecordUpcomingCacheHit(string leaderboardId, int rankTier);
	void RecordUpcomingCacheMiss(string leaderboardId, int rankTier);
}

public class LeaderboardCacheMetrics : ILeaderboardCacheMetrics
{
	private readonly Counter<long> _anchorCacheHits;
	private readonly Counter<long> _anchorCacheMisses;
	private readonly Counter<long> _upcomingCacheHits;
	private readonly Counter<long> _upcomingCacheMisses;

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

	private static string GetRankTierLabel(int rank) {
		return rank switch {
			<= 1000 => "top_1k",
			<= 5000 => "top_5k",
			<= 25000 => "top_25k",
			<= 50000 => "top_50k",
			_ => "50k_plus"
		};
	}
}

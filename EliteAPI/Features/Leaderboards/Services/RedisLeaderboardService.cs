using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Leaderboard = EliteAPI.Features.Leaderboards.Models.Leaderboard;
using Order = StackExchange.Redis.Order;

namespace EliteAPI.Features.Leaderboards.Services;

[RegisterService<IRedisLeaderboardService>(LifeTime.Scoped)]
public class RedisLeaderboardService(
	IConnectionMultiplexer redis,
	ILeaderboardRegistrationService registrationService,
	HybridCache cache,
	DataContext context,
	IOptions<ConfigLeaderboardSettings>? lbSettings = null
) : IRedisLeaderboardService
{
	private static readonly RedisValue ProfileHash = "p";
	private static readonly RedisValue IgnHash = "i";
	private static readonly RedisValue UuidHash = "u";

	// Fallback if not provided (e.g. testing)
	private readonly ConfigLeaderboardSettings _settings = lbSettings?.Value ?? new ConfigLeaderboardSettings();

	public async Task<Dictionary<string, LeaderboardPositionDto?>> GetMultipleLeaderboardRanks(
		List<string> leaderboards, LeaderboardRankRequestWithoutId request) {
		var ct = request.CancellationToken ?? CancellationToken.None;

		var lbEntities = await context.Leaderboards
			.AsNoTracking()
			.Where(l => leaderboards.Contains(l.Slug))
			.ToDictionaryAsync(l => l.Slug, ct);
		
		string? memberId = null;
		var needsMemberId = leaderboards.Any(id =>
			registrationService.LeaderboardsById.TryGetValue(id, out var def) && def.IsMemberLeaderboard());

		if (needsMemberId && !string.IsNullOrEmpty(request.ProfileId) && !string.IsNullOrEmpty(request.PlayerUuid)) {
			memberId = await ResolveMemberId(request.ProfileId, request.PlayerUuid, ct);
		}

		// 3. Execute all requests concurrently
		var tasks = leaderboards.Select(async lbId => {
			if (!lbEntities.TryGetValue(lbId, out var lb) ||
			    !registrationService.LeaderboardsById.TryGetValue(lbId, out var definition)) {
				return (lbId, Result: new LeaderboardPositionDto { Rank = -1 });
			}

			if (definition.IsMemberLeaderboard() && string.IsNullOrEmpty(memberId)) {
				return (lbId, Result: new LeaderboardPositionDto { Rank = -1 });
			}

			var subRequest = new LeaderboardRankRequest {
				LeaderboardId = lbId,
				PlayerUuid = request.PlayerUuid,
				ProfileId = request.ProfileId,
				ResourceId = request.ResourceId,
				Upcoming = request.Upcoming,
				Previous = request.Previous,
				AtRank = request.AtRank,
				AtAmount = request.AtAmount,
				GameMode = request.GameMode,
				RemovedFilter = request.RemovedFilter,
				Identifier = request.Identifier,
				SkipUpdate = request.SkipUpdate,
				CancellationToken = request.CancellationToken
			};

			var rankResult = await GetLeaderboardRankInternal(lb, definition, memberId, subRequest);
			return (lbId, Result: rankResult);
		}).ToList();

		var completedResults = await Task.WhenAll(tasks);
		return completedResults.ToDictionary(x => x.lbId, x => (LeaderboardPositionDto?)x.Result);
	}

	public async Task<Dictionary<string, PlayerLeaderboardEntryWithRankDto>> GetCachedPlayerLeaderboardRanks(
		string playerUuid, string profileId, int? maxRank = null) {
		var db = redis.GetDatabase();
		var result = new Dictionary<string, PlayerLeaderboardEntryWithRankDto>();

		var memberIdValue = await db.StringGetAsync($"memberid:{profileId}:{playerUuid}");
		var memberId = memberIdValue.HasValue ? memberIdValue.ToString() : null;

		var lookups = new List<(
			string Slug,
			ILeaderboardDefinition Definition,
			string ResourceId,
			string? IntervalIdentifier,
			Task<long?> RankTask,
			Task<double?> ScoreTask)>();

		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			var resourceId = definition.IsProfileLeaderboard() ? profileId : memberId;
			if (string.IsNullOrWhiteSpace(resourceId)) continue;

			var key = $"lb:{slug}:all";
			var intervalType = LbService.GetTypeFromSlug(slug);
			var intervalIdentifier = LbService.GetCurrentIdentifier(intervalType);

			lookups.Add((
				slug,
				definition,
				resourceId!,
				intervalIdentifier,
				db.SortedSetRankAsync(key, resourceId, Order.Descending),
				db.SortedSetScoreAsync(key, resourceId)));
		}

		await Task.WhenAll(lookups.SelectMany(x => new Task[] { x.RankTask, x.ScoreTask }));

		foreach (var lookup in lookups) {
			if (!lookup.RankTask.Result.HasValue) continue;

			var rank = (int)lookup.RankTask.Result.Value + 1;
			if (maxRank is not null && rank > maxRank.Value) continue;

			result[lookup.Slug] = new PlayerLeaderboardEntryWithRankDto {
				Title = lookup.Definition.Info.Title,
				Short = lookup.Definition.Info.ShortTitle,
				Slug = lookup.Slug,
				Profile = lookup.Definition.IsProfileLeaderboard() ? true : null,
				Rank = rank,
				IntervalIdentifier = lookup.IntervalIdentifier,
				Amount = lookup.ScoreTask.Result ?? 0,
				InitialAmount = 0, // Redis cache only stores current score.
				Type = lookup.Definition.Info.ScoreDataType
			};
		}

		return result;
	}

	public async Task<LeaderboardPositionDto> GetLeaderboardRank(LeaderboardRankRequest request) {
		var (lb, definition) = await GetLeaderboard(request.LeaderboardId);
		if (lb is null || definition is null) return new LeaderboardPositionDto { Rank = -1 };

		// Resolve MemberId
		string? memberId = null;
		if (definition is IMemberLeaderboardDefinition) {
			memberId = await ResolveMemberId(request.ProfileId, request.PlayerUuid, request.CancellationToken);
			if (string.IsNullOrEmpty(memberId)) return new LeaderboardPositionDto { Rank = -1 };
		}

		return await GetLeaderboardRankInternal(lb, definition, memberId, request);
	}

	private async Task<string?> ResolveMemberId(string? profileId, string? playerUuid, CancellationToken? ct = null) {
		if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(playerUuid)) return null;

		var memberIdCacheKey = $"memberid:{profileId}:{playerUuid}";
		return await cache.GetOrCreateAsync(memberIdCacheKey, async cancel => {
			var member = await context.ProfileMembers
				.AsNoTracking()
				.Where(p => p.ProfileId.Equals(profileId) && p.PlayerUuid.Equals(playerUuid))
				.Select(p => p.Id.ToString())
				.FirstOrDefaultAsync(cancel);
			return member;
		}, cancellationToken: ct ?? CancellationToken.None);
	}

	private async Task<LeaderboardPositionDto> GetLeaderboardRankInternal(
		Leaderboard lb,
		ILeaderboardDefinition definition,
		string? memberId,
		LeaderboardRankRequest request) {
		var db = redis.GetDatabase();
		var mode = request.GameMode ?? "all";
		var key = $"lb:{lb.Slug}:{mode}";

		// Check if data exists in Redis
		if (!await db.KeyExistsAsync(key)) {
			return new LeaderboardPositionDto {
				Rank = -1,
				Amount = 0,
				MinAmount = GetLeaderboardMinScore(lb.Slug),
				UpcomingRank = 10_000,
				UpcomingPlayers = [],
				Disabled = true
			};
		}

		if (definition.IsMemberLeaderboard() && string.IsNullOrWhiteSpace(memberId))
			return new LeaderboardPositionDto { Rank = -1 };

		var resourceId = definition.IsProfileLeaderboard() ? request.ProfileId : memberId;
		if (string.IsNullOrWhiteSpace(resourceId)) return new LeaderboardPositionDto { Rank = -1 };

		var rank = await db.SortedSetRankAsync(key, resourceId, Order.Descending);
		var position = rank.HasValue ? (int)rank.Value + 1 : -1;
		var score = await db.SortedSetScoreAsync(key, resourceId) ?? 0;
		List<LeaderboardEntryWithRankDto> upcomingPlayers = [];

		var anchorIndex = position > 0 ? position - 1 : -1;
		if (request.AtRank > 0 && (position == -1 || request.AtRank < position)) {
			anchorIndex = request.AtRank.Value - 1;
		}

		// Providing upcoming implies we want players *better* than us (lower rank index)
		if (request.Upcoming.HasValue && request.Upcoming.Value > 0) {
			long start = 0;
			long stop = 0;

			if (anchorIndex == -1) {
				start = -request.Upcoming.Value;
				stop = -1;
			}
			else {
				start = Math.Max(0, anchorIndex - request.Upcoming.Value);
				stop = anchorIndex - 1;
			}

			if (stop >= start) {
				var slice = await db.SortedSetRangeByRankWithScoresAsync(key, start, stop, Order.Descending);
				var dtos = (await MapRedisEntries(slice, lb.Slug)).Select(e => e.MapToDto()).ToList();

				// Calculate absolute rank based on the requested start index
				var actualStartRank = start;
				if (start < 0) {
					var count = await db.SortedSetLengthAsync(key);
					actualStartRank = count + start;
				}

				upcomingPlayers.AddRange(dtos.Select((dto, i) => new LeaderboardEntryWithRankDto {
					Ign = dto.Ign,
					Profile = dto.Profile,
					Uuid = dto.Uuid,
					Amount = dto.Amount,
					InitialAmount = dto.InitialAmount,
					Mode = dto.Mode,
					Members = dto.Members,
					Meta = dto.Meta,
					Removed = dto.Removed,
					Rank = (int)(actualStartRank + i + 1) // 1-based rank
				}));

				upcomingPlayers.Reverse();
			}
		}

		List<LeaderboardEntryDto> previousPlayers = [];
		if (request.Previous is > 0 && anchorIndex != -1) {
			long start = anchorIndex + 1;
			long stop = anchorIndex + request.Previous.Value;

			var slice = await db.SortedSetRangeByRankWithScoresAsync(key, start, stop, Order.Descending);
			var dtos = (await MapRedisEntries(slice, lb.Slug)).Select(e => e.MapToDto()).ToList();

			previousPlayers.AddRange(dtos.Select((dto, i) => new LeaderboardEntryDto {
				Ign = dto.Ign,
				Profile = dto.Profile,
				Uuid = dto.Uuid,
				Amount = dto.Amount,
				InitialAmount = dto.InitialAmount,
				Mode = dto.Mode,
				Members = dto.Members,
				Meta = dto.Meta,
				Removed = dto.Removed,
				// Rank = (int)(start + i + 1) // 1-based rank
			}));
		}

		return new LeaderboardPositionDto {
			Rank = position,
			Amount = score,
			MinAmount = await GetCachedMinScore(lb.Slug, request.GameMode),
			UpcomingRank = anchorIndex == -1 ? (int)(await db.SortedSetLengthAsync(key)) : (int)anchorIndex,
			UpcomingPlayers = upcomingPlayers,
			Previous = previousPlayers
		};
	}

	private async Task<List<LeaderboardEntry>> MapRedisEntries(SortedSetEntry[] entries, string leaderboardId) {
		var db = redis.GetDatabase();

		var tasks = entries.Select(async entry => {
			var memberId = entry.Element.ToString();
			var memberData = await db.HashGetAllAsync($"member:{memberId}");

			var profile = memberData.FirstOrDefault(x => x.Name == ProfileHash).Value.ToString();
			var ign = memberData.FirstOrDefault(x => x.Name == IgnHash).Value.ToString();
			var uuid = memberData.FirstOrDefault(x => x.Name == UuidHash).Value.ToString();

			return new LeaderboardEntry {
				MemberId = memberId,
				Amount = entry.Score,
				Profile = profile,
				Ign = ign,
				Uuid = uuid
			};
		}).ToList();

		var results = await Task.WhenAll(tasks);
		return results.ToList();
	}

	public async Task<(Leaderboard? lb, ILeaderboardDefinition? definition)> GetLeaderboard(string leaderboardId) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) return (null, null);

		var lb = await context.Leaderboards.AsNoTracking().FirstOrDefaultAsync(l => l.Slug == leaderboardId);
		return (lb, definition);
	}

	public double GetLeaderboardMinScore(string leaderboardId) {
		if (registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) {
			return (double)definition.Info.MinimumScore;
		}

		return 0;
	}

	public async Task<double> GetCachedMinScore(string leaderboardId, string? gameMode = "all") {
		var db = redis.GetDatabase();
		var key = $"lb-min:{leaderboardId}:{gameMode ?? "all"}";
		var val = await db.StringGetAsync(key);
		if (val.HasValue && double.TryParse(val.ToString(), out var min)) {
			return min;
		}

		return GetLeaderboardMinScore(leaderboardId);
	}
}
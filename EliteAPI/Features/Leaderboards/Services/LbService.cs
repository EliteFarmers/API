using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using EFCore.BulkExtensions;
using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILbService {
	Task<(Leaderboard? lb, ILeaderboardDefinition? definition)> GetLeaderboard(string leaderboardId);
	Task<List<LeaderboardEntryDto>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null);
	Task<LeaderboardEntryWithRankDto?> GetLastLeaderboardEntry(string leaderboardId, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null);
	double GetLeaderboardMinScore(string leaderboardId);
	Task UpdateMemberLeaderboardsAsync(ProfileMember member, CancellationToken c);
	Task UpdateProfileLeaderboardsAsync(EliteAPI.Models.Entities.Hypixel.Profile profile, CancellationToken c);
	Task<List<PlayerLeaderboardEntryWithRankDto>> GetPlayerLeaderboardEntriesWithRankAsync(Guid profileMemberId);
	Task<List<PlayerLeaderboardEntryWithRankDto>> GetProfileLeaderboardEntriesWithRankAsync(string profileId);
	Task<PlayerLeaderboardEntryWithRankDto?> GetLeaderboardEntryAsync(string leaderboardSlug, string memberOrProfileId, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null);
	Task<LeaderboardPositionDto?> GetLeaderboardRank(string leaderboardId, string playerUuid, string profileId, int? upcoming = null, int? atRank = null, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null, CancellationToken? c = null);
	(long start, long end) GetCurrentTimeRange(LeaderboardType type);
}

[RegisterService<ILbService>(LifeTime.Scoped)]
public class LbService(
	ILeaderboardRegistrationService registrationService,
	ILogger<LbService> logger,
	IConnectionMultiplexer redis,
	IMemberService memberService,
	DataContext context) 
	: ILbService 
{
	public async Task<(Leaderboard? lb, ILeaderboardDefinition? definition)> GetLeaderboard(string leaderboardId) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) return (null, null);
		
		var lb = await context.Leaderboards
			.AsNoTracking()
			.FirstOrDefaultAsync(lb => lb.Slug == leaderboardId);
		
		return (lb, definition);
	}

	public async Task<List<LeaderboardEntryDto>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null) {
		var (lb, definition) = await GetLeaderboard(leaderboardId);
		if (lb is null || definition is null) return [];
		
		if (definition is IMemberLeaderboardDefinition) {
			return await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, true)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.OrderByDescending(e => e.Score)
				.Skip(offset)
				.Take(limit)
				.MapToMemberLeaderboardEntries(limit <= 20)
				.ToListAsync();
		} 
		
		if (definition is IProfileLeaderboardDefinition) {
			return await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, false)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.OrderByDescending(e => e.Score)
				.Skip(offset)
				.Take(limit)
				.MapToProfileLeaderboardEntries(removedFilter)
				.ToListAsync();
		}
		
		return [];
	}

	public async Task<LeaderboardEntryWithRankDto?> GetLastLeaderboardEntry(string leaderboardId, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null) {
		var (lb, definition) = await GetLeaderboard(leaderboardId);
		if (lb is null || definition is null) return null;
		
		var key = $"leaderboard-max:{leaderboardId}:{gameMode ?? "all"}:{identifier ?? "current"}:{removedFilter}";
		var db = redis.GetDatabase();
		if (await db.KeyExistsAsync(key)) {
			var uuid = await db.StringGetAsync(key);
			if (uuid.HasValue) {
				return JsonSerializer.Deserialize<LeaderboardEntryWithRankDto>(uuid.ToString());
			}
		}
		
		LeaderboardEntryWithRankDto? entry = null;
		var rank = -1;
		
		if (definition is IMemberLeaderboardDefinition) {
			entry = await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, true)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.OrderBy(e => e.Score)
				.Select(e => new LeaderboardEntryWithRankDto {
					Uuid = e.ProfileMember!.PlayerUuid,
					Profile = e.ProfileMember.ProfileName,
					Amount = (double) e.Score,
					InitialAmount = (double) e.InitialScore,
					Mode = e.ProfileType,
					Removed = e.IsRemoved,
					Ign = e.ProfileMember.MinecraftAccount.Name
				}).FirstOrDefaultAsync();
			
			rank = await context.LeaderboardEntries
				.FromLeaderboard(lb.LeaderboardId, true)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.CountAsync() + 1;
		} 
		
		if (definition is IProfileLeaderboardDefinition) {
			entry = await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, false)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.Include(e => e.Profile)
				.OrderBy(e => e.Score)
				.Select(e => new LeaderboardEntryWithRankDto {
					Uuid = e.Profile!.ProfileId,
					Profile = e.Profile!.ProfileName,
					Amount = (double) e.Score,
					InitialAmount = (double) e.InitialScore,
					Mode = e.ProfileType,
					Removed = e.IsRemoved,
					Members = e.Profile.Members
						.Where(m => 
							(removedFilter == RemovedFilter.NotRemoved && m.WasRemoved == false)
							|| (removedFilter == RemovedFilter.Removed && m.WasRemoved == true)
							|| removedFilter == RemovedFilter.All)
						.Select(m => new ProfileLeaderboardMemberDto {
							Ign = m.MinecraftAccount.Name,
							Uuid = m.PlayerUuid,
							Xp = m.SkyblockXp,
							Removed = m.WasRemoved
						}).OrderByDescending(s => s.Xp).ToList()
				}).FirstOrDefaultAsync();
			
			rank = await context.LeaderboardEntries
				.FromLeaderboard(lb.LeaderboardId, false)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.CountAsync() + 1;
		}

		if (entry is not null) {
			entry.Rank = rank;

			if (rank != -1) {
				await db.StringSetAsync(key, JsonSerializer.Serialize(entry), TimeSpan.FromMinutes(1));
			}
			
			return entry;
		}

		return null;
	}

	public double GetLeaderboardMinScore(string leaderboardId) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) return 0;
		return (double) definition.Info.MinimumScore;
	}

	public async Task UpdateMemberLeaderboardsAsync(ProfileMember member, CancellationToken c) {
		if (member.Profile?.GameMode == "bingo") return;
		var time = DateTime.UtcNow;
		
		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);
		
		var leaderboardsIdsBySlug = await context.Leaderboards
			.AsNoTracking()
			.Select(lb => new { lb.Slug, lb.LeaderboardId, lb.MinimumScore })
			.ToDictionaryAsync(lb => lb.Slug, c);
		
		var existingEntryList = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e => 
				e.ProfileMemberId == member.Id
				&& (e.IntervalIdentifier == monthlyInterval || e.IntervalIdentifier == weeklyInterval || e.IntervalIdentifier == null))
			.ToListAsync(cancellationToken: c);

		var existingEntries = new Dictionary<int, Models.LeaderboardEntry>();
		List<Models.LeaderboardEntry>? failed = null;
		
		// Add existing entries to a dictionary to check for duplicates
		foreach (var entry in existingEntryList.Where(entry => !existingEntries.TryAdd(entry.LeaderboardId, entry))) {
			failed ??= [];
			failed.Add(entry);
		}
		
		// Delete duplicate entries (this should be very rare, but less expensive than a unique db constraint)
		if (failed is { Count: > 0 }) {
			await context.BulkDeleteAsync(failed, cancellationToken: c);
			logger.LogWarning("Deleted {Count} duplicate leaderboard entries for {Player}", failed.Count, member.PlayerUuid);
		}

		var updatedEntries = new List<Models.LeaderboardEntry>();
		var newEntries = new List<Models.LeaderboardEntry>();
		var ranRestore = false;
		
		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			if (definition is not IMemberLeaderboardDefinition memberLb) continue;
			
			var useIncrease = definition.Info.UseIncreaseForInterval;
			if (!leaderboardsIdsBySlug.TryGetValue(slug, out var lb)) continue;

			var type = GetTypeFromSlug(slug);
			var intervalIdentifier = type switch {
				LeaderboardType.Monthly => monthlyInterval,
				LeaderboardType.Weekly => weeklyInterval,
				_ => null
			};

			var score = memberLb.GetScoreFromMember(member, type);
				
			if (existingEntries.TryGetValue(lb.LeaderboardId, out var entry)) {
				var changed = false;
				
				if (entry.IsRemoved != member.WasRemoved) {
					entry.IsRemoved = member.WasRemoved;
					changed = true;

					if (entry.IsRemoved == false && !ranRestore) {
						await RestoreMemberLeaderboards(member.Id, c);
						ranRestore = true;
					}
				}

				if (score >= 0 && (score >= entry.InitialScore || !useIncrease)) {
					var newScore = (entry.IntervalIdentifier is not null && useIncrease)
						? score - entry.InitialScore
						: score;
					
					if (entry.Score != newScore) {
						entry.Score = newScore;
						changed = true;
					}
				}

				if (changed) {
					updatedEntries.Add(entry);
				}
				continue;
			}
			
			if (score <= 0 || score < lb.MinimumScore) continue;
				
			var newEntry = new Models.LeaderboardEntry() {
				LeaderboardId = lb.LeaderboardId,
				IntervalIdentifier = intervalIdentifier,
				
				ProfileMemberId = member.Id,
				 
				InitialScore = useIncrease && intervalIdentifier is not null ? score : 0,
				Score = useIncrease && intervalIdentifier is not null ? 0 : score,
				
				IsRemoved = member.WasRemoved,
				ProfileType = member.Profile?.GameMode
			};
			
			newEntries.Add(newEntry);
		}

		if (updatedEntries.Count != 0) {
			var options = new BulkConfig() {
				PropertiesToIncludeOnUpdate = [
					nameof(Models.LeaderboardEntry.Score),
					nameof(Models.LeaderboardEntry.InitialScore),
					nameof(Models.LeaderboardEntry.IsRemoved)
				]
			};
			await context.BulkUpdateAsync(updatedEntries, options, cancellationToken: c);
			logger.LogInformation("Updated {Count} leaderboard entries", updatedEntries.Count);
		}
		
		if (newEntries.Count != 0) {
			var options = new BulkConfig() {
				ConflictOption = ConflictOption.Ignore
			};
			await context.BulkInsertAsync(newEntries, options, cancellationToken: c);
			logger.LogInformation("Inserted {Count} new leaderboard entries", newEntries.Count);
		}
		
		logger.LogInformation("Updating member leaderboards for {Player} on {Profile} took {Time}ms", 
			member.PlayerUuid, 
			member.Profile?.ProfileId, 
			DateTime.UtcNow.Subtract(time).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)
		);
	}

	private async Task RestoreMemberLeaderboards(Guid profileMemberId, CancellationToken c = default) {
		await context.LeaderboardEntries
			.Where(e => e.ProfileMemberId == profileMemberId && e.IsRemoved == true)
			.ExecuteUpdateAsync(s => s.SetProperty(le => le.IsRemoved, false), cancellationToken: c);
	}

	public async Task UpdateProfileLeaderboardsAsync(EliteAPI.Models.Entities.Hypixel.Profile profile, CancellationToken c) {
		if (profile.GameMode == "bingo") return;
		var time = DateTime.UtcNow;
	
		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);
		
		var leaderboardsIdsBySlug = await context.Leaderboards
			.AsNoTracking()
			.Select(lb => new { lb.Slug, lb.LeaderboardId, lb.MinimumScore })
			.ToDictionaryAsync(lb => lb.Slug, c);
		
		var existingEntryList = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e => 
				e.ProfileId == profile.ProfileId
				&& (e.IntervalIdentifier == monthlyInterval || e.IntervalIdentifier == weeklyInterval || e.IntervalIdentifier == null))
			.ToListAsync(c);
		
		var existingEntries = new Dictionary<int, Models.LeaderboardEntry>();
		List<Models.LeaderboardEntry>? failed = null;
		
		// Add existing entries to a dictionary to check for duplicates
		foreach (var entry in existingEntryList.Where(entry => !existingEntries.TryAdd(entry.LeaderboardId, entry))) {
			failed ??= [];
			failed.Add(entry);
		}
		
		// Delete duplicate entries (this should be very rare, but less expensive than a unique db constraint)
		if (failed is { Count: > 0 }) {
			await context.BulkDeleteAsync(failed, cancellationToken: c);
			logger.LogWarning("Deleted {Count} duplicate leaderboard entries for {Profile}", failed.Count, profile.ProfileId);
		}

		var updatedEntries = new List<Models.LeaderboardEntry>();
		var newEntries = new List<Models.LeaderboardEntry>();
		var ranRestore = false;
		
		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			if (definition is not IProfileLeaderboardDefinition profileLb) continue;
			
			var useIncrease = definition.Info.UseIncreaseForInterval;
			if (!leaderboardsIdsBySlug.TryGetValue(slug, out var lb)) continue;

			var type = GetTypeFromSlug(slug);
			var intervalIdentifier = type switch {
				LeaderboardType.Monthly => monthlyInterval,
				LeaderboardType.Weekly => weeklyInterval,
				_ => null
			};
			
			var score = profileLb.GetScoreFromProfile(profile, type);
			if (score == -1 && profile.Garden is not null) {
				score = profileLb.GetScoreFromGarden(profile.Garden, type);
			}

			if (existingEntries.TryGetValue(lb.LeaderboardId, out var entry)) {
				var changed = false;
				if (entry.IsRemoved != profile.IsDeleted) {
					entry.IsRemoved = profile.IsDeleted;
					changed = true;
					
					if (entry.IsRemoved == false && !ranRestore) {
						await RestoreProfileLeaderboards(profile.ProfileId, c);
						ranRestore = true;
					}
				}
					
				if (score >= 0 && (score >= entry.InitialScore || !useIncrease)) {
					var newScore = (entry.IntervalIdentifier is not null && useIncrease)
						? score - entry.InitialScore 
						: score;
					
					if (entry.Score != newScore) {
						entry.Score = newScore;
						changed = true;
					}
				}
				
				if (changed) {
					updatedEntries.Add(entry);
				}
				continue;
			}
			
			if (score <= 0 || score < lb.MinimumScore) continue;

			var newEntry = new Models.LeaderboardEntry {
				LeaderboardId = lb.LeaderboardId,
				IntervalIdentifier = intervalIdentifier,
					
				ProfileId = profile.ProfileId,
				 
				InitialScore = useIncrease && intervalIdentifier is not null ? score : 0,
				Score = useIncrease && intervalIdentifier is not null ? 0 : score,
				
				IsRemoved = profile.IsDeleted,
				ProfileType = profile.GameMode
			};
				
			newEntries.Add(newEntry);
		}

		if (updatedEntries.Count != 0) {
			var options = new BulkConfig() {
				PropertiesToIncludeOnUpdate = [
					nameof(Models.LeaderboardEntry.Score),
					nameof(Models.LeaderboardEntry.InitialScore),
					nameof(Models.LeaderboardEntry.IsRemoved)
				]
			};
			await context.BulkUpdateAsync(updatedEntries, options, cancellationToken: c);
			logger.LogInformation("Updated {Count} leaderboard entries", updatedEntries.Count);
		}
		
		if (newEntries.Count != 0) {
			var options = new BulkConfig() {
				ConflictOption = ConflictOption.Ignore
			};
			await context.BulkInsertAsync(newEntries, options, cancellationToken: c);
			logger.LogInformation("Inserted {Count} new leaderboard entries", newEntries.Count);
		}
		
		logger.LogInformation("Updating profile leaderboards for {Profile} took {Time}ms", 
			profile!.ProfileId,
			DateTime.UtcNow.Subtract(time).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)
		);
	}
	
	private async Task RestoreProfileLeaderboards(string profileId, CancellationToken c = default) {
		await context.LeaderboardEntries
			.Where(e => e.ProfileId == profileId && e.IsRemoved == true)
			.ExecuteUpdateAsync(s => s.SetProperty(le => le.IsRemoved, false), cancellationToken: c);
	}
	
	public async Task<PlayerLeaderboardEntryWithRankDto?> GetLeaderboardEntryAsync(string leaderboardSlug, string memberOrProfileId, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardSlug, out var definition)) return null;

		var lb = await context.Leaderboards
			.AsNoTracking()
			.FirstOrDefaultAsync(lb => lb.Slug == leaderboardSlug);
		if (lb is null) return null;

		identifier ??= GetCurrentIdentifier(GetTypeFromSlug(leaderboardSlug));
		
		var entry = await (definition.IsMemberLeaderboard()
			? context.LeaderboardEntries
				.Where(e => e.ProfileMemberId == Guid.Parse(memberOrProfileId) && e.LeaderboardId == lb.LeaderboardId)
				.EntryFilter(interval: identifier, removedFilter: removedFilter, gameMode: gameMode)
			: context.LeaderboardEntries
				.Where(e => e.ProfileId == memberOrProfileId && e.LeaderboardId == lb.LeaderboardId)
				.EntryFilter(interval: identifier, removedFilter: removedFilter, gameMode: gameMode))
		.Select(le => new {
			le.LeaderboardId,
			le.IntervalIdentifier,
			le.Score,
			le.InitialScore,
			le.IsRemoved,
			le.ProfileType,
			le.Leaderboard // Include the Leaderboard for its Title/Slug etc.
		})
		.FirstOrDefaultAsync();
		if (entry is null) return null;
	
		// Calculate the rank for this specific entry
		var rank = entry.IsRemoved ? -1 : await context.LeaderboardEntries
			.FromLeaderboard(entry.LeaderboardId, definition.IsMemberLeaderboard())
			.EntryFilter(interval: identifier, removedFilter: removedFilter, gameMode: gameMode)
			.Where(otherEntry => otherEntry.Score > entry.Score)
			.CountAsync() + 1; // Rank is 1 + the number of entries with a higher score

		return new PlayerLeaderboardEntryWithRankDto
		{
			IntervalIdentifier = entry.IntervalIdentifier,
			Amount = (double) entry.Score,
			InitialAmount = (double) entry.InitialScore,
			Profile = definition.IsProfileLeaderboard(),
			Rank = rank,
			Title = entry.Leaderboard.Title,
			Slug = entry.Leaderboard.Slug,
			Short = entry.Leaderboard.ShortTitle,
			Type = entry.Leaderboard.ScoreDataType
		};
	}

	public async Task<LeaderboardPositionDto?> GetLeaderboardRank(
		string leaderboardId, string playerUuid, string profileId, int? upcoming = null, 
		int? atRank = null, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, 
		string? identifier = null, CancellationToken? c = null) 
	{
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) return null;
        
		var memberId = profileId;

		// Set the memberId to the profile member ID if the leaderboard is not a profile leaderboard
		if (definition.IsMemberLeaderboard()) {
			var member = await context.ProfileMembers
				.Where(p => p.ProfileId.Equals(profileId) && p.PlayerUuid.Equals(playerUuid))
				.Select(p => new { p.Id, p.LastUpdated, p.PlayerUuid })
				.FirstOrDefaultAsync(cancellationToken: c ?? CancellationToken.None);

			if (member is null || member.Id == Guid.Empty) {
				return null;
			}
            
			await memberService.UpdatePlayerIfNeeded(member.PlayerUuid, 4f);
            
			memberId = member.Id.ToString();
		}

		var entry = await GetLeaderboardEntryAsync(leaderboardId, memberId, gameMode, removedFilter, identifier);
		
		var position = entry?.Rank ?? -1;
		List<LeaderboardEntryDto>? upcomingPlayers = null;
        
		var rank = atRank is -1 or null ? position : Math.Max(1, atRank.Value);
		rank = position != -1 ? Math.Min(position, rank) : rank;
		
		if (upcoming > 0 && rank > 1) {
			upcomingPlayers = await GetLeaderboardSlice(leaderboardId, Math.Max(rank - upcoming.Value - 1, 0), Math.Min(rank - 1, upcoming.Value), gameMode, removedFilter, identifier);
		}

		// Reverse the list of upcoming players to show the closest upcoming player first
		upcomingPlayers?.Reverse();
		
		var result = new LeaderboardPositionDto {
			Rank = position,
			Amount = entry?.Amount ?? 0,
			InitialAmount = entry?.InitialAmount ?? 0,
			MinAmount = (double) definition.Info.MinimumScore,
			UpcomingRank = rank == -1 ? -1 : rank - 1,
			UpcomingPlayers = upcomingPlayers ?? [],
		};

		return result;
	}

	public static string? GetCurrentIdentifier(LeaderboardType type) {
		switch (type) {
			case LeaderboardType.Current:
				return null;
			case LeaderboardType.Weekly:
				var now = DateTime.UtcNow;
				var week = ISOWeek.GetWeekOfYear(now);
				return $"{now.Year}-W{week}";
			case LeaderboardType.Monthly:
				return DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}
	}
	
	public (long start, long end) GetCurrentTimeRange(LeaderboardType type) {
		switch (type) {
			case LeaderboardType.Current:
				return (0, 0);
			case LeaderboardType.Weekly:
				var nowUtc = DateTime.UtcNow;
				var isoYear = ISOWeek.GetYear(nowUtc);
				var isoWeekNumber = ISOWeek.GetWeekOfYear(nowUtc);

				var startOfWeekUtc = ISOWeek.ToDateTime(isoYear, isoWeekNumber, DayOfWeek.Monday).ToUniversalTime();
				var endOfWeekUtc = ISOWeek.ToDateTime(isoYear, isoWeekNumber, DayOfWeek.Sunday).ToUniversalTime();

				var startTimestamp = ((DateTimeOffset)startOfWeekUtc).ToUnixTimeSeconds();
				var endTimestamp = ((DateTimeOffset)endOfWeekUtc).ToUnixTimeSeconds();

				return (startTimestamp, endTimestamp);
			case LeaderboardType.Monthly:
				var now = DateTimeOffset.UtcNow;
				var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
				var endOfMonth = startOfMonth.AddMonths(1);
				
				return (startOfMonth.ToUnixTimeSeconds(), endOfMonth.ToUnixTimeSeconds());
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}
	}

	public static LeaderboardType GetTypeFromSlug(string slug) {
		return slug switch {
			_ when slug.EndsWith("-monthly") => LeaderboardType.Monthly,
			_ when slug.EndsWith("-weekly") => LeaderboardType.Weekly,
			_ => LeaderboardType.Current
		};
	}

	public async Task<List<PlayerLeaderboardEntryWithRankDto>> GetPlayerLeaderboardEntriesWithRankAsync(Guid profileMemberId)
    {
	    var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
	    var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);
	    
        var playerEntries = await context.LeaderboardEntries
            .Where(le => le.ProfileMemberId == profileMemberId
				&& (le.IntervalIdentifier == monthlyInterval || le.IntervalIdentifier == weeklyInterval || le.IntervalIdentifier == null))
            .Select(le => new
            {
                le.LeaderboardId,
                le.IntervalIdentifier,
                le.Score,
                le.InitialScore,
                le.IsRemoved,
                le.ProfileType,
                le.Leaderboard // Include the Leaderboard for its Title/Slug etc.
            })
            .ToListAsync();

        var results = new List<PlayerLeaderboardEntryWithRankDto>();

        foreach (var entry in playerEntries)
        {
            // Calculate the rank for this specific entry
            var rank = entry.IsRemoved ? -1 : await context.LeaderboardEntries
                .Where(otherEntry =>
                    otherEntry.LeaderboardId == entry.LeaderboardId &&
                    otherEntry.IntervalIdentifier == entry.IntervalIdentifier &&
                    otherEntry.Score > entry.Score &&
                    otherEntry.IsRemoved == false
                )
                .CountAsync() + 1; // Rank is 1 + the number of entries with a higher score

            results.Add(new PlayerLeaderboardEntryWithRankDto
            {
                IntervalIdentifier = entry.IntervalIdentifier,
                Amount = (double) entry.Score,
                InitialAmount = (double) entry.InitialScore,
                Rank = rank,
                Title = entry.Leaderboard.Title,
	            Slug = entry.Leaderboard.Slug,
	            Short = entry.Leaderboard.ShortTitle,
	            Type = entry.Leaderboard.ScoreDataType
            });
        }

        return results;
    }
	
	public async Task<List<PlayerLeaderboardEntryWithRankDto>> GetProfileLeaderboardEntriesWithRankAsync(string profileId)
	{
		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);
		
		var playerEntries = await context.LeaderboardEntries
			.Where(le => le.ProfileId == profileId
				&& (le.IntervalIdentifier == monthlyInterval || le.IntervalIdentifier == weeklyInterval || le.IntervalIdentifier == null))
			.Select(le => new
			{
				le.LeaderboardId,
				le.IntervalIdentifier,
				le.Score,
				le.InitialScore,
				le.IsRemoved,
				le.ProfileType,
				le.Leaderboard // Include the Leaderboard for its Title/Slug etc.
			})
			.ToListAsync();

		var results = new List<PlayerLeaderboardEntryWithRankDto>();

		foreach (var entry in playerEntries)
		{
			// Calculate the rank for this specific entry
			var rank = entry.IsRemoved ? -1 : await context.LeaderboardEntries
				.Where(otherEntry =>
					otherEntry.LeaderboardId == entry.LeaderboardId &&
					otherEntry.IntervalIdentifier == entry.IntervalIdentifier &&
					otherEntry.Score > entry.Score &&
					otherEntry.IsRemoved == false
				)
				.CountAsync() + 1; // Rank is 1 + the number of entries with a higher score

			results.Add(new PlayerLeaderboardEntryWithRankDto
			{
				IntervalIdentifier = entry.IntervalIdentifier,
				Amount = (double) entry.Score,
				InitialAmount = (double) entry.InitialScore,
				Profile = true,
				Rank = rank,
				Title = entry.Leaderboard.Title,
				Slug = entry.Leaderboard.Slug,
				Short = entry.Leaderboard.ShortTitle,
				Type = entry.Leaderboard.ScoreDataType
			});
		}

		return results;
	}
}

public class PlayerLeaderboardEntryWithRankDto
{	
	public required string Title { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Short { get; set; }
	public required string Slug { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Profile { get; set; }
	public int Rank { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? IntervalIdentifier { get; set; }
	public double Amount { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double InitialAmount { get; set; }
	
	[JsonConverter(typeof(JsonStringEnumConverter<LeaderboardScoreDataType>))]
	public LeaderboardScoreDataType Type { get; set; }
}

public enum RemovedFilter {
	NotRemoved = 0,
	Removed = 1,
	All = 2
}
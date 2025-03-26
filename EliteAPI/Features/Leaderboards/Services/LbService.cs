using System.Globalization;
using System.Text.Json.Serialization;
using EFCore.BulkExtensions;
using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILbService {
	Task<(Leaderboard? lb, ILeaderboardDefinition? definition)> GetLeaderboard(string leaderboardId);
	Task<List<LeaderboardEntryDto>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
	Task UpdateMemberLeaderboardsAsync(ProfileMember member, CancellationToken c);
	Task UpdateProfileLeaderboardsAsync(EliteAPI.Models.Entities.Hypixel.Profile profile, CancellationToken c);
	Task<List<PlayerLeaderboardEntryWithRankDto>> GetPlayerLeaderboardEntriesWithRankAsync(Guid profileMemberId);
	Task<List<PlayerLeaderboardEntryWithRankDto>> GetProfileLeaderboardEntriesWithRankAsync(string profileId);
	Task<PlayerLeaderboardEntryWithRankDto?> GetLeaderboardEntryAsync(string leaderboardSlug, string memberOrProfileId, string? intervalIdentifier = null);
	Task<LeaderboardPositionDto?> GetLeaderboardRank(string leaderboardId, string playerUuid, string profileId, int? upcoming = null, int? atRank = null, CancellationToken? c = null);
	string? GetCurrentIdentifier(LeaderboardType type);
}

[RegisterService<ILbService>(LifeTime.Scoped)]
public class LbService(
	ILeaderboardRegistrationService registrationService,
	ILogger<LbService> logger,
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

	public async Task<List<LeaderboardEntryDto>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20) {
		var (lb, definition) = await GetLeaderboard(leaderboardId);
		if (lb is null || definition is null) return [];

		if (definition is IMemberLeaderboardDefinition) {
			return await context.LeaderboardEntries.AsNoTracking()
				.Where(e => e.LeaderboardId == lb.LeaderboardId && e.ProfileMemberId != null && !e.IsRemoved)
				.OrderByDescending(e => e.Score)
				.Skip(offset)
				.Take(limit)
				.Select(e => new LeaderboardEntryDto {
					Uuid = e.ProfileMember!.PlayerUuid,
					Profile = e.ProfileMember.ProfileName,
					Amount = (double) e.Score,
					Removed = e.IsRemoved,
					Ign = e.ProfileMember.MinecraftAccount.Name
				}).ToListAsync();
		} 
		
		if (definition is IProfileLeaderboardDefinition) {
			return await context.LeaderboardEntries.AsNoTracking()
				.Where(e => e.LeaderboardId == lb.LeaderboardId && e.ProfileId != null && !e.IsRemoved)
				.Include(e => e.Profile)
				.OrderByDescending(e => e.Score)
				.Skip(offset)
				.Take(limit)
				.Select(e => new LeaderboardEntryDto {
					Uuid = e.Profile!.ProfileId,
					Profile = e.Profile!.ProfileName,
					Amount = (double) e.Score,
					Removed = e.IsRemoved,
					Members = e.Profile.Members
						.Where(m => !m.WasRemoved)
						.Select(m => new ProfileLeaderboardMemberDto {
							Ign = m.MinecraftAccount.Name,
							Uuid = m.PlayerUuid,
							Xp = m.SkyblockXp
						}).OrderByDescending(s => s.Xp).ToList()
				}).ToListAsync();
		}
		
		return [];
	}

	public async Task UpdateMemberLeaderboardsAsync(ProfileMember member, CancellationToken c) {
		if (member.Profile?.GameMode == "bingo") return;
		var time = DateTime.UtcNow;
		
		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);
		
		var leaderboardsIdsBySlug = await context.Leaderboards
			.AsNoTracking()
			.Select(lb => new { lb.Slug, lb.LeaderboardId })
			.ToDictionaryAsync(lb => lb.Slug, c);
		
		var existingEntries = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e => 
				e.ProfileMemberId == member.Id
				&& (e.IntervalIdentifier == monthlyInterval || e.IntervalIdentifier == weeklyInterval || e.IntervalIdentifier == null))
			.ToDictionaryAsync(e => e.LeaderboardId, c);

		var updatedEntries = new List<Models.LeaderboardEntry>();
		var newEntries = new List<Models.LeaderboardEntry>();
		
		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			var useIncrease = definition.Info.UseIncreaseForInterval;
			if (!leaderboardsIdsBySlug.TryGetValue(slug, out var lb)) continue;

			var intervalIdentifier = slug switch {
				_ when slug.EndsWith("-monthly") => monthlyInterval,
				_ when slug.EndsWith("-weekly") => weeklyInterval,
				_ => null
			};

			if (definition is not IMemberLeaderboardDefinition memberLb) continue;
			var score = memberLb.GetScoreFromMember(member);
				
			if (existingEntries.TryGetValue(lb.LeaderboardId, out var entry)) {
				entry.IsRemoved = member.WasRemoved;

				if (score is not null) {
					var newScore = (entry.IntervalIdentifier is not null && useIncrease)
						? score.ToDecimal(CultureInfo.InvariantCulture) - entry.InitialScore
						: score.ToDecimal(CultureInfo.InvariantCulture);

					entry.Score = newScore;
				}

				updatedEntries.Add(entry);
				continue;
			}
				
			if (score is null) continue;
				
			var newEntry = new Models.LeaderboardEntry() {
				LeaderboardId = lb.LeaderboardId,
				IntervalIdentifier = intervalIdentifier,
					
				ProfileMemberId = member.Id,
				 
				InitialScore = useIncrease && intervalIdentifier is not null ? score.ToDecimal(CultureInfo.InvariantCulture) : 0,
				Score = useIncrease && intervalIdentifier is not null ? 0 : score.ToDecimal(CultureInfo.InvariantCulture),
				
				IsRemoved = member.WasRemoved,
				ProfileType = member.Profile?.GameMode
			};
			
			newEntries.Add(newEntry);
		}

		if (updatedEntries.Count != 0) {
			await context.BulkUpdateAsync(updatedEntries, cancellationToken: c);
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

	public async Task UpdateProfileLeaderboardsAsync(EliteAPI.Models.Entities.Hypixel.Profile profile, CancellationToken c) {
		if (profile.GameMode == "bingo") return;
		var time = DateTime.UtcNow;
	
		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);
		
		var leaderboardsIdsBySlug = await context.Leaderboards
			.AsNoTracking()
			.Select(lb => new { lb.Slug, lb.LeaderboardId })
			.ToDictionaryAsync(lb => lb.Slug, c);
		
		var existingEntries = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e => 
				e.ProfileId == profile.ProfileId
				&& (e.IntervalIdentifier == monthlyInterval || e.IntervalIdentifier == weeklyInterval || e.IntervalIdentifier == null))
			.ToDictionaryAsync(e => e.LeaderboardId, c);

		var updatedEntries = new List<Models.LeaderboardEntry>();
		var newEntries = new List<Models.LeaderboardEntry>();
		
		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			var useIncrease = definition.Info.UseIncreaseForInterval;
			if (!leaderboardsIdsBySlug.TryGetValue(slug, out var lb)) continue;

			var intervalIdentifier = slug switch {
				_ when slug.EndsWith("-monthly") => monthlyInterval,
				_ when slug.EndsWith("-weekly") => weeklyInterval,
				_ => null
			};

			if (definition is not IProfileLeaderboardDefinition profileLb) continue;
			
			var score =
				(profile is not null ? profileLb.GetScoreFromProfile(profile) : null)
				?? (profile?.Garden is not null ? profileLb.GetScoreFromGarden(profile.Garden) : null);
				
			if (existingEntries.TryGetValue(lb.LeaderboardId, out var entry)) {
				entry.IsRemoved = profile!.IsDeleted;
					
				if (score is not null) {
					var newScore = (entry.IntervalIdentifier is not null && useIncrease)
						? score.ToDecimal(CultureInfo.InvariantCulture) - entry.InitialScore 
						: score.ToDecimal(CultureInfo.InvariantCulture);
					
					entry.Score = newScore;
				}
				
				updatedEntries.Add(entry);
				continue;
			}
			
			if (score is null) continue;

			var newEntry = new Models.LeaderboardEntry() {
				LeaderboardId = lb.LeaderboardId,
				IntervalIdentifier = intervalIdentifier,
					
				ProfileId = profile!.ProfileId,
				 
				InitialScore = useIncrease && intervalIdentifier is not null ? score.ToDecimal(CultureInfo.InvariantCulture) : 0,
				Score = useIncrease && intervalIdentifier is not null ? 0 : score.ToDecimal(CultureInfo.InvariantCulture),
				
				IsRemoved = profile.IsDeleted,
				ProfileType = profile.GameMode
			};
				
			newEntries.Add(newEntry);
		}

		if (updatedEntries.Count != 0) {
			await context.BulkUpdateAsync(updatedEntries, cancellationToken: c);
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
	
	public async Task<PlayerLeaderboardEntryWithRankDto?> GetLeaderboardEntryAsync(string leaderboardSlug, string memberOrProfileId, string? intervalIdentifier = null) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardSlug, out var definition)) return null;

		var lb = await context.Leaderboards
			.AsNoTracking()
			.FirstOrDefaultAsync(lb => lb.Slug == leaderboardSlug);
		if (lb is null) return null;
		
		intervalIdentifier ??= leaderboardSlug switch {
			_ when leaderboardSlug.EndsWith("-monthly") => GetCurrentIdentifier(LeaderboardType.Monthly),
			_ when leaderboardSlug.EndsWith("-weekly") => GetCurrentIdentifier(LeaderboardType.Weekly),
			_ => null
		};
		
		var entry = await (definition.IsMemberLeaderboard()
			? context.LeaderboardEntries.Where(e => e.ProfileMemberId == Guid.Parse(memberOrProfileId) && e.LeaderboardId == lb.LeaderboardId && e.IntervalIdentifier == intervalIdentifier)
			: context.LeaderboardEntries.Where(e => e.ProfileId == memberOrProfileId && e.LeaderboardId == lb.LeaderboardId && e.IntervalIdentifier == intervalIdentifier))
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
			.Where(otherEntry =>
				otherEntry.LeaderboardId == entry.LeaderboardId &&
				otherEntry.IntervalIdentifier == entry.IntervalIdentifier &&
				otherEntry.Score > entry.Score &&
				otherEntry.IsRemoved == false
			)
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
	

	public async Task<LeaderboardPositionDto?> GetLeaderboardRank(string leaderboardId, string playerUuid, string profileId, int? upcoming = null,
		int? atRank = null, CancellationToken? c = null) 
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
            
			await memberService.UpdatePlayerIfNeeded(member.PlayerUuid, 2.5f);
            
			memberId = member.Id.ToString();
		}

		var entry = await GetLeaderboardEntryAsync(leaderboardId, memberId);
		if (entry is null) return null;
		
		var position = entry.Rank;
		List<LeaderboardEntryDto>? upcomingPlayers = null;
        
		var rank = atRank is -1 or null ? position : Math.Max(1, atRank.Value);

		if (upcoming > 0 && rank > 1) {
			upcomingPlayers = await GetLeaderboardSlice(leaderboardId, Math.Max(rank - upcoming.Value - 1, 0), Math.Min(rank - 1, upcoming.Value));
		}

		// Reverse the list of upcoming players to show the closest upcoming player first
		upcomingPlayers?.Reverse();
		
		var result = new LeaderboardPositionDto {
			Rank = position,
			Amount = entry.Amount,
			UpcomingRank = rank == -1 ? -1 : rank - 1,
			UpcomingPlayers = upcomingPlayers ?? [],
		};

		return result;
	}

	public string? GetCurrentIdentifier(LeaderboardType type) {
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
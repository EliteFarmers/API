using System.Globalization;
using System.Text.Json.Serialization;
using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILbService {
	Task<(Leaderboard? lb, ILeaderboardDefinition? definition)> GetLeaderboard(string leaderboardId);
	Task<List<LeaderboardEntryDto>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
	Task UpdateMemberLeaderboardsAsync(ProfileMember member, CancellationToken c);
	Task UpdateProfileLeaderboardsAsync(EliteAPI.Models.Entities.Hypixel.Profile profile, CancellationToken c);
	Task UpdateGardenLeaderboardsAsync(EliteAPI.Models.Entities.Hypixel.Garden member, CancellationToken c);
	Task<List<PlayerLeaderboardEntryWithRankDto>> GetPlayerLeaderboardEntriesWithRankAsync(Guid profileMemberId);
	Task<List<PlayerLeaderboardEntryWithRankDto>> GetProfileLeaderboardEntriesWithRankAsync(string profileId);
	string? GetCurrentIdentifier(LeaderboardType type);
}

[RegisterService<ILbService>(LifeTime.Scoped)]
public class LbService(
	ILeaderboardRegistrationService registrationService,
	ILogger<LbService> logger,
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
				.Where(e => e.LeaderboardId == lb.LeaderboardId && e.ProfileMemberId != null)
				.OrderByDescending(e => e.Score)
				.Skip(offset)
				.Take(limit)
				.Select(e => new LeaderboardEntryDto {
					Uuid = e.ProfileMember!.PlayerUuid,
					Profile = e.ProfileMember.ProfileName,
					Amount = (double) e.Score,
					Ign = e.ProfileMember.MinecraftAccount.Name
				}).ToListAsync();
		} 
		
		if (definition is IProfileLeaderboardDefinition) {
			return await context.LeaderboardEntries.AsNoTracking()
				.Where(e => e.LeaderboardId == lb.LeaderboardId && e.ProfileId != null)
				.Include(e => e.Profile)
				.OrderByDescending(e => e.Score)
				.Skip(offset)
				.Take(limit)
				.Select(e => new LeaderboardEntryDto {
					Uuid = e.Profile!.ProfileId,
					Profile = e.Profile!.ProfileName,
					Amount = (double) e.Score,
					Members = e.Profile.Members.Select(m => new ProfileLeaderboardMemberDto {
						Ign = m.MinecraftAccount.Name,
						Uuid = m.PlayerUuid,
						Xp = m.SkyblockXp
					}).OrderByDescending(s => s.Xp).ToList()
				}).ToListAsync();
		}
		
		return [];
	}

	public async Task UpdateMemberLeaderboardsAsync(ProfileMember member, CancellationToken c) {
		var time = DateTime.UtcNow;
		foreach (var leaderboard in registrationService.Leaderboards) {
			if (c.IsCancellationRequested) break;
			if (leaderboard is IMemberLeaderboardDefinition memberLb) {
				var score = memberLb.GetScoreFromMember(member);
				await UpdateMemberScore(memberLb, member, score, c);
			} else if (leaderboard is IProfileLeaderboardDefinition profileLb) {
				var score = profileLb.GetScoreFromProfile(member.Profile) 
		            ?? (member.Profile.Garden is not null
			            ? profileLb.GetScoreFromGarden(member.Profile.Garden)
			            : null);
				await UpdateProfileScore(profileLb, member.Profile, score, c);
			}
		}
		await context.SaveChangesAsync(c);
		logger.LogInformation("Updating member leaderboards for {Player} on {Profile} took {Time}", member.PlayerUuid, member.Profile.ProfileId, DateTime.UtcNow.Subtract(time));
	}

	public async Task UpdateProfileLeaderboardsAsync(EliteAPI.Models.Entities.Hypixel.Profile profile, CancellationToken c) {
		throw new NotImplementedException();
	}

	public async Task UpdateGardenLeaderboardsAsync(EliteAPI.Models.Entities.Hypixel.Garden member, CancellationToken c) {
		throw new NotImplementedException();
	}
	
	private async Task UpdateMemberScore(IMemberLeaderboardDefinition leaderboard, ProfileMember member, IConvertible? score, CancellationToken c) {
		foreach (var type in leaderboard.Info.IntervalType) {
			var slug = type switch {
				LeaderboardType.Current => leaderboard.Info.Slug,
				LeaderboardType.Monthly => $"{leaderboard.Info.Slug}-monthly",
				LeaderboardType.Weekly => $"{leaderboard.Info.Slug}-weekly",
				_ => throw new ArgumentOutOfRangeException(nameof(type))
			};
			var identifier = GetCurrentIdentifier(type);
			
			if (score is null) {
				// If score is null, we can assume the member has been removed or otherwise skip creating/updating an entry
				await context.LeaderboardEntries
					.Where(e => 
						e.Leaderboard.Slug == slug 
						&& e.ProfileMemberId == member.Id
						&& e.IsRemoved == false
						&& e.IntervalIdentifier == identifier)
					.ExecuteUpdateAsync(e => 
						e.SetProperty(l => l.IsRemoved, true),
						cancellationToken: c);
				continue;
			}
			
			var entry = await context.LeaderboardEntries
				.Include(e => e.Leaderboard)
				.FirstOrDefaultAsync(e => 
						e.Leaderboard.Slug == slug 
						&& e.ProfileMemberId == member.Id
						&& e.IntervalIdentifier == identifier, 
					cancellationToken: c);
		
			var entryScore = identifier is not null 
				? leaderboard.Info.UseIncreaseForInterval
					? score.ToDecimal(CultureInfo.InvariantCulture) - (entry?.InitialScore ?? 0)
					: score.ToDecimal(CultureInfo.InvariantCulture)
				: score.ToDecimal(CultureInfo.InvariantCulture);
			var initalScore = identifier is not null && leaderboard.Info.UseIncreaseForInterval
				? entry?.InitialScore ?? score.ToDecimal(CultureInfo.InvariantCulture)
				: 0;
			
			if (entry is null) {
				var lb = await context.Leaderboards
					.FirstOrDefaultAsync(lb => lb.Slug == slug, c);
				if (lb is null) return;
				
				entry = new Models.LeaderboardEntry() {
					LeaderboardId = lb.LeaderboardId,
					IntervalIdentifier = identifier,
					
					ProfileMemberId = member.Id,
				 
					InitialScore = initalScore,
					Score = entryScore,
				
					IsRemoved = member.WasRemoved,
					ProfileType = member.Profile?.GameMode
				};
				context.LeaderboardEntries.Add(entry);
			} else {
				entry.Score = entryScore;
				entry.IsRemoved = member.WasRemoved;
			}
			// TODO: Add leaderboard entry history
		}
	}
	
	private async Task UpdateProfileScore(IProfileLeaderboardDefinition leaderboard, EliteAPI.Models.Entities.Hypixel.Profile profile, IConvertible? score, CancellationToken c) {
		foreach (var type in leaderboard.Info.IntervalType) {
			var slug = type switch {
				LeaderboardType.Current => leaderboard.Info.Slug,
				LeaderboardType.Monthly => $"{leaderboard.Info.Slug}-monthly",
				LeaderboardType.Weekly => $"{leaderboard.Info.Slug}-weekly",
				_ => throw new ArgumentOutOfRangeException(nameof(type))
			};
			var identifier = GetCurrentIdentifier(type);
			
			if (score is null) {
				// If score is null, we can assume the member has been removed or otherwise skip creating/updating an entry
				await context.LeaderboardEntries
					.Where(e => 
						e.Leaderboard.Slug == slug 
						&& e.ProfileId == profile.ProfileId
						&& e.IsRemoved == false
						&& e.IntervalIdentifier == identifier)
					.ExecuteUpdateAsync(e => 
						e.SetProperty(l => l.IsRemoved, true),
						cancellationToken: c);
				continue;
			}
			
			var entry = await context.LeaderboardEntries
				.Include(e => e.Leaderboard)
				.FirstOrDefaultAsync(e => 
						e.Leaderboard.Slug == slug 
						&& e.ProfileId == profile.ProfileId
						&& e.IntervalIdentifier == identifier, 
					cancellationToken: c);
		
			var entryScore = identifier is not null 
				? leaderboard.Info.UseIncreaseForInterval
					? score.ToDecimal(CultureInfo.InvariantCulture) - (entry?.InitialScore ?? 0)
					: score.ToDecimal(CultureInfo.InvariantCulture)
				: score.ToDecimal(CultureInfo.InvariantCulture);
			var initalScore = identifier is not null && leaderboard.Info.UseIncreaseForInterval
				? entry?.InitialScore ?? score.ToDecimal(CultureInfo.InvariantCulture)
				: 0;
			
			if (entry is null) {
				var lb = await context.Leaderboards
					.FirstOrDefaultAsync(lb => lb.Slug == slug, c);
				if (lb is null) return;
				
				entry = new Models.LeaderboardEntry() {
					LeaderboardId = lb.LeaderboardId,
					IntervalIdentifier = identifier,
					
					ProfileId = profile.ProfileId,
				 
					InitialScore = initalScore,
					Score = entryScore,
				
					IsRemoved = profile.IsDeleted,
					ProfileType = profile.GameMode
				};
				context.LeaderboardEntries.Add(entry);
			} else {
				entry.Score = entryScore;
				entry.IsRemoved = profile.IsDeleted;
			}
			// TODO: Add leaderboard entry history
		}
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
        var playerEntries = await context.LeaderboardEntries
            .Where(le => le.ProfileMemberId == profileMemberId)
            .Select(le => new
            {
                le.LeaderboardId,
                le.IntervalIdentifier,
                le.Score,
                le.InitialScore,
                le.EntryTimestamp,
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
                Score = (double) entry.Score,
                InitialScore = (double) entry.InitialScore,
                // EntryTimestamp = entry.EntryTimestamp,
                // IsRemoved = entry.IsRemoved,
                Rank = rank,
                Title = entry.Leaderboard.Title,
	            Slug = entry.Leaderboard.Slug
            });
        }

        return results;
    }
	
	public async Task<List<PlayerLeaderboardEntryWithRankDto>> GetProfileLeaderboardEntriesWithRankAsync(string profileId)
	{
		var playerEntries = await context.LeaderboardEntries
			.Where(le => le.ProfileId == profileId)
			.Select(le => new
			{
				le.LeaderboardId,
				le.IntervalIdentifier,
				le.Score,
				le.InitialScore,
				le.EntryTimestamp,
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
				Score = (double) entry.Score,
				InitialScore = (double) entry.InitialScore,
				// EntryTimestamp = entry.EntryTimestamp,
				// IsRemoved = entry.IsRemoved,
				Rank = rank,
				Title = entry.Leaderboard.Title,
				Slug = entry.Leaderboard.Slug
			});
		}

		return results;
	}
}

public class PlayerLeaderboardEntryWithRankDto
{	
	public required string Title { get; set; }
	public required string Slug { get; set; }
	public int Rank { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? IntervalIdentifier { get; set; }
	public double Score { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double InitialScore { get; set; }
	// public DateTimeOffset EntryTimestamp { get; set; }
	// public bool IsRemoved { get; set; }
}
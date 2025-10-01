using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Features.Profiles;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Leaderboards.Models;

public class LeaderboardEntry
{
	public int LeaderboardEntryId { get; set; }
	public int LeaderboardId { get; set; }
	public Leaderboard Leaderboard { get; set; } = null!;

	/// <summary>
	/// Null for Current leaderboard, otherwise the interval identifier
	/// </summary>
	public string? IntervalIdentifier { get; set; }

	public string? ProfileId { get; set; }
	public EliteAPI.Models.Entities.Hypixel.Profile? Profile { get; set; }
	public Guid? ProfileMemberId { get; set; }
	public EliteAPI.Models.Entities.Hypixel.ProfileMember? ProfileMember { get; set; }

	public decimal InitialScore { get; set; }
	public decimal Score { get; set; }
	
	public bool IsRemoved { get; set; }
	/// <summary>
	/// Profile type to filter by, null for dominant "classic" profile type
	/// </summary>
	public string? ProfileType { get; set; }
}

public class LeaderboardEntryConfiguration : IEntityTypeConfiguration<LeaderboardEntry>
{
	public void Configure(EntityTypeBuilder<LeaderboardEntry> builder)
	{
		builder.HasKey(le => le.LeaderboardEntryId);
		
		builder.HasOne(le => le.Leaderboard)
			.WithMany()
			.HasForeignKey(le => le.LeaderboardId)
			.OnDelete(DeleteBehavior.Cascade);
		
		builder.Property(le => le.IntervalIdentifier).HasMaxLength(50);
		
		builder.Property(le => le.InitialScore)
			.IsRequired()
			.HasColumnType("decimal(24, 4)")
			.HasDefaultValue(0);
		
		builder.Property(le => le.Score)
			.IsRequired()
			.HasColumnType("decimal(24, 4)");
		
		builder.Property(le => le.IsRemoved)
			.IsRequired()
			.HasDefaultValue(false);
		
		builder.Property(le => le.ProfileType)
			.HasMaxLength(100);
		
		builder.HasIndex(le => new { le.LeaderboardId, le.IntervalIdentifier, le.Score })
			.IsDescending(false, false, true);
		
		builder.HasIndex(le => new { le.ProfileType, le.LeaderboardId, le.IntervalIdentifier });
		builder.HasIndex(le => le.IsRemoved);
		
		// Index specifically for getting all ranks of a player in a leaderboard
		builder.HasIndex(le => new { le.LeaderboardId, le.IsRemoved, le.IntervalIdentifier, le.Score })
			.IsDescending(false, false, false, true)
			.HasDatabaseName("IX_LeaderboardEntries_Ranks_Subquery");
		
		// Index specifically for getting all ranks of a player in a leaderboard, without interval
		builder.HasIndex(le => new { le.LeaderboardId, le.IsRemoved, le.Score })
			.HasFilter(@"""IntervalIdentifier"" IS NULL")
			.IsDescending(false, false, true)
			.HasDatabaseName("IX_LeaderboardEntries_Rank_Subquery_AllTime");
		
		builder.HasIndex(le => le.ProfileId);
		builder.HasIndex(le => le.ProfileMemberId);
	}
}

public static class LeaderboardEntryExtensions {
	
	public static IQueryable<LeaderboardEntry> FromLeaderboard(this IQueryable<LeaderboardEntry> query, int leaderboardId, bool? memberLeaderboard = null) {
		if (memberLeaderboard is null) {
			return query.Where(e => e.LeaderboardId == leaderboardId);
		}
		return memberLeaderboard is true
			? query.Where(e => e.LeaderboardId == leaderboardId && e.ProfileMemberId != null) 
			: query.Where(e => e.LeaderboardId == leaderboardId && e.ProfileId != null);
	}
	
	public static IQueryable<LeaderboardEntry> EntryFilter(this IQueryable<LeaderboardEntry> query, string? interval = null, RemovedFilter? removedFilter = RemovedFilter.NotRemoved, string? gameMode = null) {
		if (interval is not null) {
			query = query.Where(e => e.IntervalIdentifier == interval);
		} else {
			var monthlyInterval = LbService.GetCurrentIdentifier(LeaderboardType.Monthly);
			var weeklyInterval = LbService.GetCurrentIdentifier(LeaderboardType.Weekly);
			query = query.Where(e => 
					e.IntervalIdentifier == null 
	             || e.IntervalIdentifier == monthlyInterval 
	             || e.IntervalIdentifier == weeklyInterval);
		}
		
		query = removedFilter switch {
			RemovedFilter.NotRemoved => query.Where(e => !e.IsRemoved),
			RemovedFilter.Removed => query.Where(e => e.IsRemoved),
			_ => query
		};

		query = gameMode switch {
			"classic" => query.Where(e => e.ProfileType == null),
			"ironman" => query.Where(e => e.ProfileType == "ironman"),
			"island" => query.Where(e => e.ProfileType == "island"),
			_ => query
		};
		
		query = query.Where(e => e.Score > 0);
		
		return query;
	}

	public static IQueryable<LeaderboardEntryDto> MapToProfileLeaderboardEntries(this IQueryable<LeaderboardEntry> query, RemovedFilter removedFilter = RemovedFilter.NotRemoved) {
		return query
			.Include(e => e.Profile)
			.Select(e => new LeaderboardEntryDto {
				Uuid = e.Profile!.ProfileId,
				Profile = e.Profile!.ProfileName,
				Amount = (double)e.Score,
				InitialAmount = (double)e.InitialScore,
				Removed = e.IsRemoved,
				Mode = e.ProfileType,
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
			});
	}
	
	public static IQueryable<LeaderboardEntryDto> MapToMemberLeaderboardEntries(this IQueryable<LeaderboardEntry> query, bool includeMeta = false) {
		if (includeMeta)
		{
			query = query.Include(a => a.ProfileMember!.MinecraftAccount!.EliteAccount!.UserSettings);
		}
		return query.Select(e => new LeaderboardEntryDto {
			Uuid = e.ProfileMember!.PlayerUuid,
			Profile = e.ProfileMember.ProfileName,
			Amount = (double)e.Score,
			InitialAmount = (double)e.InitialScore,
			Removed = e.IsRemoved,
			Mode = e.ProfileType,
			Ign = e.ProfileMember.MinecraftAccount.Name,
			Meta = includeMeta ? e.ProfileMember.GetCosmeticsDto() : null
		});
	}
}
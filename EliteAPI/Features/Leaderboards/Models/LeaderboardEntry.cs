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
	public Guid? ProfileMemberId { get; set; }
	
	public decimal InitialScore { get; set; }
	public decimal Score { get; set; }
	
	public bool IsRemoved { get; set; }
	/// <summary>
	/// Profile type to filter by, null for dominant "classic" profile type
	/// </summary>
	public string? ProfileType { get; set; }

	public DateTimeOffset EntryTimestamp { get; set; } = DateTimeOffset.UtcNow;
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
		
		builder.Property(le => le.EntryTimestamp)
			.HasDefaultValueSql("now()");

		builder.HasIndex(le => new { le.LeaderboardId, le.IntervalIdentifier, le.Score })
			.IsDescending(false, false, true);
		
		builder.HasIndex(le => new { le.ProfileType, le.LeaderboardId, le.IntervalIdentifier });
		builder.HasIndex(le => le.IsRemoved);
		
		builder.HasIndex(le => le.ProfileId);
		builder.HasIndex(le => le.ProfileMemberId);
		
		builder.ToTable(table => table
			.HasCheckConstraint(
				"CK_LeaderboardEntries_ProfileOrMember", 
				"((\"ProfileId\" IS NOT NULL AND \"ProfileMemberId\" IS NULL) OR (\"ProfileId\" IS NULL AND \"ProfileMemberId\" IS NOT NULL))"
			)
		);
	}
}
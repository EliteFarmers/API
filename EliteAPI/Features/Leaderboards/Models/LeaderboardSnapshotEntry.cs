using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Leaderboards.Models;

public class LeaderboardSnapshotEntry {
	public int LeaderboardSnapshotEntryId { get; set; }
	public int LeaderboardSnapshotId { get; set; }
	public LeaderboardSnapshot LeaderboardSnapshot { get; set; } = null!;

	/// <summary>
	/// Null for current leaderboards, otherwise the identifier of the interval
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

public class LeaderboardSnapshotEntryConfiguration : IEntityTypeConfiguration<LeaderboardSnapshotEntry> {
	public void Configure(EntityTypeBuilder<LeaderboardSnapshotEntry> builder) {
		builder.ToTable("LeaderboardSnapshotEntries");
		builder.HasKey(lse => lse.LeaderboardSnapshotEntryId);
		builder.HasOne(lse => lse.LeaderboardSnapshot)
			.WithMany()
			.HasForeignKey(lse => lse.LeaderboardSnapshotId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(le => le.Score)
			.IsRequired()
			.HasColumnType("decimal(24, 4)");

		builder.Property(lse => lse.ProfileType).HasMaxLength(100);

		builder.HasIndex(lse => new { lse.LeaderboardSnapshotId, lse.Score });
		builder.HasIndex(lse => new { lse.ProfileType, lse.LeaderboardSnapshotId });

		builder.ToTable(table => table
			.HasCheckConstraint(
				"CK_LeaderboardSnapshotEntries_ProfileOrMember",
				"((\"ProfileId\" IS NOT NULL AND \"ProfileMemberId\" IS NULL) OR (\"ProfileId\" IS NULL AND \"ProfileMemberId\" IS NOT NULL))"
			)
		);
	}
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Leaderboards.Models;

public class LeaderboardSnapshot
{
	public int LeaderboardSnapshotId { get; set; }
	public int LeaderboardId { get; set; }
	public Leaderboard Leaderboard { get; set; } = null!;

	public DateTimeOffset SnapshotTimestamp { get; set; } = DateTimeOffset.UtcNow;
	public string? IntervalIdentifier { get; set; }
}

public class LeaderboardSnapshotConfiguration : IEntityTypeConfiguration<LeaderboardSnapshot>
{
	public void Configure(EntityTypeBuilder<LeaderboardSnapshot> builder)
	{
		builder.HasKey(ld => ld.LeaderboardSnapshotId);
		
		builder.HasOne(ls => ls.Leaderboard)
			.WithMany()
			.HasForeignKey(ls => ls.LeaderboardId)
			.OnDelete(DeleteBehavior.Cascade);
		
		builder.Property(ls => ls.SnapshotTimestamp).IsRequired();
		
		builder.Property(ls => ls.IntervalIdentifier).IsRequired().HasMaxLength(50);
		
		builder.HasIndex(
			ls => new { ls.LeaderboardId, ls.SnapshotTimestamp, ls.IntervalIdentifier }, 
			name: "IX_LeaderboardSnapshots_Definition_Timestamp_Interval")
			.IsUnique();
		
		builder.HasIndex(ls => new { ls.LeaderboardId, ls.IntervalIdentifier });
	}
}
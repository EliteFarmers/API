using EliteAPI.Models.Entities.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Leaderboards.Models;

public class Leaderboard {
	public int LeaderboardId { get; set; }
	public required string Slug { get; set; }
	public LeaderboardType IntervalType { get; set; } = LeaderboardType.Current;
	public LeaderboardEntryType EntryType { get; set; } = LeaderboardEntryType.Member;
	public LeaderboardScoreDataType ScoreDataType { get; set; } = LeaderboardScoreDataType.Double;
	
	public string? IconId { get; set; }
	public Image? Icon { get; set; }
	
	public required string Title { get; set; }
	public string? ShortTitle { get; set; }
	
	/// <summary>
	/// Property to use for seeding the leaderboard
	/// </summary>
	public string? Property { get; set; }
	
	public DateOnly? StartDate { get; set; }
	public DateOnly? EndDate { get; set; }
}

public enum LeaderboardType {
	/// <summary>
	/// Default leaderboard type, shows current scores
	/// </summary>
	Current,
	/// <summary>
	/// Weekly leaderboard type, shows score increases during the week
	/// </summary>
	Weekly,
	/// <summary>
	/// Monthly leaderboard type, shows score increases during the month
	/// </summary>
	Monthly
}

public enum LeaderboardEntryType
{
	Member,
	Profile,
}

public enum LeaderboardScoreDataType
{
	Double,
	Long,
	Decimal
}

public class LeaderboardConfiguration : IEntityTypeConfiguration<Leaderboard>
{
	public void Configure(EntityTypeBuilder<Leaderboard> builder)
	{
		builder.HasKey(ld => ld.LeaderboardId);
		builder.Property(ld => ld.Title).IsRequired().HasMaxLength(200);
		builder.Property(ld => ld.IntervalType).IsRequired().HasConversion<string>().HasMaxLength(50);
		builder.Property(ld => ld.EntryType).IsRequired().HasConversion<string>().HasMaxLength(50);
		builder.Property(ld => ld.ScoreDataType).IsRequired().HasConversion<string>().HasMaxLength(50);
		
		builder.HasOne(ld => ld.Icon).WithMany().HasForeignKey(ld => ld.IconId);
	}
}

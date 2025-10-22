using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Leaderboards.Models;

public class LeaderboardRanksQueryResult
{
	public string? IntervalIdentifier { get; set; }
	public decimal Score { get; set; }
	public decimal InitialScore { get; set; }
	public long Rank { get; set; }
	public required string Title { get; set; }
	public required string Slug { get; set; }
	public required string ShortTitle { get; set; }
	public required string ScoreDataType { get; set; }
}

public class LeaderboardRanksQueryResultConfiguration : IEntityTypeConfiguration<LeaderboardRanksQueryResult>
{
	public void Configure(EntityTypeBuilder<LeaderboardRanksQueryResult> builder) {
		builder.HasNoKey();
		builder.ToTable("LeaderboardRanksQueryResult", t => t.ExcludeFromMigrations());
	}
}
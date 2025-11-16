using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuildLeaderboardResult
{
	public required string Id { get; set; }
	public required string Name { get; set; }

	public long CreatedAt { get; set; }
	
	public string? Tag { get; set; }
	public string? TagColor { get; set; }

	public int MemberCount { get; set; }
	
	public long LastUpdated { get; set; }
	
	/// <summary>
	/// Populated when sorting guilds by a specific collection or skill
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double Amount { get; set; }
}

public class HypixelGuildLeaderboardResultEntityConfiguration : IEntityTypeConfiguration<HypixelGuildLeaderboardResult>
{
	public void Configure(EntityTypeBuilder<HypixelGuildLeaderboardResult> builder) {
		builder.HasNoKey();
		builder.ToTable("HypixelGuildLeaderboardResult", t => t.ExcludeFromMigrations());
	}
}
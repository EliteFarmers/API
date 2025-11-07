using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuildMemberExp
{
	public long GuildMemberId { get; set; }
	public DateOnly Day { get; set; }
	public int Xp { get; set; }
}

public class HypixelGuildMemberExpEntityConfiguration : IEntityTypeConfiguration<HypixelGuildMemberExp>
{
	public void Configure(EntityTypeBuilder<HypixelGuildMemberExp> builder) {
		builder.HasKey(x => new { x.GuildMemberId, x.Day });
		builder.HasIndex(x => x.GuildMemberId);
		builder.HasIndex(x => x.Day).IsDescending();
	}
}
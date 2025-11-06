using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuildMember
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long Id { get; set; }
	
	public required string GuildId { get; set; }
	public HypixelGuild Guild { get; set; } = null!;

	public required string PlayerUuid { get; set; }
	public MinecraftAccount MinecraftAccount { get; set; } = null!;

	public string? Rank { get; set; }

	public long JoinedAt { get; set; }
	public int QuestParticipation { get; set; }
	
	public bool Active { get; set; }

	public List<HypixelGuildMemberExp> ExpHistory { get; set; } = [];
}

public class HypixelGuildMemberEntityConfiguration : IEntityTypeConfiguration<HypixelGuildMember>
{
	public void Configure(EntityTypeBuilder<HypixelGuildMember> builder) {
		builder.HasOne(x => x.MinecraftAccount)
			.WithMany()
			.HasForeignKey(x => x.PlayerUuid);
		
		builder.HasOne(x => x.Guild)
			.WithMany(x => x.Members)
			.HasForeignKey(x => x.GuildId);
		
		builder.HasMany(x => x.ExpHistory)
			.WithOne()
			.HasForeignKey(x => x.GuildMemberId);
	}
}
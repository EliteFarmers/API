using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuildStats
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[MapperIgnore]
	public long Id { get; set; }
	[MapperIgnore]
	public required string GuildId { get; set; }
	public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
	
	public int MemberCount { get; set; }
	
	public HypixelGuildStat HypixelLevel { get; set; } = new();
	public HypixelGuildStat SkyblockExperience { get; set; } = new();
	public HypixelGuildStat SkillLevel { get; set; } = new();
	public HypixelGuildStat SlayerExperience { get; set; } = new();
	public HypixelGuildStat CatacombsExperience { get; set; } = new();
	public HypixelGuildStat FarmingWeight { get; set; } = new();
	public HypixelGuildStat Networth { get; set; } = new();
	
	[Column(TypeName = "jsonb")]
	public Dictionary<string, long> Collections { get; set; } = new();
	
	[Column(TypeName = "jsonb")]
	public Dictionary<string, long> Skills { get; set; } = new();
}

[Owned]
public class HypixelGuildStat
{
	public double Total { get; set; }
	public double Average { get; set; }
}

public class HypixelGuildStatsEntityConfiguration : IEntityTypeConfiguration<HypixelGuildStats>
{
	public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<HypixelGuildStats> builder)
	{
		builder.HasIndex(x => x.RecordedAt);
		
		builder.HasOne<HypixelGuild>()
			.WithMany(x => x.Stats)
			.HasForeignKey(x => x.GuildId)
			.OnDelete(DeleteBehavior.Cascade);
		
		builder.OwnsOne(x => x.SkyblockExperience, o => {
			o.HasIndex(x => x.Average);
			o.HasIndex(x => x.Total);
		});
		
		builder.OwnsOne(x => x.SkillLevel, o => {
			o.HasIndex(x => x.Average);
			o.HasIndex(x => x.Total);
		});
		
		builder.OwnsOne(x => x.Networth, o => {
			o.HasIndex(x => x.Average);
			o.HasIndex(x => x.Total);
		});
		
		builder.OwnsOne(x => x.FarmingWeight, o => {
			o.HasIndex(x => x.Total);
		});
		
		builder.OwnsOne(x => x.CatacombsExperience, o => {
			o.HasIndex(x => x.Total);
		});
		
		builder.OwnsOne(x => x.SlayerExperience, o => {
			o.HasIndex(x => x.Total);
		});
		
		builder.OwnsOne(x => x.HypixelLevel, o => {
			o.HasIndex(x => x.Total);
		});
	}
}

[JsonStringEnum]
public enum SortHypixelGuildsBy
{
	MemberCount,
	SkyblockExperience,
	SkyblockExperienceAverage,
	SkillLevel,
	SkillLevelAverage,
	HypixelLevelAverage,
	SlayerExperience,
	CatacombsExperience,
	FarmingWeight,
	Networth,
	NetworthAverage
}
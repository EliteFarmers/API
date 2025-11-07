using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuild
{
	[Key]
	public required string Id { get; set; }
	public required string Name { get; set; }
	public required string NameLower { get; set; }

	public long CreatedAt { get; set; }
	public string Description { get; set; }
	
	[Column(TypeName = "jsonb")]
	public List<string>? PreferredGames { get; set; }
	
	/// <summary>
	/// Publicaly listed in Hypixel
	/// </summary>
	public bool PubliclyListed { get; set; }
	
	/// <summary>
	/// Public for Elite purposes
	/// </summary>
	public bool Public { get; set; }
	
	public long Exp { get; set; }
	
	public string? Tag { get; set; }
	public string? TagColor { get; set; }

	[Column(TypeName = "jsonb")] public Dictionary<string, long> GameExp { get; set; } = new();
	[Column(TypeName = "jsonb")] public List<RawHypixelGuildRank> Ranks { get; set; } = [];

	public List<HypixelGuildMember> Members { get; set; } = [];
	
	public long LastUpdated { get; set; }
}

public class HypixelGuildEntityConfiguration : IEntityTypeConfiguration<HypixelGuild>
{
	public void Configure(EntityTypeBuilder<HypixelGuild> builder) {
		builder.HasKey(x => x.Id);
		builder.HasIndex(x => x.NameLower);
	}
}
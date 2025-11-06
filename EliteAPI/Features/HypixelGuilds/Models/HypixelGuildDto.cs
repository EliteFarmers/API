using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.HypixelGuilds.Models;

[Mapper]
public static partial class HypixelGuildMapper
{
	public static partial HypixelGuildDto ToDto(this HypixelGuild guild);

	public static HypixelGuildMemberDto ToDto(this HypixelGuildMember guildMember) {
		var prefix = guildMember.MinecraftAccount.EliteAccount?.UserSettings.Prefix ?? string.Empty;
		var suffix = guildMember.MinecraftAccount.EliteAccount?.UserSettings.Suffix ?? string.Empty;

		return new HypixelGuildMemberDto() {
			PlayerUuid = guildMember.PlayerUuid,
			Name = guildMember.MinecraftAccount.Name,
			FormattedName = $"{prefix} {guildMember.MinecraftAccount.Name} {suffix}".Trim(),
			Rank = guildMember.Rank,
			JoinedAt = guildMember.JoinedAt,
			Active = guildMember.Active,
			ExpHistory = guildMember.ExpHistory.ToDto()
		};
	}

	public static Dictionary<string, int> ToDto(this List<HypixelGuildMemberExp> xp) {
		return xp
			.OrderByDescending(x => x.Day)
			.ToDictionary(x => $"{x.Day.Year}-{x.Day.Month}-{x.Day.Day}", x => x.Xp);
	}
}

public class HypixelGuildDto
{
	public required string Id { get; set; }
	public required string Name { get; set; }

	public long CreatedAt { get; set; }
	public string Description { get; set; }
	
	public List<string>? PreferredGames { get; set; }
	
	public bool PubliclyListed { get; set; }
	public bool Public { get; set; }
	
	public long Exp { get; set; }
	
	public string? Tag { get; set; }
	public string? TagColor { get; set; }

	public Dictionary<string, long> GameExp { get; set; } = new();
	public List<RawHypixelGuildRank> Ranks { get; set; } = [];

	public List<HypixelGuildMemberDto> Members { get; set; } = [];
	
	public long LastUpdated { get; set; }
}
using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.DTOs;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.HypixelGuilds.Models;

[Mapper]
public static partial class HypixelGuildMapper
{
	public static partial HypixelGuildDto ToDto(this HypixelGuild guild);

	public static partial IQueryable<HypixelGuildDetailsDto> SelectDetailsDto(this IQueryable<HypixelGuild> guild);
	public static HypixelGuildDetailsDto ToDetailsDto(this HypixelGuild guild) {
		return new HypixelGuildDetailsDto() {
			Id = guild.Id,
			Name = guild.Name,
			CreatedAt = guild.CreatedAt,
			Tag = guild.Tag,
			TagColor = guild.TagColor,
			MemberCount = guild.MemberCount,
			LastUpdated = guild.LastUpdated,
			Stats = guild.Stats.OrderByDescending(x => x.RecordedAt).Take(1).Select(x => x.ToDto()).FirstOrDefault()
		};
	}
	
	[MapperIgnoreTarget(nameof(HypixelGuildDetailsDto.Stats))]
	public static partial HypixelGuildDetailsDto ToDto(this HypixelGuildLeaderboardResult guild);
	public static partial IQueryable<HypixelGuildStatsDto> SelectDto(this IQueryable<HypixelGuildStats> guild);
	
	[MapperIgnoreSource(nameof(HypixelGuildStats.Collections))]
	[MapperIgnoreSource(nameof(HypixelGuildStats.Skills))]
	public static partial HypixelGuildStatsDto ToDto(this HypixelGuildStats guild);
	public static partial HypixelGuildStatsFullDto ToFullDto(this HypixelGuildStats guild);

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

	public static HypixelGuildMemberDetailsDto ToDetailsDto(this HypixelGuildMember guildMember) {
		return new HypixelGuildMemberDetailsDto() {
			Guild = ((HypixelGuild?)guildMember.Guild)?.ToDetailsDto(),
			Rank = guildMember.Rank,
			JoinedAt = guildMember.JoinedAt,
			QuestParticipation = guildMember.QuestParticipation,
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
	public int MemberCount { get; set; }
	public long LastUpdated { get; set; }
	
	public List<HypixelGuildStatsFullDto> Stats { get; set; } = [];
}

public class HypixelGuildDetailsDto
{
	public required string Id { get; set; }
	public required string Name { get; set; }

	public long CreatedAt { get; set; }
	
	public string? Tag { get; set; }
	public string? TagColor { get; set; }

	public int MemberCount { get; set; }
	
	public long LastUpdated { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public HypixelGuildStatsDto? Stats { get; set; }
	
	/// <summary>
	/// Populated when sorting guilds by a specific collection or skill
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double Amount { get; set; }
}
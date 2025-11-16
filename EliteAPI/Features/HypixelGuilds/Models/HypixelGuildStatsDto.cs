using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuildStatsDto
{
	public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
	
	public int MemberCount { get; set; }
	public HypixelGuildStat HypixelLevel { get; set; } = new();
	public HypixelGuildStat SkyblockExperience { get; set; } = new();
	public HypixelGuildStat SkillLevel { get; set; } = new();
	public HypixelGuildStat SlayerExperience { get; set; } = new();
	public HypixelGuildStat CatacombsExperience { get; set; } = new();
	public HypixelGuildStat FarmingWeight { get; set; } = new();
	public HypixelGuildStat Networth { get; set; } = new();
}

public class HypixelGuildStatsFullDto : HypixelGuildStatsDto
{
	public Dictionary<string, long> Collections { get; set; } = new();
	public Dictionary<string, long> Skills { get; set; } = new();
}
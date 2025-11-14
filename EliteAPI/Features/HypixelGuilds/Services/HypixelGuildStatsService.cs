using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.HypixelGuilds.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Features.HypixelGuilds.Services;

public interface IHypixelGuildStatsService
{
	Task UpdateGuildStats(string guildId, CancellationToken ct);
}

[RegisterService<IHypixelGuildStatsService>(LifeTime.Scoped)]
public class HypixelGuildStatsService(
	DataContext context
	) : IHypixelGuildStatsService
{
	/// <summary>
	/// Updates the stats for a given guild
	/// </summary>
	/// <param name="guildId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task UpdateGuildStats(string guildId, CancellationToken ct) {
		var guild = await context.HypixelGuilds
			.Include(g => g.Members.Where(m => m.Active))
			.ThenInclude(m => m.MinecraftAccount)
			.ThenInclude(m => m.PlayerData)
			.Select(g => new {
				g.Id,
				g.MemberCount,
				Members = g.Members.Select(m => new {
					m.PlayerUuid, 
					HypixelLevel = m.MinecraftAccount.PlayerData != null ? m.MinecraftAccount.PlayerData.NetworkExp : 0
				}).ToList(),
			})
			.FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken: ct);
		
		if (guild is null) {
			return;
		}
		
		var playerUuids = guild.Members.Select(m => m.PlayerUuid).ToList();
		
		var newStats = new HypixelGuildStats {
			GuildId = guild.Id,
			RecordedAt = DateTimeOffset.UtcNow,
			MemberCount = guild.MemberCount,
			HypixelLevel = new HypixelGuildStat() {
				Total = guild.Members.Sum(m => m.HypixelLevel),
				Average = guild.Members.Count > 0
					? guild.Members.Average(m => m.HypixelLevel)
					: 0
			}
		};
		
		/*
		 * 	public HypixelGuildStat HypixelLevel { get; set; } = new();
	public HypixelGuildStat SkyblockExperience { get; set; } = new();
	public HypixelGuildStat SkillLevel { get; set; } = new();
	public HypixelGuildStat SlayerExperience { get; set; } = new();
	public HypixelGuildStat CatacombsExperience { get; set; } = new();
	public HypixelGuildStat FarmingWeight { get; set; } = new();
	public HypixelGuildStat Networth { get; set; } = new();
		 */
		
		var memberStats = await context.ProfileMembers
			.Include(p => p.Farming)
			.Include(p => p.Skills)
			.Where(p => playerUuids.Contains(p.PlayerUuid))
			.Select(p => new {
				SkyblockXp = p.SkyblockXp,
				SkillsEnabled = p.Api.Skills,
				SkillXp = p.Skills.Alchemy + p.Skills.Carpentry + p.Skills.Combat + p.Skills.Enchanting + p.Skills.Farming +
				          p.Skills.Fishing + p.Skills.Foraging + p.Skills.Mining + p.Skills.Taming,
				SlayerXp = p.Slayers != null ? p.Slayers.Xp : 0,
				CataXp = p.Unparsed.Dungeons.DungeonTypes != null ? p.Unparsed.Dungeons.DungeonTypes.Catacombs.Experience : 0,
				CollectionsEnabled = p.Api.Collections,
				FarmingWeight = p.Farming.TotalWeight,
			})
			.ToListAsync(ct);
		
		newStats.FarmingWeight.Total = memberStats.Sum(m => m.FarmingWeight);
		newStats.FarmingWeight.Average = memberStats.Count > 0 
			? memberStats.Where(m => m.CollectionsEnabled).Average(m => m.FarmingWeight)
			: 0;
		newStats.SkyblockExperience.Total = memberStats.Sum(m => m.SkyblockXp);
		newStats.SkyblockExperience.Average = memberStats.Average(m => m.SkyblockXp);
		// newStats.SkillLevel.Total = memberStats.Sum(m => m.SkillXp);
		// newStats.SkillLevel.Average = memberStats.Count > 0
		// 	? memberStats.Where(m => m.SkillsEnabled).Average(m => m.SkillXp)
		// 	: 0;
		newStats.CatacombsExperience.Total = memberStats.Sum(m => m.CataXp);
		newStats.CatacombsExperience.Average = memberStats.Average(m => m.CataXp);
		newStats.SlayerExperience.Total = memberStats.Sum(m => m.SlayerXp);
		newStats.SlayerExperience.Average = memberStats.Average(m => m.SlayerXp);
		
		context.HypixelGuildStats.Add(newStats);
		await context.SaveChangesAsync(ct);

		return;
	}
}
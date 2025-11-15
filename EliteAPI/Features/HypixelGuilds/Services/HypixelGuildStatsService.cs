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
					? guild.Members.Where(m => m.HypixelLevel > 0).Average(m => m.HypixelLevel)
					: 0
			}
		};
		
		var memberStats = await context.ProfileMembers
			.Include(p => p.Farming)
			.Include(p => p.Skills)
			.Where(p => playerUuids.Contains(p.PlayerUuid))
			.Select(p => new {
				SkyblockXp = p.SkyblockXp,
				SkillsEnabled = p.Api.Skills,
				SlayerXp = p.Slayers != null ? p.Slayers.Xp : 0,
				CataXp = p.Unparsed.Dungeons.DungeonTypes != null ? p.Unparsed.Dungeons.DungeonTypes.Catacombs.Experience : 0,
				Collections = p.Collections,
				Skills = p.Skills,
				CollectionsEnabled = p.Api.Collections,
				FarmingWeight = p.Farming.TotalWeight,
			})
			.ToListAsync(ct);
		
		newStats.FarmingWeight.Total = memberStats.Sum(m => m.FarmingWeight);
		newStats.FarmingWeight.Average = memberStats.Count > 0 
			? memberStats.Where(m => m.CollectionsEnabled && m.FarmingWeight > 0).Average(m => m.FarmingWeight)
			: 0;
		newStats.SkyblockExperience.Total = memberStats.Sum(m => m.SkyblockXp);
		newStats.SkyblockExperience.Average = memberStats.Where(m => m.SkyblockXp > 0)
			.Average(m => m.SkyblockXp);
		// newStats.SkillLevel.Total = memberStats.Sum(m => m.SkillXp);
		// newStats.SkillLevel.Average = memberStats.Count > 0
		// 	? memberStats.Where(m => m.SkillsEnabled).Average(m => m.SkillXp)
		// 	: 0;
		newStats.CatacombsExperience.Total = memberStats.Sum(m => m.CataXp);
		newStats.CatacombsExperience.Average = memberStats.Where(m => m.CataXp > 0).Average(m => m.CataXp);
		newStats.SlayerExperience.Total = memberStats.Sum(m => m.SlayerXp);
		newStats.SlayerExperience.Average = memberStats.Where(m => m.SlayerXp > 0).Average(m => m.SlayerXp);
		
		var collections = new Dictionary<string, long>();
		var skills = new Dictionary<string, long>() {
			{ "farming", 0 },
			{ "mining", 0 },
			{ "combat", 0 },
			{ "foraging", 0 },
			{ "fishing", 0 },
			{ "enchanting", 0 },
			{ "alchemy", 0 },
			{ "carpentry", 0 },
			{ "runecrafting", 0 },
			{ "taming", 0 },
			{ "social", 0 },
		};

		foreach (var member in memberStats) {
			if (member.CollectionsEnabled) {
				foreach (var (key, value) in member.Collections) {
					if (!collections.TryAdd(key, value)) {
						collections[key] += value;
					}
				}
			}

			if (!member.SkillsEnabled) continue;
			
			skills["farming"] += (long) member.Skills.Farming;
			skills["mining"] += (long) member.Skills.Mining;
			skills["combat"] += (long) member.Skills.Combat;
			skills["foraging"] += (long) member.Skills.Foraging;
			skills["fishing"] += (long) member.Skills.Fishing;
			skills["enchanting"] += (long) member.Skills.Enchanting;
			skills["alchemy"] += (long) member.Skills.Alchemy;
			skills["carpentry"] += (long) member.Skills.Carpentry;
			skills["runecrafting"] += (long) member.Skills.Runecrafting;
			skills["taming"] += (long) member.Skills.Taming;
			skills["social"] += (long) member.Skills.Social;
		}
		
		newStats.Collections = collections;
		newStats.Skills = skills;
		
		context.HypixelGuildStats.Add(newStats);
		await context.SaveChangesAsync(ct);

		return;
	}
}
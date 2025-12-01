using EliteAPI.Data;
using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Parsers.Profiles;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

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
					HypixelLevelXp = m.MinecraftAccount.PlayerData != null ? m.MinecraftAccount.PlayerData.NetworkExp : 0,
					HypixelLevel = m.MinecraftAccount.PlayerData != null ? m.MinecraftAccount.PlayerData.GetNetworkLevel() : 0
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
				Total = guild.Members.Sum(m => m.HypixelLevelXp),
				Average = guild.Members.Count > 0
					? guild.Members.Where(m => m.HypixelLevel > 0).Average(m => m.HypixelLevel)
					: 0
			}
		};
		
		var memberStats = await context.ProfileMembers
			.Include(p => p.Farming)
			.Include(p => p.Skills)
			.Where(p => playerUuids.Contains(p.PlayerUuid) && !p.WasRemoved)
			.Select(p => new {
				PlayerUuid = p.PlayerUuid,
				Selected = p.IsSelected,
				SkyblockXp = p.SkyblockXp,
				Networth = p.Networth,
				SkillsEnabled = p.Api.Skills,
				SlayerXp = p.Slayers != null ? p.Slayers.Xp : 0,
				CataXp = p.Unparsed.Dungeons.DungeonTypes != null ? p.Unparsed.Dungeons.DungeonTypes.Catacombs.Experience : 0,
				Collections = p.Collections,
				Skills = p.Skills,
				CollectionsEnabled = p.Api.Collections,
				FarmingWeight = p.Farming.TotalWeight,
			})
			.OrderByDescending(p => p.Selected)
			.GroupBy(p => p.PlayerUuid)
			.ToListAsync(ct);
		
		newStats.FarmingWeight.Total = memberStats
			.Sum(m => m.Sum(p => p.FarmingWeight));
		newStats.FarmingWeight.Average = memberStats.Count > 0 
			? memberStats.Select(g => g.Where(m => m.CollectionsEnabled && m.FarmingWeight > 0)
					.OrderByDescending(m => m.FarmingWeight)
					.Select(m => m.FarmingWeight)
					.Take(1)
					.FirstOrDefault())
				.Where(m => m > 0)
				.DefaultIfEmpty()
				.Average()
			: 0;
		newStats.SkyblockExperience.Total = memberStats
			.Sum(m => m.Sum(p => p.SkyblockXp));
		newStats.SkyblockExperience.Average = memberStats.Count > 0 
			? memberStats.Select(g => g.Where(m => m.SkyblockXp > 0)
					.OrderByDescending(m => m.SkyblockXp)
					.Select(m => m.SkyblockXp)
					.Take(1)
					.FirstOrDefault())
				.Where(m => m > 0)
				.DefaultIfEmpty()
				.Average()
			: 0;
		newStats.SkillLevel.Total = memberStats
			.Sum(m => m.Sum(p => p.SkillsEnabled ? p.Skills.GetStandardSkillsXp() : 0));
		newStats.SkillLevel.Average = memberStats.Count > 0 
			? memberStats.Select(g => g.Where(m => m.SkillsEnabled)
					.Select(m => m.Skills.GetAverageSkillLevel())
					.OrderByDescending(m => m)
					.Take(1)
					.FirstOrDefault())
				.Where(m => m > 0)
				.DefaultIfEmpty()
				.Average()
			: 0;
		newStats.CatacombsExperience.Total = memberStats
			.Sum(m => m.Sum(p => p.CataXp));
		newStats.CatacombsExperience.Average = memberStats.Count > 0 
			? memberStats.Select(g => g.Where(m => m.CataXp > 0)
					.OrderByDescending(m => m.CataXp)
					.Select(m => SkillParser.GetDungeoneeringLevel(m.CataXp))
					.Take(1)
					.FirstOrDefault())
				.Where(m => m > 0)
				.DefaultIfEmpty()
				.Average()
			: 0;
		newStats.SlayerExperience.Total = memberStats
			.Sum(m => m.Sum(p => p.SlayerXp));
		newStats.SlayerExperience.Average = memberStats.Count > 0 
			? memberStats.Select(g => g.Where(m => m.SlayerXp > 0)
					.OrderByDescending(m => m.SlayerXp)
					.Select(m => m.SlayerXp)
					.Take(1)
					.FirstOrDefault())
				.Where(m => m > 0)
				.DefaultIfEmpty()
				.Average()
			: 0;
		newStats.Networth.Total = memberStats
			.Sum(m => m.Sum(p => p.Networth));
		newStats.Networth.Average = memberStats.Count > 0 
			? memberStats.Select(g => g.Where(m => m.Networth > 0)
					.OrderByDescending(m => m.Networth)
					.Select(m => m.Networth)
					.Take(1)
					.FirstOrDefault())
				.Where(m => m > 0)
				.DefaultIfEmpty()
				.Average()
			: 0;
		
		var collections = new Dictionary<string, long>();
		var skills = new Dictionary<string, long>() {
			{ SkillName.Farming, 0 },
			{ SkillName.Mining, 0 },
			{ SkillName.Combat, 0 },
			{ SkillName.Foraging, 0 },
			{ SkillName.Fishing, 0 },
			{ SkillName.Enchanting, 0 },
			{ SkillName.Alchemy, 0 },
			{ SkillName.Carpentry, 0 },
			{ SkillName.Runecrafting, 0 },
			{ SkillName.Taming, 0 },
			{ SkillName.Social, 0 },
		};

		foreach (var member in memberStats.SelectMany(group => group)) {
			if (member.CollectionsEnabled) {
				foreach (var (key, value) in member.Collections) {
					if (!collections.TryAdd(key, value)) {
						collections[key] += value;
					}
				}
			}

			if (!member.SkillsEnabled) continue;

			skills[SkillName.Farming] += (long)member.Skills.Farming;
			skills[SkillName.Mining] += (long)member.Skills.Mining;
			skills[SkillName.Combat] += (long)member.Skills.Combat;
			skills[SkillName.Foraging] += (long)member.Skills.Foraging;
			skills[SkillName.Fishing] += (long)member.Skills.Fishing;
			skills[SkillName.Enchanting] += (long)member.Skills.Enchanting;
			skills[SkillName.Alchemy] += (long)member.Skills.Alchemy;
			skills[SkillName.Carpentry] += (long)member.Skills.Carpentry;
			skills[SkillName.Runecrafting] += (long)member.Skills.Runecrafting;
			skills[SkillName.Taming] += (long)member.Skills.Taming;
			skills[SkillName.Social] += (long)member.Skills.Social;
		}

		newStats.Collections = collections;
		newStats.Skills = skills;
		
		context.HypixelGuildStats.Add(newStats);
		await context.SaveChangesAsync(ct);

		return;
	}
}
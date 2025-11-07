using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Quartz.Impl.Calendar;

namespace EliteAPI.Features.HypixelGuilds.Services;

public interface IHypixelGuildService
{
	Task UpdateGuildIfNeeded(MinecraftAccount account, CancellationToken c = default);
}

[RegisterService<IHypixelGuildService>(LifeTime.Scoped)]
public class HypixelGuildService(
	DataContext context,
	IHttpContextAccessor httpContextAccessor,
	HybridCache hybridCache,
	IMojangService mojangService,
	ILogger<HypixelGuildService> logger,
	IHypixelApi hypixelApi,
	IOptions<ConfigCooldownSettings> cooldownSettings
	) : IHypixelGuildService
{
	private readonly ConfigCooldownSettings _coolDowns = cooldownSettings.Value;
	
	public async Task UpdateGuildIfNeeded(MinecraftAccount account, CancellationToken c = default) {
		if (httpContextAccessor.HttpContext is not null && httpContextAccessor.HttpContext.IsKnownBot()) {
			return;
		}
		
		var member = await context.HypixelGuildMembers
			.FirstOrDefaultAsync(m => m.PlayerUuid == account.Id && m.Active, cancellationToken: c);

		try {
			if (member is not null) {
				await UpdateGuild(member.GuildId, c);
				return;
			}

			if (account.GuildLastUpdated.OlderThanSeconds(_coolDowns.HypixelGuildMemberCooldown)) {
				account.GuildLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				
				var hypixelXp = await context.PlayerData
					.Where(p => p.Uuid == account.Id)
					.Select(p => p.NetworkExp)
					.FirstOrDefaultAsync(cancellationToken: c);

				// Check that the player is decently active first
				// Hypixel network level ~124
				if (hypixelXp > 20_000_000) {
					await UpdateGuildByPlayerUuid(account.Id, c);
				}
			}
		} catch (Exception e) {
			logger.LogError(e, "Failed to update guild");
		}
	}

	private async Task UpdateGuild(string guildId, CancellationToken c = default) {
		await hybridCache.GetOrCreateAsync(guildId, async (ct) => {
			var guildLastUpdated = await context.HypixelGuilds
				.Where(g => g.Id == guildId)
				.Select(g => g.LastUpdated)
				.FirstOrDefaultAsync(cancellationToken: ct);

			if (guildLastUpdated.OlderThanSeconds(_coolDowns.HypixelGuildCooldown)) {
				return true;
			}
			
			var guildResponse = await hypixelApi.FetchGuildByIdAsync(guildId, ct);
			var guild = guildResponse.Content?.Guild;
		
			if (!guildResponse.IsSuccessful || guild is null) {
				logger.LogWarning("Failed to fetch guild {GuildId}", guildId);
				return true;
			}
			
			await UpdateGuildInternal(guild, guildId, ct);
			return true;
		}, cancellationToken: c);
	}
	
	private async Task UpdateGuildInternal(RawHypixelGuild guild, string guildId, CancellationToken c = default) {
		var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);
		var date = new DateOnly(sevenDaysAgo.Year, sevenDaysAgo.Month, sevenDaysAgo.Day);

		var existing = await context.HypixelGuilds
			.Include(g => g.Members)
			.ThenInclude(m => m.ExpHistory.Where(e => e.Day >= date))
			.FirstOrDefaultAsync(g => g.Id == guild.Id, c);

		if (existing is null) {
			existing = new HypixelGuild() {
				Id = guild.Id,
				Name = guild.Name,
				NameLower = guild.Name.ToLowerInvariant(),
				CreatedAt = guild.Created,
			};
			
			context.HypixelGuilds.Add(existing);
		}

		existing.Name = guild.Name;
		existing.NameLower = guild.Name.ToLowerInvariant();
		existing.CreatedAt = guild.Created;
		existing.Description = guild.Description ?? string.Empty;
		existing.PreferredGames = guild.PreferredGames;
		existing.PubliclyListed = guild.PublicallyListed;
		existing.Exp = guild.Exp;
		existing.Tag = guild.Tag;
		existing.TagColor = guild.TagColor;
		existing.GameExp = guild.GuildExpByGameType;
		existing.Ranks = guild.Ranks;
		existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		await UpdateGuildMembers(existing, guild.Members, c);
		await context.SaveChangesAsync(c);
	}

	private async Task UpdateGuildMembers(HypixelGuild guild, List<RawHypixelGuildMember> newMembers, CancellationToken c = default) {
		var current = guild.Members.ToDictionary(m => m.PlayerUuid);
		
		var batchGet = await mojangService.GetMinecraftAccounts(newMembers.Select(m => m.Uuid).ToList());

		foreach (var member in newMembers) {
			if (!batchGet.TryGetValue(member.Uuid, out var accountMeta) || accountMeta is null) continue;
			
			var xpHistory = member.ExpHistory.ToDictionary(kvp => {
				var split = kvp.Key.Split('-');
				var year = int.Parse(split[0]);
				var month = int.Parse(split[1]);
				var day = int.Parse(split[2]);
				return new DateOnly(year, month, day);
			}, kvp => kvp.Value);
			
			if (current.TryGetValue(member.Uuid, out var existing)) {
				existing.QuestParticipation = member.QuestParticipation;
				existing.Rank = member.Rank;
				existing.JoinedAt = member.Joined;

				foreach (var (date, xp) in xpHistory) {
					if (xp == 0) continue; // No point in saving zeros
					
					var found = existing.ExpHistory.FirstOrDefault(e => e.Day == date);
					if (found is not null) {
						found.Xp = xp;
						continue;
					}
					
					existing.ExpHistory.Add(new HypixelGuildMemberExp() {
						GuildMemberId = existing.Id,
						Day = date,
						Xp = xp
					});
				}
				continue;
			}

			var newMember = new HypixelGuildMember() {
				PlayerUuid = member.Uuid,
				GuildId = guild.Id,
				Active = true,
				Rank = member.Rank,
				JoinedAt = member.Joined,
				QuestParticipation = member.QuestParticipation,
				ExpHistory = xpHistory
					.Where(x => x.Value > 0)
					.Select(x => new HypixelGuildMemberExp {
						Day = x.Key,
						Xp = x.Value
					}).ToList()
			};
			
			context.HypixelGuildMembers.Add(newMember);
		}

		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		
		// Flag deleted members and update last updated times
		foreach (var member in guild.Members) {
			var stillExists = newMembers.FirstOrDefault(n => n.Uuid == member.PlayerUuid);
			if (stillExists is null) {
				member.Active = false;
			}
			
			await context.MinecraftAccounts
				.Where(a => member.PlayerUuid == a.Id)
				.ExecuteUpdateAsync(a => 
					a.SetProperty(e => e.GuildLastUpdated, now), cancellationToken: c);
		}
	}
	
	private async Task UpdateGuildByPlayerUuid(string playerUuid, CancellationToken c = default) {
		var guildResponse = await hypixelApi.FetchGuildByPlayerUuidAsync(playerUuid, c);
		var guild = guildResponse.Content?.Guild;
		
		if (!guildResponse.IsSuccessful || guild is null) {
			return;
		}
			
		await UpdateGuildInternal(guild, guild.Id, c);
	}
}
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.HypixelGuilds.Commands;
using EliteAPI.Features.Profiles.Commands;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using IMapper = AutoMapper.IMapper;

namespace EliteAPI.Features.Profiles.Services;

public interface IMemberService
{
	Task UpdatePlayerIfNeeded(string playerUuid, float cooldownMultiplier = 1);
	Task UpdatePlayerIfNeeded(string playerUuid, RequestedResources resources);
	Task UpdateProfileMemberIfNeeded(Guid memberId, float cooldownMultiplier = 1);
	Task<Guid?> GetProfileMemberId(string playerUuid, string profileId);
	Task<IQueryable<ProfileMember>?> ProfileMemberQuery(string playerUuid);
	Task<IQueryable<ProfileMember>?> ProfileMemberCompleteQuery(string playerUuid);
	Task<IQueryable<ProfileMember>?> ProfileMemberQuery(string playerUuid, RequestedResources resources);
	Task<IQueryable<ProfileMember>?> ProfileMemberCompleteQuery(string playerUuid, RequestedResources resources);
	Task<IQueryable<ProfileMember>?> ProfileMemberIgnQuery(string playerName);

	Task RefreshPlayerData(string playerUuid, MinecraftAccount? account = null);
	Task RefreshProfiles(string playerUuid, RequestedResources resources);
	Task<bool> IsPlayerActiveAsync(Guid profileMemberId);
}

public record RequestedResources
{
	public float CooldownMultiplier { get; set; } = 1;
	public bool Profiles { get; set; } = true;
	public bool PlayerData { get; set; } = true;
	public bool Museum { get; set; } = true;
	public bool Guild { get; set; } = true;
	public bool Garden { get; set; } = true;

	public static RequestedResources All => new() {
		Profiles = true,
		PlayerData = true,
		Museum = true,
		Guild = true,
		Garden = true
	};

	public static RequestedResources ProfilesOnly => new() {
		Profiles = true,
		PlayerData = false,
		Museum = false,
		Guild = false,
		Garden = false
	};
}

[RegisterService<IMemberService>(LifeTime.Scoped)]
public class MemberService(
	DataContext context,
	IHttpContextAccessor contextAccessor,
	IServiceScopeFactory provider,
	IMojangService mojangService,
	IOptions<ConfigCooldownSettings> coolDowns,
	IConnectionMultiplexer redis)
	: IMemberService
{
	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;

	public async Task<Guid?> GetProfileMemberId(string playerUuid, string profileId) {
		var guid = await context.ProfileMembers
			.AsNoTracking()
			.Where(p => p.PlayerUuid == playerUuid && p.ProfileId == profileId)
			.Select(p => p.Id)
			.FirstOrDefaultAsync();

		if (guid == Guid.Empty) return null;
		return guid;
	}

	public async Task<IQueryable<ProfileMember>?> ProfileMemberQuery(string playerUuid) {
		return await ProfileMemberQuery(playerUuid, RequestedResources.All);
	}

	public async Task<IQueryable<ProfileMember>?> ProfileMemberCompleteQuery(string playerUuid) {
		return await ProfileMemberCompleteQuery(playerUuid, RequestedResources.All);
	}

	public async Task<IQueryable<ProfileMember>?> ProfileMemberQuery(string playerUuid, RequestedResources resources) {
		await UpdatePlayerIfNeeded(playerUuid, resources);

		return context.ProfileMembers
			.Where(p => p.PlayerUuid == playerUuid)
			.Include(p => p.Metadata);
	}

	public async Task<IQueryable<ProfileMember>?> ProfileMemberCompleteQuery(string playerUuid,
		RequestedResources resources) {
		await UpdatePlayerIfNeeded(playerUuid, resources);

		var now = DateTimeOffset.UtcNow;
		return context.ProfileMembers
			.AsNoTracking()
			.Where(p => p.PlayerUuid == playerUuid)
			.Include(p => p.Metadata)
			.Include(p => p.MinecraftAccount)
			.ThenInclude(p => p.EliteAccount)
			.ThenInclude(e => e!.UserSettings)
			.Include(p => p.Profile)
			.ThenInclude(p => p.Garden)
			.Include(p => p.Skills)
			.Include(p => p.Farming)
			.Include(p => p.EventEntries!
				.Where(e =>
					(e.Status == EventMemberStatus.Active || e.Status == EventMemberStatus.Inactive)
					&& e.EndTime > now && e.StartTime <= now))
			.ThenInclude(m => m.Event)
			.Include(p => p.ChocolateFactory).AsNoTracking()
			.Include(p => p.JacobData)
			.ThenInclude(j => j.Contests)
			.ThenInclude(c => c.JacobContest)
			.Include(p => p.Inventories)
			.AsNoTracking()
			.AsSplitQuery();
	}

	public async Task<IQueryable<ProfileMember>?> ProfileMemberIgnQuery(string playerName) {
		var uuid = await mojangService.GetUuidFromUsername(playerName);

		if (uuid is null) return null;

		return await ProfileMemberQuery(uuid);
	}

	public async Task UpdatePlayerIfNeeded(string playerUuid, float cooldownMultiplier = 1) {
		await UpdatePlayerIfNeeded(playerUuid, RequestedResources.All);
	}

	public async Task UpdatePlayerIfNeeded(string playerUuid, RequestedResources resources) {
		if (contextAccessor.HttpContext.IsKnownBot()) return;

		var account = await mojangService.GetMinecraftAccountByUuidOrIgn(playerUuid);
		if (account is null) return;

		var lastUpdated = new LastUpdatedDto {
			PlayerUuid = account.Id,
			PlayerData = account.PlayerDataLastUpdated,
			Profiles = account.ProfilesLastUpdated,
		};

		// Background guild update - not critical for profile response
		await new UpdateGuildCommand { PlayerUuid = account.Id }.QueueJobAsync();

		await RefreshNeededData(lastUpdated, resources);
	}

	public async Task UpdateProfileMemberIfNeeded(Guid memberId, float cooldownMultiplier = 1) {
		if (contextAccessor.HttpContext.IsKnownBot()) return;

		var lastUpdated = await context.ProfileMembers
			.AsNoTracking()
			.Where(a => a.Id == memberId)
			.Include(a => a.MinecraftAccount)
			.AsNoTracking()
			.Select(a => new LastUpdatedDto {
				Profiles = a.MinecraftAccount.ProfilesLastUpdated,
				PlayerData = a.MinecraftAccount.PlayerDataLastUpdated,
				PlayerUuid = a.MinecraftAccount.Id
			})
			.FirstOrDefaultAsync();

		if (lastUpdated?.PlayerUuid is null) return;

		var account = await mojangService.GetMinecraftAccountByUuid(lastUpdated.PlayerUuid);
		if (account is null) return;

		await RefreshNeededData(lastUpdated, cooldownMultiplier);
	}

	private async Task RefreshNeededData(LastUpdatedDto lastUpdated, float cooldownMultiplier = 1) {
		await RefreshNeededData(lastUpdated, new RequestedResources() { CooldownMultiplier = cooldownMultiplier });
	}

	private async Task RefreshNeededData(LastUpdatedDto lastUpdated, RequestedResources resources) {
		var playerUuid = lastUpdated.PlayerUuid;
		var db = redis.GetDatabase();

		var updatePlayer = false;
		var updateProfiles = false;

		if (resources.Profiles && lastUpdated.Profiles.OlderThanSeconds((int)(_coolDowns.SkyblockProfileCooldown *
		                                                                      resources.CooldownMultiplier))
		                       && !await db.KeyExistsAsync($"profile:{playerUuid}:updating")) {
			db.StringSet($"profile:{playerUuid}:updating", "1", TimeSpan.FromSeconds(15));
			updateProfiles = true;
		}

		if (resources.PlayerData && lastUpdated.PlayerData.OlderThanSeconds((int)(_coolDowns.HypixelPlayerDataCooldown *
			                         resources.CooldownMultiplier))
		                         && !await db.KeyExistsAsync($"player:{playerUuid}:updating")) {
			db.StringSet($"player:{playerUuid}:updating", "1", TimeSpan.FromSeconds(15));
			updatePlayer = true;
		}

		if (updateProfiles) await RefreshProfiles(playerUuid, resources);

		// Background player data refresh
		if (updatePlayer) {
			await new RefreshPlayerDataCommand { PlayerUuid = playerUuid }.QueueJobAsync();
		}
	}

	public async Task RefreshProfiles(string playerUuid, RequestedResources resources) {
		using var scope = provider.CreateScope();
		var processor = scope.ServiceProvider.GetRequiredService<IProfileProcessorService>();
		var hypixelService = scope.ServiceProvider.GetRequiredService<IHypixelService>();

		var data = await hypixelService.FetchProfiles(playerUuid);

		if (data.Value is null) {
			var logger = scope.ServiceProvider.GetRequiredService<ILogger<MemberService>>();
			logger.LogError("Failed to load profiles for {PlayerUuid}", playerUuid);
			return;
		}

		try {
			await processor.ProcessProfilesWaitForOnePlayer(data.Value, playerUuid, resources);
		}
		catch (Exception e) {
			var logger = scope.ServiceProvider.GetRequiredService<ILogger<MemberService>>();
			logger.LogError(e, "Failed to process profiles for {PlayerUuid}", playerUuid);
		}
	}

	public async Task RefreshPlayerData(string playerUuid, MinecraftAccount? account = null) {
		using var scope = provider.CreateScope();
		await using var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

		var mojang = scope.ServiceProvider.GetRequiredService<IMojangService>();
		var hypixelService = scope.ServiceProvider.GetRequiredService<IHypixelService>();
		var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

		var minecraftAccount = account ?? await mojang.GetMinecraftAccountByUuidOrIgn(playerUuid);
		if (minecraftAccount is null) return;

		playerUuid = minecraftAccount.Id;

		var data = await hypixelService.FetchPlayer(playerUuid);
		var player = data.Value;

		if (player?.Player is null) return;

		var existing = await dataContext.PlayerData.FirstOrDefaultAsync(a => a.Uuid == minecraftAccount.Id);
		var playerData = mapper.Map<PlayerData>(player.Player);

		if (existing is null) {
			playerData.Uuid = minecraftAccount.Id;

			minecraftAccount.PlayerData = playerData;
			minecraftAccount.PlayerDataLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			dataContext.PlayerData.Add(playerData);
		}
		else {
			mapper.Map(playerData, existing);

			if (existing.MinecraftAccount is not null) {
				existing.MinecraftAccount.PlayerDataLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			}
			else {
				minecraftAccount.PlayerDataLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				if (dataContext.Entry(minecraftAccount).State == EntityState.Detached)
					dataContext.Entry(minecraftAccount).State = EntityState.Modified;
			}

			dataContext.PlayerData.Update(existing);
		}

		await dataContext.SaveChangesAsync();
	}

	/// <summary>
	/// Checks if a player is considered active based on their skill experience gains.
	/// </summary>
	/// <param name="profileMemberId"></param>
	/// <returns></returns>
	public async Task<bool> IsPlayerActiveAsync(Guid profileMemberId) {
		var entries = await context.SkillExperiences
			.Where(s => s.ProfileMemberId == profileMemberId)
			.OrderByDescending(s => s.Time)
			.Take(2)
			.ToListAsync();

		if (entries.Count < 2) return true;

		var latest = entries[0];
		var previous = entries[1];

		if (latest.Time < DateTimeOffset.UtcNow.AddHours(-_coolDowns.ActivityStaleThresholdHours)) {
			return true;
		}

		var xpGain = (latest.Farming - previous.Farming)
		             + (latest.Combat - previous.Combat)
		             + (latest.Mining - previous.Mining)
		             + (latest.Foraging - previous.Foraging)
		             + (latest.Fishing - previous.Fishing)
		             + (latest.Enchanting - previous.Enchanting)
		             + (latest.Alchemy - previous.Alchemy)
		             + (latest.Carpentry - previous.Carpentry)
		             + (latest.Runecrafting - previous.Runecrafting)
		             + (latest.Social - previous.Social)
		             + (latest.Taming - previous.Taming);

		return xpGain >= _coolDowns.ActivityMinXpGain;
	}
}

public class LastUpdatedDto
{
	public long Profiles { get; set; }
	public long PlayerData { get; set; }
	public required string PlayerUuid { get; set; }
}
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
using System.Diagnostics;
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
	public Guid? RequireActiveMemberId { get; set; } = null;

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
	IConnectionMultiplexer redis,
	IUpdatePathMetrics updatePathMetrics)
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
		var totalTimer = Stopwatch.StartNew();
		var success = false;

		try {
			if (contextAccessor.HttpContext.IsKnownBot()) {
				success = true;
				return;
			}

			var mojangTimer = Stopwatch.StartNew();
			var mojangSuccess = false;
			MinecraftAccount? account = null;
			try {
				account = await mojangService.GetMinecraftAccountByUuidOrIgn(playerUuid);
				mojangSuccess = true;
			}
			finally {
				mojangTimer.Stop();
				updatePathMetrics.RecordStage("update_player_if_needed", "mojang_lookup",
					mojangTimer.Elapsed.TotalMilliseconds, mojangSuccess);
			}

			if (account is null) {
				success = true;
				return;
			}

			var lastUpdated = new LastUpdatedDto {
				PlayerUuid = account.Id,
				PlayerData = account.PlayerDataLastUpdated,
				Profiles = account.ProfilesLastUpdated,
			};

			// Background guild update - not critical for profile response
			if (resources.Guild) {
				var guildTimer = Stopwatch.StartNew();
				var guildSuccess = false;
				try {
					await new UpdateGuildCommand { PlayerUuid = account.Id }.QueueJobAsync();
					guildSuccess = true;
				}
				finally {
					guildTimer.Stop();
					updatePathMetrics.RecordStage("update_player_if_needed", "queue_guild_update",
						guildTimer.Elapsed.TotalMilliseconds, guildSuccess);
				}
			}

			var refreshTimer = Stopwatch.StartNew();
			var refreshSuccess = false;
			try {
				await RefreshNeededData(lastUpdated, resources);
				refreshSuccess = true;
			}
			finally {
				refreshTimer.Stop();
				updatePathMetrics.RecordStage("update_player_if_needed", "refresh_needed_data",
					refreshTimer.Elapsed.TotalMilliseconds, refreshSuccess);
			}

			success = true;
		}
		finally {
			totalTimer.Stop();
			updatePathMetrics.RecordStage("update_player_if_needed", "total",
				totalTimer.Elapsed.TotalMilliseconds, success);
		}
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

		var profileGateTimer = Stopwatch.StartNew();
		try {
			if (resources.Profiles && lastUpdated.Profiles.OlderThanSeconds((int)(_coolDowns.SkyblockProfileCooldown *
			                                                                      resources.CooldownMultiplier))
			                       && !await db.KeyExistsAsync($"profile:{playerUuid}:updating")) {
				db.StringSet($"profile:{playerUuid}:updating", "1", TimeSpan.FromSeconds(15));

				if (resources.RequireActiveMemberId is { } memberId) {
					updateProfiles = await IsPlayerActiveAsync(memberId);
				}
				else {
					updateProfiles = true;
				}
			}
		}
		finally {
			profileGateTimer.Stop();
			updatePathMetrics.RecordStage("refresh_needed_data",
				updateProfiles ? "profile_refresh_gate_triggered" : "profile_refresh_gate_skipped",
				profileGateTimer.Elapsed.TotalMilliseconds, true);
		}

		var playerGateTimer = Stopwatch.StartNew();
		try {
			if (resources.PlayerData && lastUpdated.PlayerData.OlderThanSeconds((int)(_coolDowns.HypixelPlayerDataCooldown *
				                         resources.CooldownMultiplier))
			                         && !await db.KeyExistsAsync($"player:{playerUuid}:updating")) {
				db.StringSet($"player:{playerUuid}:updating", "1", TimeSpan.FromSeconds(15));
				updatePlayer = true;
			}
		}
		finally {
			playerGateTimer.Stop();
			updatePathMetrics.RecordStage("refresh_needed_data",
				updatePlayer ? "player_refresh_gate_triggered" : "player_refresh_gate_skipped",
				playerGateTimer.Elapsed.TotalMilliseconds, true);
		}

		if (updateProfiles) {
			var refreshProfilesTimer = Stopwatch.StartNew();
			var refreshProfilesSuccess = false;
			try {
				await RefreshProfiles(playerUuid, resources);
				refreshProfilesSuccess = true;
			}
			finally {
				refreshProfilesTimer.Stop();
				updatePathMetrics.RecordStage("refresh_needed_data", "refresh_profiles",
					refreshProfilesTimer.Elapsed.TotalMilliseconds, refreshProfilesSuccess);
			}
		}

		// Background player data refresh
		if (updatePlayer) {
			var queuePlayerRefreshTimer = Stopwatch.StartNew();
			var queuePlayerRefreshSuccess = false;
			try {
				await new RefreshPlayerDataCommand { PlayerUuid = playerUuid }.QueueJobAsync();
				queuePlayerRefreshSuccess = true;
			}
			finally {
				queuePlayerRefreshTimer.Stop();
				updatePathMetrics.RecordStage("refresh_needed_data", "queue_player_data_refresh",
					queuePlayerRefreshTimer.Elapsed.TotalMilliseconds, queuePlayerRefreshSuccess);
			}
		}
	}

	public async Task RefreshProfiles(string playerUuid, RequestedResources resources) {
		if (contextAccessor.HttpContext.IsKnownBot()) return;

		var totalTimer = Stopwatch.StartNew();
		var success = false;

		try {
			using var scope = provider.CreateScope();
			var processor = scope.ServiceProvider.GetRequiredService<IProfileProcessorService>();
			var hypixelService = scope.ServiceProvider.GetRequiredService<IHypixelService>();

			var fetchTimer = Stopwatch.StartNew();
			var fetchSuccess = false;
			var data = await hypixelService.FetchProfiles(playerUuid);
			fetchSuccess = true;
			fetchTimer.Stop();
			updatePathMetrics.RecordStage("refresh_profiles", "fetch_profiles_api",
				fetchTimer.Elapsed.TotalMilliseconds, fetchSuccess);

			if (data.Value is null) {
				var logger = scope.ServiceProvider.GetRequiredService<ILogger<MemberService>>();
				logger.LogError("Failed to load profiles for {PlayerUuid}", playerUuid);
				return;
			}

			var processTimer = Stopwatch.StartNew();
			var processSuccess = false;
			try {
				await processor.ProcessProfilesWaitForOnePlayer(data.Value, playerUuid, resources);
				processSuccess = true;
			}
			finally {
				processTimer.Stop();
				updatePathMetrics.RecordStage("refresh_profiles", "process_profiles",
					processTimer.Elapsed.TotalMilliseconds, processSuccess);
			}

			success = true;
		}
		catch (Exception e) {
			using var scope = provider.CreateScope();
			var logger = scope.ServiceProvider.GetRequiredService<ILogger<MemberService>>();
			logger.LogError(e, "Failed to process profiles for {PlayerUuid}", playerUuid);
		}
		finally {
			totalTimer.Stop();
			updatePathMetrics.RecordStage("refresh_profiles", "total",
				totalTimer.Elapsed.TotalMilliseconds, success);
		}
	}

	public async Task RefreshPlayerData(string playerUuid, MinecraftAccount? account = null) {
		if (contextAccessor.HttpContext.IsKnownBot()) return;

		var totalTimer = Stopwatch.StartNew();
		var success = false;

		try {
			using var scope = provider.CreateScope();
			await using var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

			var mojang = scope.ServiceProvider.GetRequiredService<IMojangService>();
			var hypixelService = scope.ServiceProvider.GetRequiredService<IHypixelService>();
			var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

			var mojangTimer = Stopwatch.StartNew();
			var mojangSuccess = false;
			var minecraftAccount = account ?? await mojang.GetMinecraftAccountByUuidOrIgn(playerUuid);
			mojangSuccess = true;
			mojangTimer.Stop();
			updatePathMetrics.RecordStage("refresh_player_data", "mojang_lookup",
				mojangTimer.Elapsed.TotalMilliseconds, mojangSuccess);

			if (minecraftAccount is null) return;

			playerUuid = minecraftAccount.Id;

			var fetchTimer = Stopwatch.StartNew();
			var fetchSuccess = false;
			var data = await hypixelService.FetchPlayer(playerUuid);
			fetchSuccess = true;
			fetchTimer.Stop();
			updatePathMetrics.RecordStage("refresh_player_data", "fetch_player_api",
				fetchTimer.Elapsed.TotalMilliseconds, fetchSuccess);

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

			var saveTimer = Stopwatch.StartNew();
			var saveSuccess = false;
			await dataContext.SaveChangesAsync();
			saveSuccess = true;
			saveTimer.Stop();
			updatePathMetrics.RecordStage("refresh_player_data", "save_player_data",
				saveTimer.Elapsed.TotalMilliseconds, saveSuccess);

			success = true;
		}
		finally {
			totalTimer.Stop();
			updatePathMetrics.RecordStage("refresh_player_data", "total",
				totalTimer.Elapsed.TotalMilliseconds, success);
		}
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


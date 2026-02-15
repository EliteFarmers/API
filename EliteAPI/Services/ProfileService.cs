using AutoMapper;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;

namespace EliteAPI.Services;

public class ProfileService(
	DataContext context,
	IMojangService mojangService,
	IMapper mapper,
	IOptions<ConfigCooldownSettings> coolDowns,
	IMemberService memberService)
	: IProfileService
{
	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;

	public async Task<Profile?> GetProfile(string profileId) {
		return await context.Profiles
			.AsNoTracking()
			.Include(p => p.Members)
			.ThenInclude(m => m.MinecraftAccount)
			.ThenInclude(m => m.EliteAccount!.UserSettings)
			.Include(p => p.Members)
			.ThenInclude(m => m.Farming)
			.Include(p => p.Members)
			.ThenInclude(m => m.Metadata)
			.FirstOrDefaultAsync(p => p.ProfileId.Equals(profileId));
	}

	public async Task<Profile?> GetPlayersProfileByName(string playerUuid, string profileName) {
		var query = await memberService.ProfileMemberQuery(playerUuid);
		if (query is null) return null;

		var member = await query
			.Include(p => p.Profile)
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Profile.ProfileName == profileName);

		return member?.Profile;
	}

	public async Task<Profile?> GetPlayersSelectedProfile(string playerUuid) {
		return (await GetSelectedProfileMember(playerUuid))?.Profile;
	}

	public async Task<List<Profile>> GetPlayersProfiles(string playerUuid) {
		await memberService.UpdatePlayerIfNeeded(playerUuid);

		var profiles = await context.ProfileMembers
			.Include(p => p.Profile)
			.Include(p => p.MinecraftAccount)
			.ThenInclude(m => m.EliteAccount!.UserSettings)
			.Select(p => p.Profile)
			.ToListAsync();

		return profiles;
	}

	public async Task<List<ProfileDetailsDto>> GetProfilesDetails(string playerUuid) {
		await memberService.UpdatePlayerIfNeeded(playerUuid);

		var existing = await context.ProfileMembers
			.AsNoTracking()
			.Where(m => m.PlayerUuid.Equals(playerUuid))
			.Select(m => new { m.ProfileId, m.ProfileName, m.IsSelected })
			.ToListAsync();

		var profileIds = existing.Select(e => e.ProfileId).ToList();

		var profiles = await context.Profiles
			.AsNoTracking()
			.Include(p => p.Members)
			.ThenInclude(m => m.MinecraftAccount)
			.ThenInclude(m => m.EliteAccount!.UserSettings)
			.Include(p => p.Members)
			.ThenInclude(m => m.Farming)
			.Include(p => p.Members)
			.ThenInclude(m => m.Metadata)
			.Where(p => profileIds.Contains(p.ProfileId))
			.ToListAsync();

		var mappedProfiles = mapper.Map<List<ProfileDetailsDto>>(profiles);

		// This needs to be fetched because "selected" lives on the ProfileMembers
		var selected = existing.FirstOrDefault(e => e.IsSelected)?.ProfileId;

		mappedProfiles.ForEach(p => {
			if (selected is not null) p.Selected = p.ProfileId == selected;

			// Make the profile name local to the member if it exists
			p.ProfileName = existing.FirstOrDefault(e => e.ProfileId == p.ProfileId)?.ProfileName ?? p.ProfileName;
		});

		return mappedProfiles;
	}

	public async Task<ProfileMember?> GetProfileMember(string playerUuid, string profileUuid) {
		var query = await memberService.ProfileMemberCompleteQuery(playerUuid);
		if (query is null) return null;

		return await query.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Profile.ProfileId.Equals(profileUuid));
	}

	public async Task<ProfileMember?> GetSelectedProfileMember(string playerUuid) {
		var query = await memberService.ProfileMemberCompleteQuery(playerUuid);
		if (query is null) return null;

		return await query.AsNoTracking()
			.FirstOrDefaultAsync(p => p.IsSelected);
	}

	public async Task<Garden?> GetProfileGarden(string profileUuid) {
		return await context.Gardens.AsNoTracking()
			.FirstOrDefaultAsync(g => g.ProfileId.Equals(profileUuid));
	}

	public async Task<Garden?> GetSelectedGarden(string playerUuid) {
		var profileId = await GetSelectedProfileUuid(playerUuid);
		if (profileId is null) return null;

		return await GetProfileGarden(profileId);
	}

	public async Task<ProfileMember?> GetProfileMemberByProfileName(string playerUuid, string profileName) {
		var query = await memberService.ProfileMemberCompleteQuery(playerUuid);
		if (query is null) return null;

		return await query.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Profile.ProfileName == profileName);
	}

	private async Task<string?> GetSelectedProfileUuid(string playerUuid) {
		return await context.ProfileMembers.AsNoTracking()
			.Where(p => p.PlayerUuid == playerUuid && p.IsSelected)
			.Select(p => p.ProfileId)
			.FirstOrDefaultAsync();
	}

	public async Task<PlayerData?> GetPlayerData(string playerUuid, bool skipCooldown = false) {
		await memberService.UpdatePlayerIfNeeded(playerUuid);

		var data = await context.PlayerData
			.AsNoTracking()
			.Include(p => p.MinecraftAccount)
			.ThenInclude(p => p.GuildMembers)
			.ThenInclude(p => p.ExpHistory.OrderByDescending(e => e.Day).Take(14))
			.Include(p => p.MinecraftAccount)
			.ThenInclude(p => p.GuildMembers)
			.ThenInclude(p => p.Guild)
			.FirstOrDefaultAsync(p => p.Uuid.Equals(playerUuid));

		if (data is not null && !data.LastUpdated.OlderThanSeconds(skipCooldown
			    ? _coolDowns.HypixelPlayerDataLinkingCooldown
			    : _coolDowns.HypixelPlayerDataCooldown))
			return data;

		await memberService.RefreshPlayerData(playerUuid);

		return await context.PlayerData
			.AsNoTracking()
			.Include(p => p.MinecraftAccount)
			.ThenInclude(p => p.GuildMembers)
			.ThenInclude(p => p.ExpHistory.OrderByDescending(e => e.Day).Take(14))
			.Include(p => p.MinecraftAccount)
			.ThenInclude(p => p.GuildMembers)
			.ThenInclude(p => p.Guild)
			.FirstOrDefaultAsync(p => p.Uuid.Equals(playerUuid));
	}

	public async Task<PlayerData?> GetPlayerDataByIgn(string playerName, bool skipCooldown = false) {
		var uuid = await mojangService.GetUuidFromUsername(playerName);
		if (uuid is null) return null;

		return await GetPlayerData(uuid, skipCooldown);
	}

	public async Task<PlayerData?> GetPlayerDataByUuidOrIgn(string uuidOrIgn, bool skipCooldown = false) {
		if (uuidOrIgn.Length == 32) return await GetPlayerData(uuidOrIgn, skipCooldown);
		return await GetPlayerDataByIgn(uuidOrIgn, skipCooldown);
	}
}

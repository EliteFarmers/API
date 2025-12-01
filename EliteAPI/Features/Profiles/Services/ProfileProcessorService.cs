using EFCore.BulkExtensions;
using EliteAPI.Background.Profiles;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Features.Profiles.Mappers;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Events;
using EliteAPI.Parsers.Farming;
using EliteAPI.Parsers.Inventories;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using HypixelAPI.Networth.Calculators;
using HypixelAPI.Networth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Features.Profiles.Services;

public interface IProfileProcessorService
{
	/// <summary>
	/// Processes the response from the Hypixel API
	/// </summary>
	/// <param name="data">Hypixel API Response</param>
	/// <param name="requestedPlayerUuid">The player uuid that was requested to get this data</param>
	/// <returns></returns>
	Task<List<ProfileResponse>> ProcessProfilesResponse(ProfilesResponse data, string? requestedPlayerUuid);

	/// <summary>
	/// Processes the response from the Hypixel API, only waiting for a single player to finish processing
	/// </summary>
	/// <param name="data">Hypixel API Response</param>
	/// <param name="requestedPlayerUuid">The player uuid that was requested to get this data</param>
	/// <returns></returns>
	Task ProcessProfilesWaitForOnePlayer(ProfilesResponse data, string requestedPlayerUuid);

	/// <summary>
	/// Processes profile members from the Hypixel API, for all but one player
	/// </summary>
	/// <param name="data">Hypixel API Response</param>
	/// <param name="excludedPlayerUuid">The player uuid that will be skipped</param>
	/// <returns></returns>
	Task ProcessRemainingMembers(ProfilesResponse data, string excludedPlayerUuid);

	/// <summary>
	/// Processes a profile from the Hypixel API
	/// </summary>
	///	<param name="profileData">The profile to process</param>
	/// <param name="requestedPlayerUuid">The player uuid that was requested to get this data</param>
	Task<(Profile? profile, Dictionary<string, ProfileMemberResponse> members)> ProcessProfileData(
		ProfileResponse profileData, string? requestedPlayerUuid);

	Task UpdateGardenData(string profileId);

	/// <summary>
	/// Processes a member from the Hypixel API
	/// </summary>
	/// <param name="profile">The profile to process</param>
	/// <param name="memberData">The member data to process</param>
	/// <param name="playerUuid">The player uuid of the member</param>
	/// <param name="requestedPlayerUuid">The player uuid that was requested to get this data</param>
	/// <param name="profileData">Raw profile data</param>
	Task ProcessMemberData(Profile profile, ProfileMemberResponse memberData, string playerUuid,
		string requestedPlayerUuid, ProfileResponse? profileData = null);

	Task<NetworthBreakdown> GetNetworthBreakdownAsync(ProfileMember member);
}

[RegisterService<IProfileProcessorService>(LifeTime.Scoped)]
public partial class ProfileProcessorService(
	DataContext context,
	ILogger<ProfileProcessorService> logger,
	IMojangService mojangService,
	IMessageService messageService,
	ILbService lbService,
	ISchedulerFactory schedulerFactory,
	IOptions<ChocolateFactorySettings> cfOptions,
	IOptions<ConfigCooldownSettings> coolDowns,
	IOptions<ConfigFarmingWeightSettings> farmingWeightOptions,
	AutoMapper.IMapper mapper,
	SkyBlockItemNetworthCalculator networthCalculator,
	PetNetworthCalculator petNetworthCalculator,
	IPriceProvider priceProvider,
	IHttpContextAccessor httpContextAccessor
) : IProfileProcessorService
{
	private readonly ChocolateFactorySettings _cfSettings = cfOptions.Value;
	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
	private readonly ConfigFarmingWeightSettings _farmingWeightOptions = farmingWeightOptions.Value;
	private readonly SkyBlockItemNetworthCalculator _networthCalculator = networthCalculator;
	private readonly PetNetworthCalculator _petNetworthCalculator = petNetworthCalculator;
	private readonly IPriceProvider _priceProvider = priceProvider;

	private readonly Func<DataContext, string, string, Task<ProfileMember?>> _fetchProfileMemberData =
		EF.CompileAsyncQuery((DataContext c, string playerUuid, string profileUuid) =>
			c.ProfileMembers
				.Include(p => p.MinecraftAccount)
				.Include(p => p.Profile)
				.ThenInclude(p => p.Garden)
				.Include(p => p.Skills)
				.Include(p => p.Farming)
				.Include(p => p.JacobData)
				.ThenInclude(j => j.Contests)
				.ThenInclude(p => p.JacobContest)
				.Include(p => p.EventEntries)
				.Include(p => p.ChocolateFactory)
				.Include(p => p.Metadata)
				.Include(p => p.Inventories)
				.ThenInclude(i => i.Items)
				.AsSplitQuery()
				.FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
		);

	public async Task<List<ProfileResponse>>
		ProcessProfilesResponse(ProfilesResponse data, string? requestedPlayerUuid) {
		if (!data.Success) {
			logger.LogWarning("Received unsuccessful profiles response from {PlayerUuid}", requestedPlayerUuid);
			return [];
		}

		var profileIds = (data.Profiles ?? []).Select(p => p.ProfileId.Replace("-", "")).ToList();

		// Get profiles that aren't in the response
		var wipedProfiles = await context.ProfileMembers
			.AsNoTracking()
			.Include(p => p.Profile)
			.Include(p => p.MinecraftAccount)
			.Where(p => p.PlayerUuid.Equals(requestedPlayerUuid) && !profileIds.Contains(p.ProfileId))
			.ToListAsync();

		// Mark profiles as removed
		foreach (var wiped in wipedProfiles) {
			if (profileIds.Contains(wiped.ProfileId)) continue; // Shouldn't be necessary, but just in case

			if (wiped.Profile.GameMode != "bingo" && !wiped.WasRemoved)
				messageService.SendWipedMessage(
					wiped.PlayerUuid,
					wiped.MinecraftAccount.Name,
					wiped.ProfileId,
					wiped.MinecraftAccount.AccountId?.ToString() ?? "");

			// Ensure member is marked as deleted
			if (!wiped.WasRemoved || wiped.IsSelected)
				await context.ProfileMembers
					.Where(m => m.Id == wiped.Id)
					.ExecuteUpdateAsync(member => member
						.SetProperty(m => m.WasRemoved, true)
						.SetProperty(m => m.IsSelected, false)
					);

			// Ensure profile is marked as deleted if all members are removed
			if (!wiped.Profile.IsDeleted) {
				var count = await context.Profiles
					.Where(p => p.ProfileId == wiped.ProfileId && p.Members.All(m => m.WasRemoved))
					.ExecuteUpdateAsync(p => p.SetProperty(pr => pr.IsDeleted, true));

				// Mark profile leaderboard entries as removed if the profile was updated
				if (count > 0) await MarkProfileLeaderboardEntriesAsDeleted(wiped.ProfileId);
			}
			else {
				await MarkProfileLeaderboardEntriesAsDeleted(wiped.ProfileId);
			}

			await context.LeaderboardEntries
				.Where(e => e.ProfileMemberId == wiped.Id)
				.ExecuteUpdateAsync(e => e.SetProperty(le => le.IsRemoved, true));
		}

		return data.Profiles?.ToList() ?? [];
	}

	private async Task MarkProfileLeaderboardEntriesAsDeleted(string profileId) {
		await context.LeaderboardEntries
			.Where(e => e.ProfileId == profileId)
			.ExecuteUpdateAsync(e => e.SetProperty(le => le.IsRemoved, true));
	}

	public async Task ProcessProfilesWaitForOnePlayer(ProfilesResponse data, string requestedPlayerUuid) {
		var profiles = await ProcessProfilesResponse(data, requestedPlayerUuid);
		if (profiles.Count == 0) return;

		foreach (var profileData in profiles) {
			var (profile, members) = await ProcessProfileData(profileData, requestedPlayerUuid);
			if (profile is null) continue;

			if (!members.TryGetValue(requestedPlayerUuid, out var member)) continue;

			await ProcessMemberData(profile, member, requestedPlayerUuid, requestedPlayerUuid, profileData);
		}

		await context.SaveChangesAsync();

		// Send remaining members to background job
		var jobData = new JobDataMap {
			{ "playerUuid", requestedPlayerUuid },
			{ "data", data }
		};

		var scheduler = await schedulerFactory.GetScheduler();
		await scheduler.TriggerJob(ProcessRemainingMembersBackgroundJob.Key, jobData);
	}

	public async Task ProcessRemainingMembers(ProfilesResponse data, string excludedPlayerUuid) {
		var profiles = data.Profiles?.ToList();
		if (profiles is null || profiles.Count == 0) return;

		foreach (var profileData in profiles) {
			var members = profileData.Members.ToDictionary(
				pair => pair.Key.Replace("-", ""), // Strip hyphens from UUIDs
				pair => pair.Value);
			if (members.Count == 0) continue;

			var profileId = profileData.ProfileId.Replace("-", "");
			var profile = await context.Profiles
				.Include(p => p.Garden)
				.FirstOrDefaultAsync(p => p.ProfileId == profileId);

			if (profile is null) {
				logger.LogWarning("Profile {ProfileId} was not found when processing remaining members!", profileId);
				continue;
			}

			foreach (var (playerUuid, member) in members) {
				if (playerUuid == excludedPlayerUuid) continue;
				await ProcessMemberData(profile, member, playerUuid, excludedPlayerUuid, profileData);
			}
		}

		await context.SaveChangesAsync();
	}

	public async Task<(Profile? profile, Dictionary<string, ProfileMemberResponse> members)> ProcessProfileData(
		ProfileResponse profileData, string? requestedPlayerUuid) {
		var members = profileData.Members.ToDictionary(
			pair => pair.Key.Replace("-", ""), // Strip hyphens from UUIDs
			pair => pair.Value);
		if (members.Count == 0) return (null, members);

		var profileId = profileData.ProfileId.Replace("-", "");
		var existing = await context.Profiles
			.Include(p => p.Garden)
			.FirstOrDefaultAsync(p => p.ProfileId == profileId);

		var profile = existing ?? new Profile {
			ProfileId = profileId,
			ProfileName = profileData.CuteName,
			GameMode = profileData.GameMode,
			Members = [],
			IsDeleted = false
		};

		profile.BankBalance = profileData.Banking?.Balance ?? 0.0;

		profile.SocialXp = 0;
		foreach (var member in members.Values) {
			profile.CombineMinions(member.PlayerData?.CraftedGenerators);
			profile.SocialXp += member.PlayerData?.Experience?.SkillSocial ?? 0;
		}

		if (existing is not null) {
			if (profile.GameMode != profileData.GameMode) {
				context.GameModeHistories.Add(new GameModeHistory() {
					ProfileId = profile.ProfileId,
					Old = profile.GameMode ?? "classic",
					New = profileData.GameMode ?? "classic",
					ChangedAt = DateTimeOffset.UtcNow
				});
				profile.GameMode = profileData.GameMode;
			}

			profile.ProfileName = profileData.CuteName;
		}
		else {
			context.Profiles.Add(profile);
		}

		try {
			await context.SaveChangesAsync();
		}
		catch (Exception e) {
			logger.LogError(e, "Failed to save profile {ProfileId} to database", profileId);
		}

		await lbService.UpdateProfileLeaderboardsAsync(profile, CancellationToken.None);

		if (httpContextAccessor.HttpContext is not null && !httpContextAccessor.HttpContext.IsKnownBot()) {
			if (existing?.Garden is null ||
			    existing.Garden.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockGardenCooldown)) {
				await UpdateGardenData(profileId);
			}

			if (existing is null || existing.MuseumLastUpdated.OlderThanSeconds(_coolDowns.SkyblockMuseumCooldown)) {
				await new MuseumUpdateCommand { ProfileId = profileId }.QueueJobAsync();
			}
		}

		return (profile, members);
	}

	public async Task UpdateGardenData(string profileId) {
		var data = new JobDataMap {
			{ "ProfileId", profileId }
		};

		var scheduler = await schedulerFactory.GetScheduler();
		await scheduler.TriggerJob(RefreshGardenBackgroundJob.Key, data);
	}

	public async Task ProcessMemberData(Profile profile, ProfileMemberResponse memberData, string playerUuid,
		string requestedPlayerUuid, ProfileResponse? profileData = null) {
		var existing = await _fetchProfileMemberData(context, playerUuid, profile.ProfileId);

		// Should remove if deleted or coop invitation is not accepted
		var shouldRemove = memberData.Profile?.DeletionNotice is not null ||
		                   memberData.Profile?.CoopInvitation is { Confirmed: false };

		// Exit early if removed, and still should be removed
		// This means that we already processed the member when it was removed, and the data is still the same
		if (existing?.WasRemoved == true && shouldRemove) return;

		var isSelected = profileData?.Selected is true && playerUuid == requestedPlayerUuid;

		if (existing is not null) {
			existing.WasRemoved = shouldRemove;

			if (shouldRemove) {
				existing.IsSelected = false;

				messageService.SendWipedMessage(
					playerUuid,
					existing.MinecraftAccount.Name,
					existing.ProfileId,
					existing.MinecraftAccount.AccountId?.ToString() ?? "");
			}

			// Only update if the player is the requester
			if (playerUuid == requestedPlayerUuid) {
				existing.IsSelected = isSelected;
				existing.ProfileName = profile.ProfileName;
			}

			// Only update if null (profile names can differ between members)
			existing.ProfileName ??= profile.ProfileName;
			existing.Metadata ??= new ProfileMemberMetadata {
				Name = existing.MinecraftAccount.Name,
				Uuid = existing.MinecraftAccount.Id,
				Profile = profile.ProfileName,
				ProfileUuid = profile.ProfileId,
				SkyblockExperience = existing.SkyblockXp
			};

			existing.Metadata.Name = existing.MinecraftAccount.Name;
			existing.Metadata.Profile = profile.ProfileName;
			existing.Metadata.SkyblockExperience = existing.SkyblockXp;

			existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			existing.MinecraftAccount.ProfilesLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			context.MinecraftAccounts.Update(existing.MinecraftAccount);
			context.Entry(existing).State = EntityState.Modified;

			if (existing.WasRemoved == false) profile.IsDeleted = false;

			await UpdateProfileMember(profile, existing, memberData);

			return;
		}

		var minecraftAccount = await mojangService.GetMinecraftAccountByUuid(playerUuid);
		if (minecraftAccount is null) return;

		var member = new ProfileMember {
			Id = Guid.NewGuid(),
			PlayerUuid = playerUuid,

			Profile = profile,
			ProfileId = profile.ProfileId,
			ProfileName = profile.ProfileName,

			Metadata = new ProfileMemberMetadata {
				Name = playerUuid,
				Uuid = minecraftAccount.Id,
				Profile = profile.ProfileName,
				ProfileUuid = profile.ProfileId
			},

			LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			IsSelected = isSelected,
			WasRemoved = memberData.Profile?.DeletionNotice is not null
		};

		context.ProfileMembers.Add(member);
		profile.Members.Add(member);

		minecraftAccount.ProfilesLastUpdated = playerUuid == requestedPlayerUuid
			? DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			: 0;

		if (member.WasRemoved == false) profile.IsDeleted = false;

		await UpdateProfileMember(profile, member, memberData);

		try {
			await context.SaveChangesAsync();
			await context.Entry(member).GetDatabaseValuesAsync();
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to save profile member {ProfileMemberId} to database", member.Id);
		}

		// Set if the profile is deleted
		if (shouldRemove && !profile.IsDeleted) {
			var updated = await context.Profiles
				.Where(p => p.ProfileId == profile.ProfileId && p.Members.All(m => m.WasRemoved))
				.ExecuteUpdateAsync(p => p.SetProperty(pr => pr.IsDeleted, true));

			if (updated > 0) await MarkProfileLeaderboardEntriesAsDeleted(profile.ProfileId);
		}
	}

	private async Task UpdateProfileMember(Profile profile, ProfileMember member, ProfileMemberResponse incomingData) {
		member.Collections = incomingData.Collection ?? member.Collections;
		member.Api.Collections = incomingData.Collection is not null;

		member.SkyblockXp = incomingData.Leveling?.Experience ?? 0;
		member.Purse = incomingData.Currencies?.CoinPurse ?? 0;
		member.PersonalBank = incomingData.Profile?.BankAccount ?? 0;
		member.Pets = mapper.Map<List<Pet>>(incomingData.PetsData?.Pets?.ToList() ?? []);
		member.Sacks = incomingData.Inventories?.SackContents.Where(kv => kv.Value > 0)
			.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, long>();

		member.Unparsed = new UnparsedApiData {
			Copper = incomingData.Garden?.Copper ?? 0,
			Consumed = new Dictionary<string, int>(),
			LevelCaps = new Dictionary<string, int> {
				{ "farming", incomingData.Jacob?.Perks?.FarmingLevelCap ?? 0 },
				{ "taming", incomingData.PetsData?.PetCare?.PetTypesSacrificed.Count ?? 0 }
			},
			Perks = incomingData.PlayerData?.Perks ?? new Dictionary<string, int>(),
			TempStatBuffs = incomingData.PlayerData?.TempStatBuffs ?? [],
			AccessoryBagSettings = incomingData.AccessoryBagSettings ?? new RawAccessoryBagStorage(),
			Bestiary = incomingData.Bestiary ?? new RawBestiaryResponse(),
			Dungeons = incomingData.Dungeons ?? new RawDungeonsResponse(),
			Essence = new Dictionary<string, int> {
				{ "WITHER", incomingData.Currencies?.Essence?.Wither?.Current ?? 0 },
				{ "DRAGON", incomingData.Currencies?.Essence?.Dragon?.Current ?? 0 },
				{ "DIAMOND", incomingData.Currencies?.Essence?.Diamond?.Current ?? 0 },
				{ "SPIDER", incomingData.Currencies?.Essence?.Spider?.Current ?? 0 },
				{ "UNDEAD", incomingData.Currencies?.Essence?.Undead?.Current ?? 0 },
				{ "ICE", incomingData.Currencies?.Essence?.Ice?.Current ?? 0 },
				{ "GOLD", incomingData.Currencies?.Essence?.Gold?.Current ?? 0 },
				{ "CRIMSON", incomingData.Currencies?.Essence?.Crimson?.Current ?? 0 }
			}
		};

		member.Slayers = incomingData.Slayer?.ToDto();

		if (incomingData.Garden?.LarvaConsumed is not null)
			member.Unparsed.Consumed.Add("wriggling_larva", incomingData.Garden.LarvaConsumed);

		if (incomingData.Events?.Easter?.RefinedDarkCacaoTrufflesConsumed is not null)
			member.Unparsed.Consumed.Add("refined_dark_cacao_truffles",
				incomingData.Events.Easter.RefinedDarkCacaoTrufflesConsumed);

		member.ParseJacob(incomingData.Jacob);

		await context.SaveChangesAsync();

		member.ParseSkills(incomingData);
		member.ParseCollectionTiers(incomingData.PlayerData?.UnlockedCollTiers);

		if (incomingData.Events?.Easter is not null) {
			member.ParseChocolateFactory(incomingData.Events.Easter, _cfSettings);
			context.ChocolateFactories.Update(member.ChocolateFactory);
		}

		ParseMemberInventories(member, incomingData);

		await AddTimeScaleRecords(member);

		await member.ParseFarmingWeight(profile.CraftedMinions, incomingData);

		// Load progress for all active events (if any)
		if (member.EventEntries is { Count: > 0 })
			try {
				foreach (var entry in member.EventEntries.Where(entry => entry.IsEventRunning())) {
					var real = await context.EventMembers
						.Include(e => e.Team)
						.FirstOrDefaultAsync(e => e.Id == entry.Id);
					var @event = await context.Events.FindAsync(entry.EventId);

					if (real is null || @event is null) continue;

					real.LoadProgress(context, member, @event);
				}
			}
			catch (Exception e) {
				logger.LogError(e, "Failed to load event progress for {PlayerUuid} in {ProfileId}", member.PlayerUuid,
					member.ProfileId);
			}

		context.Farming.Update(member.Farming);
		context.Entry(member.JacobData).State = EntityState.Modified;
		context.JacobData.Update(member.JacobData);
		context.Profiles.Update(profile);

		try {
			var networth = await GetNetworthBreakdownAsync(member);
			member.Networth = networth.Networth;
			member.LiquidNetworth = networth.LiquidNetworth;
			member.FunctionalNetworth = networth.FunctionalNetworth;
			member.LiquidFunctionalNetworth = networth.LiquidFunctionalNetworth;
		} catch (Exception e) {
			logger.LogError(e, "Failed to calculate networth for {PlayerUuid} in {ProfileId}", member.PlayerUuid,
				member.ProfileId);
		}

		await context.SaveChangesAsync();

		// Runs on background service
		await ParseJacobContests(member.PlayerUuid, member.ProfileId, member.Id, incomingData.Jacob);

		await lbService.UpdateMemberLeaderboardsAsync(member, CancellationToken.None);
	}

	private async Task ParseJacobContests(string playerUuid, string profileUuid, Guid memberId,
		RawJacobData? incomingData) {
		var data = new JobDataMap {
			{ "AccountId", playerUuid },
			{ "ProfileId", profileUuid },
			{ "MemberId", memberId },
			{ "Jacob", incomingData ?? new RawJacobData() }
		};

		var scheduler = await schedulerFactory.GetScheduler();
		await scheduler.TriggerJob(ProcessContestsBackgroundJob.Key, data);
	}

	private void ParseMemberInventories(ProfileMember member, ProfileMemberResponse incomingData) {
		ParseInventory("inventory", incomingData.Inventories?.InventoryContents?.Data, member);
		ParseInventory("ender_chest", incomingData.Inventories?.EnderChestContents?.Data, member);
		ParseInventory("personal_vault", incomingData.Inventories?.PersonalVaultContents?.Data, member);
		ParseInventory("armor", incomingData.Inventories?.Armor?.Data, member);
		ParseInventory("equipment", incomingData.Inventories?.EquipmentContents?.Data, member);
		ParseInventory("wardrobe", incomingData.Inventories?.WardrobeContents?.Data, member,
			meta: new Dictionary<string, string>() {
				{ "equipped_slot", (incomingData.Inventories?.WardrobeEquippedSlot ?? 0).ToString() }
			});
		ParseInventory("talisman_bag", incomingData.Inventories?.BagContents?.TalismanBag?.Data, member);
		ParseInventory("potion_bag", incomingData.Inventories?.BagContents?.PotionBag?.Data, member);
		ParseInventory("fishing_bag", incomingData.Inventories?.BagContents?.FishingBag?.Data, member);
		ParseInventory("sacks_bag", incomingData.Inventories?.BagContents?.SacksBag?.Data, member);
		ParseInventory("quiver", incomingData.Inventories?.BagContents?.Quiver?.Data, member);

		if (incomingData.Inventories?.BackpackContents is not null) {
			foreach (var (backpack, contents) in incomingData.Inventories.BackpackContents) {
				ParseInventory($"backpack_{backpack}", contents.Data, member);
			}
		}

		if (incomingData.Inventories?.BackpackIcons is not null) {
			var hash = HashUtility.ComputeSha256Hash(string.Join(",",
				incomingData.Inventories.BackpackIcons.Select(i => i.Value.Data)));

			var existing = member.Inventories.FirstOrDefault(i => i.Name == "icons_backpack");
			if (existing is not null) {
				if (existing.Hash == hash && !existing.HypixelInventoryId.ExtractUnixSeconds().OlderThanDays(2)) return;
				context.HypixelInventory.Remove(existing);
				member.Inventories.Remove(existing);
			}

			var items = new List<ItemDto>();

			foreach (var (backpack, icon) in incomingData.Inventories.BackpackIcons) {
				var newItems = NbtParser.NbtToItems(icon.Data);
				if (newItems.Count == 0) continue;
				var iconItem = newItems.FirstOrDefault(i => i?.SkyblockId is not null);
				if (iconItem is null) continue;

				iconItem.Slot = backpack.ToString();
				iconItem.ToHypixelItem();
				items.Add(iconItem);
			}

			if (items.Count > 0) {
				var iconInventory = new HypixelInventory {
					Name = "icons_backpack",
					Hash = hash,
					Items = items.Select(i => i.ToHypixelItem()).ToList(),
					ProfileMemberId = member.Id
				};

				member.Inventories.Add(iconInventory);
			}
		}
	}

	private void ParseInventory(string name, string? data, ProfileMember member,
		Dictionary<string, string>? meta = null) {
		var hash = HashUtility.ComputeSha256Hash(data ?? string.Empty);

		// Remove existing inventory if we have new data, or it hasn't been updated in a while
		var existing = member.Inventories.FirstOrDefault(i => i.Name == name);
		if (existing is not null) {
			if (existing.Hash == hash && !existing.HypixelInventoryId.ExtractUnixSeconds().OlderThanDays(2)) return;
			context.HypixelInventory.Remove(existing);
			member.Inventories.Remove(existing);
		}

		var inventory = NbtParser.ParseInventory(name, data);
		if (inventory is null) return;

		if (meta is not null) {
			inventory.Metadata = meta;
		}

		inventory.ProfileMemberId = member.Id;
		member.Inventories.Add(inventory);
	}

	private async Task AddTimeScaleRecords(ProfileMember member) {
		if (member.Api.Collections || member.Farming.TotalWeight > _farmingWeightOptions.MinimumWeightForTracking) {
			var cropCollection = new CropCollection {
				Time = DateTimeOffset.UtcNow,
				ProfileMemberId = member.Id,
				ProfileMember = member,

				Cactus = member.Collections.GetValueOrDefault(CropId.Cactus),
				Carrot = member.Collections.GetValueOrDefault(CropId.Carrot),
				CocoaBeans = member.Collections.GetValueOrDefault(CropId.CocoaBeans),
				Melon = member.Collections.GetValueOrDefault(CropId.Melon),
				Mushroom = member.Collections.GetValueOrDefault(CropId.Mushroom),
				NetherWart = member.Collections.GetValueOrDefault(CropId.NetherWart),
				Potato = member.Collections.GetValueOrDefault(CropId.Potato),
				Pumpkin = member.Collections.GetValueOrDefault(CropId.Pumpkin),
				SugarCane = member.Collections.GetValueOrDefault(CropId.SugarCane),
				Wheat = member.Collections.GetValueOrDefault(CropId.Wheat),
				Seeds = member.Collections.GetValueOrDefault(CropId.Seeds),

				Beetle = member.Farming.Pests.Beetle,
				Cricket = member.Farming.Pests.Cricket,
				Fly = member.Farming.Pests.Fly,
				Locust = member.Farming.Pests.Locust,
				Mite = member.Farming.Pests.Mite,
				Mosquito = member.Farming.Pests.Mosquito,
				Moth = member.Farming.Pests.Moth,
				Rat = member.Farming.Pests.Rat,
				Slug = member.Farming.Pests.Slug,
				Earthworm = member.Farming.Pests.Earthworm,
				Mouse = member.Farming.Pests.Mouse
			};

			await context.BulkInsertAsync([cropCollection]);
		}

		if (member.Api.Skills) {
			var skillExp = new SkillExperience {
				Time = DateTimeOffset.UtcNow,

				Alchemy = member.Skills.Alchemy,
				Carpentry = member.Skills.Carpentry,
				Combat = member.Skills.Combat,
				Enchanting = member.Skills.Enchanting,
				Farming = member.Skills.Farming,
				Fishing = member.Skills.Fishing,
				Foraging = member.Skills.Foraging,
				Mining = member.Skills.Mining,
				Runecrafting = member.Skills.Runecrafting,
				Taming = member.Skills.Taming,
				Social = member.Skills.Social,

				ProfileMemberId = member.Id,
				ProfileMember = member
			};

			await context.BulkInsertAsync([skillExp]);
		}
	}
}
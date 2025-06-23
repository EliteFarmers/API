using System.Text.Json.Nodes;
using EFCore.BulkExtensions;
using EliteAPI.Background.Profiles;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Features.Resources.Items.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Events;
using EliteAPI.Parsers.Farming;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Features.Profiles.Services;

public interface IProfileProcessorService {
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
	Task<(Profile? profile, Dictionary<string, ProfileMemberResponse> members)> ProcessProfileData(ProfileResponse profileData, string? requestedPlayerUuid);
	
	Task UpdateGardenData(string profileId);

	/// <summary>
	/// Processes a member from the Hypixel API
	/// </summary>
	/// <param name="profile">The profile to process</param>
	/// <param name="memberData">The member data to process</param>
	/// <param name="playerUuid">The player uuid of the member</param>
	/// <param name="requestedPlayerUuid">The player uuid that was requested to get this data</param>
	/// <param name="profileData">Raw profile data</param>
	Task ProcessMemberData(Profile profile, ProfileMemberResponse memberData, string playerUuid, string requestedPlayerUuid, ProfileResponse? profileData = null);
}

[RegisterService<IProfileProcessorService>(LifeTime.Scoped)]
public class ProfileProcessorService(
	DataContext context,
	ILogger<ProfileProcessorService> logger,
	IMojangService mojangService,
	IMessageService messageService,
	ILbService lbService,
	ILeaderboardService leaderboardService,
	ISchedulerFactory schedulerFactory,
	IOptions<ChocolateFactorySettings> cfOptions,
	IOptions<ConfigCooldownSettings> coolDowns,
	IOptions<ConfigFarmingWeightSettings> farmingWeightOptions,
	AutoMapper.IMapper mapper
	) : IProfileProcessorService 
{
	private readonly ChocolateFactorySettings _cfSettings = cfOptions.Value;
	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
	private readonly ConfigFarmingWeightSettings _farmingWeightOptions = farmingWeightOptions.Value;
	
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
				.AsSplitQuery()
				.FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
		);

	public async Task<List<ProfileResponse>> ProcessProfilesResponse(ProfilesResponse data, string? requestedPlayerUuid) {
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
            
            if (wiped.Profile.GameMode != "bingo" && !wiped.WasRemoved) {
                messageService.SendWipedMessage(
	                wiped.PlayerUuid, 
	                wiped.MinecraftAccount.Name, 
	                wiped.ProfileId, 
	                wiped.MinecraftAccount.AccountId?.ToString() ?? "");
            }

            // Ensure member is marked as deleted
            if (!wiped.WasRemoved || wiped.IsSelected) {
	            await context.ProfileMembers
		            .Where(m => m.Id == wiped.Id)
		            .ExecuteUpdateAsync(member => member
			            .SetProperty(m => m.WasRemoved, true)
			            .SetProperty(m => m.IsSelected, false)
		            );
            }

            // Ensure profile is marked as deleted if all members are removed
            if (!wiped.Profile.IsDeleted) {
	            var count = await context.Profiles
		            .Where(p => p.ProfileId == wiped.ProfileId && p.Members.All(m => m.WasRemoved))
		            .ExecuteUpdateAsync(p => p.SetProperty(pr => pr.IsDeleted, true));

	            // Mark profile leaderboard entries as removed if the profile was updated
	            if (count > 0) {
		            await MarkProfileLeaderboardEntriesAsDeleted(wiped.ProfileId);
	            }
            } else {
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

	public async Task<(Profile? profile, Dictionary<string, ProfileMemberResponse> members)> ProcessProfileData(ProfileResponse profileData, string? requestedPlayerUuid) {
		var members = profileData.Members.ToDictionary(
			pair => pair.Key.Replace("-", ""), // Strip hyphens from UUIDs
			pair => pair.Value);
        if (members.Count == 0) return (null, members);

        var profileId = profileData.ProfileId.Replace("-", "");
        var existing = await context.Profiles
            .Include(p => p.Garden)
            .FirstOrDefaultAsync(p => p.ProfileId == profileId);

        var profile = existing ?? new Profile
        {
            ProfileId = profileId,
            ProfileName = profileData.CuteName,
            GameMode = profileData.GameMode,
            Members = [],
            IsDeleted = false
        };
        
        profile.BankBalance = profileData.Banking?.Balance ?? 0.0;

        foreach (var member in members.Values) {
	        profile.CombineMinions(member.PlayerData?.CraftedGenerators);
        }

        if (existing is not null) { 
	        profile.GameMode = profileData.GameMode;
            profile.ProfileName = profileData.CuteName;
        } else {
	        context.Profiles.Add(profile);
        }
        
        try {
            await context.SaveChangesAsync();
        } catch (Exception e) {
            logger.LogError(e, "Failed to save profile {ProfileId} to database", profileId);
        }
        
        await lbService.UpdateProfileLeaderboardsAsync(profile, CancellationToken.None);
        
        if (existing?.Garden is null || existing.Garden.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockGardenCooldown)) 
        {
	        await UpdateGardenData(profileId);
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

	public async Task ProcessMemberData(Profile profile, ProfileMemberResponse memberData, string playerUuid, string requestedPlayerUuid, ProfileResponse? profileData = null) 
	{
		var existing = await _fetchProfileMemberData(context, playerUuid, profile.ProfileId);
        
        // Should remove if deleted or coop invitation is not accepted
        var shouldRemove = memberData.Profile?.DeletionNotice is not null || memberData.Profile?.CoopInvitation is { Confirmed: false };
        
        // Exit early if removed, and still should be removed
        // This means that we already processed the member when it was removed, and the data is still the same
        if (existing?.WasRemoved == true && shouldRemove) return;
        
        var isSelected = profileData?.Selected is true && playerUuid == requestedPlayerUuid;
        
        if (existing is not null)
        {
            existing.WasRemoved = shouldRemove;

            if (shouldRemove) {
                existing.IsSelected = false;
                
                // Remove leaderboard positions
                await leaderboardService.RemoveMemberFromAllLeaderboards(existing.Id.ToString());
                
                messageService.SendWipedMessage(
                    playerUuid, 
                    existing.MinecraftAccount.Name ?? "", 
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
                Name = existing.MinecraftAccount.Name ?? playerUuid,
                Uuid = existing.MinecraftAccount.Id,
                Profile = profile.ProfileName,
                ProfileUuid = profile.ProfileId,
                SkyblockExperience = existing.SkyblockXp
            };
            
            existing.Metadata.Name = existing.MinecraftAccount.Name ?? playerUuid;
            existing.Metadata.Profile = profile.ProfileName;
            existing.Metadata.SkyblockExperience = existing.SkyblockXp;
            
            existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            existing.MinecraftAccount.ProfilesLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            context.MinecraftAccounts.Update(existing.MinecraftAccount);
            context.Entry(existing).State = EntityState.Modified;

            if (existing.WasRemoved == false) {
                profile.IsDeleted = false;
            }
            
            await UpdateProfileMember(profile, existing, memberData);
            
            return;
        }
        
        var minecraftAccount = await mojangService.GetMinecraftAccountByUuid(playerUuid);
        if (minecraftAccount is null) return;

        var member = new ProfileMember
        {
            Id = Guid.NewGuid(),
            PlayerUuid = playerUuid,
            
            Profile = profile,
            ProfileId = profile.ProfileId,
            ProfileName = profile.ProfileName,
            
            Metadata = new ProfileMemberMetadata {
                Name = playerUuid,
                Uuid = minecraftAccount.Id,
                Profile = profile.ProfileName,
                ProfileUuid = profile.ProfileId,
            },

            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsSelected = isSelected,
            WasRemoved = memberData.Profile?.DeletionNotice is not null
        };
        
        context.ProfileMembers.Add(member);
        profile.Members.Add(member);

        minecraftAccount.ProfilesLastUpdated = playerUuid == requestedPlayerUuid 
            ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : 0;

        if (member.WasRemoved == false) {
            profile.IsDeleted = false;
        }

        await UpdateProfileMember(profile, member, memberData);

        try
        {
            await context.SaveChangesAsync();
            await context.Entry(member).GetDatabaseValuesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save profile member {ProfileMemberId} to database", member.Id);
        }

        // Set if the profile is deleted
        if (shouldRemove && !profile.IsDeleted) {
            var updated = await context.Profiles
                .Where(p => p.ProfileId == profile.ProfileId && p.Members.All(m => m.WasRemoved))
                .ExecuteUpdateAsync(p => p.SetProperty(pr => pr.IsDeleted, true));
            
            if (updated > 0) {
				await MarkProfileLeaderboardEntriesAsDeleted(profile.ProfileId);
            }
        }
    }

    private async Task UpdateProfileMember(Profile profile, ProfileMember member, ProfileMemberResponse incomingData) {
        var previousApi = new ApiAccess {
            Collections = member.Api.Collections,
            Inventories = member.Api.Inventories,
            Skills = member.Api.Skills
        };
        
        member.Collections = incomingData.Collection ?? member.Collections;
        member.Api.Collections = incomingData.Collection is not null;
        
        member.SkyblockXp = incomingData.Leveling?.Experience ?? 0;
        member.Purse = incomingData.Currencies?.CoinPurse ?? 0;
        member.Pets = mapper.Map<List<Pet>>(incomingData.PetsData?.Pets?.ToList() ?? []);
        
        member.Unparsed = new UnparsedApiData
        {
            Copper = incomingData.Garden?.Copper ?? 0,
            Consumed = new Dictionary<string, int>(),
            LevelCaps = new Dictionary<string, int>() {
                { "farming", incomingData.Jacob?.Perks?.FarmingLevelCap ?? 0 },
                { "taming", incomingData.PetsData?.PetCare?.PetTypesSacrificed.Count ?? 0 }
            },
            Perks = incomingData.PlayerData?.Perks ?? new Dictionary<string, int>(),
            TempStatBuffs = incomingData.PlayerData?.TempStatBuffs ?? [],
            AccessoryBagSettings = incomingData.AccessoryBagSettings ?? new JsonObject(),
            Bestiary = incomingData.Bestiary ?? new JsonObject()
        };

        member.Stats ??= new MemberStats();
        member.Stats.UnqiueShards = incomingData.PlayerStats?.UniqueShards ?? 0;
        member.Stats.Shards = incomingData.PlayerStats?.AppliedShards ?? member.Stats.Shards;

        if (incomingData.Garden?.LarvaConsumed is not null) {
            member.Unparsed.Consumed.Add("wriggling_larva", incomingData.Garden.LarvaConsumed);
        }
        
        if (incomingData.Events?.Easter?.RefinedDarkCacaoTrufflesConsumed is not null) {
            member.Unparsed.Consumed.Add("refined_dark_cacao_truffles", incomingData.Events.Easter.RefinedDarkCacaoTrufflesConsumed);
        }
        
        member.ParseJacob(incomingData.Jacob);
        
        await context.SaveChangesAsync();
        
        member.ParseSkills(incomingData);
        member.ParseCollectionTiers(incomingData.PlayerData?.UnlockedCollTiers);
        
        if (incomingData.Events?.Easter is not null) {
            member.ParseChocolateFactory(incomingData.Events.Easter, _cfSettings);
            context.ChocolateFactories.Update(member.ChocolateFactory);
        }

        await AddTimeScaleRecords(member);
        
        await member.ParseFarmingWeight(profile.CraftedMinions, incomingData);

        // Load progress for all active events (if any)
        if (member.EventEntries is { Count: > 0 }) {
            try {
                foreach (var entry in member.EventEntries.Where(entry => entry.IsEventRunning())) {
                    var real = await context.EventMembers
                        .Include(e => e.Team)
                        .FirstOrDefaultAsync(e => e.Id == entry.Id);
                    var @event = await context.Events.FindAsync(entry.EventId);
                
                    if (real is null || @event is null) continue;
                
                    real.LoadProgress(context, member, @event);
                }
            } catch (Exception e) {
                logger.LogError(e, "Failed to load event progress for {PlayerUuid} in {ProfileId}", member.PlayerUuid, member.ProfileId);
            }
        }

        context.Farming.Update(member.Farming);
        context.Entry(member.JacobData).State = EntityState.Modified;
        context.JacobData.Update(member.JacobData);
        context.Profiles.Update(profile);
        
        await context.SaveChangesAsync();
        
        // Runs on background service
        await ParseJacobContests(member.PlayerUuid, member.ProfileId, member.Id, incomingData.Jacob);

        UpdateLeaderboards(member, previousApi);

        await lbService.UpdateMemberLeaderboardsAsync(member, CancellationToken.None);
    }
    
    private void UpdateLeaderboards(ProfileMember member, ApiAccess previousApi) {
        var memberId = member.Id.ToString();
        
        // Update misc leaderboards
        leaderboardService.UpdateLeaderboardScore("farmingweight", memberId, member.Farming.TotalWeight);
        leaderboardService.UpdateLeaderboardScore("skyblockxp", memberId, member.SkyblockXp);

        // Might want to do this later, but for now the leaderboard queries don't check API access
        // So removing members from leaderboards if they disable the API is not a good idea
        
        // If collections api was turned off, remove scores
        // if (previousApi.Collections && !member.Api.Collections) {
        //     await leaderboardService.RemoveMemberFromLeaderboards(_lbSettings.CollectionLeaderboards.Keys, memberId);
        // }
        //
        // If skills api was turned off, remove scores
        // if (previousApi.Skills && !member.Api.Skills) {
        //     await leaderboardService.RemoveMemberFromLeaderboards(_lbSettings.SkillLeaderboards.Keys, memberId);
        // }
        
        // Update pest leaderboards
        leaderboardService.UpdateLeaderboardScore("mite", memberId, member.Farming.Pests.Mite);
        leaderboardService.UpdateLeaderboardScore("cricket", memberId, member.Farming.Pests.Cricket);
        leaderboardService.UpdateLeaderboardScore("moth", memberId, member.Farming.Pests.Moth);
        leaderboardService.UpdateLeaderboardScore("earthworm", memberId, member.Farming.Pests.Earthworm);
        leaderboardService.UpdateLeaderboardScore("slug", memberId, member.Farming.Pests.Slug);
        leaderboardService.UpdateLeaderboardScore("beetle", memberId, member.Farming.Pests.Beetle);
        leaderboardService.UpdateLeaderboardScore("locust", memberId, member.Farming.Pests.Locust);
        leaderboardService.UpdateLeaderboardScore("rat", memberId, member.Farming.Pests.Rat);
        leaderboardService.UpdateLeaderboardScore("mosquito", memberId, member.Farming.Pests.Mosquito);
        leaderboardService.UpdateLeaderboardScore("fly", memberId, member.Farming.Pests.Fly);
        leaderboardService.UpdateLeaderboardScore("mouse", memberId, member.Farming.Pests.Mouse);
        
        // Update chocolate factory leaderboards
        leaderboardService.UpdateLeaderboardScore("chocolate", memberId, member.ChocolateFactory.TotalChocolate);
    }

    private async Task ParseJacobContests(string playerUuid, string profileUuid, Guid memberId, RawJacobData? incomingData) 
    {
        var data = new JobDataMap {
            { "AccountId", playerUuid },
            { "ProfileId", profileUuid },
            { "MemberId", memberId },
            { "Jacob", incomingData ?? new RawJacobData() }
        };

        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.TriggerJob(ProcessContestsBackgroundJob.Key, data);
    }
    
    private async Task AddTimeScaleRecords(ProfileMember member) {
        if (member.Api.Collections || member.Farming.TotalWeight > _farmingWeightOptions.MinimumWeightForTracking) {
            var cropCollection = new CropCollection {
                Time = DateTimeOffset.UtcNow,
                ProfileMemberId = member.Id,
                ProfileMember = member,
            
                Cactus = member.Collections.RootElement.TryGetProperty("CACTUS", out var cactus) ? cactus.GetInt64() : 0,
                Carrot = member.Collections.RootElement.TryGetProperty("CARROT_ITEM", out var carrot) ? carrot.GetInt64() : 0,
                CocoaBeans = member.Collections.RootElement.TryGetProperty("INK_SACK:3", out var cocoa) ? cocoa.GetInt64() : 0,
                Melon = member.Collections.RootElement.TryGetProperty("MELON", out var melon) ? melon.GetInt64() : 0,
                Mushroom = member.Collections.RootElement.TryGetProperty("MUSHROOM_COLLECTION", out var mushroom) ? mushroom.GetInt64() : 0,
                NetherWart = member.Collections.RootElement.TryGetProperty("NETHER_STALK", out var netherWart) ? netherWart.GetInt64() : 0,
                Potato = member.Collections.RootElement.TryGetProperty("POTATO_ITEM", out var potato) ? potato.GetInt64() : 0,
                Pumpkin = member.Collections.RootElement.TryGetProperty("PUMPKIN", out var pumpkin) ? pumpkin.GetInt64() : 0,
                SugarCane = member.Collections.RootElement.TryGetProperty("SUGAR_CANE", out var sugarCane) ? sugarCane.GetInt64() : 0,
                Wheat = member.Collections.RootElement.TryGetProperty("WHEAT", out var wheat) ? wheat.GetInt64() : 0,
                Seeds = member.Collections.RootElement.TryGetProperty("SEEDS", out var seeds) ? seeds.GetInt64() : 0,
                
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
            
            await context.BulkInsertAsync([ cropCollection ]);

            // Update leaderboard positions
            var memberId = member.Id.ToString();
            leaderboardService.UpdateLeaderboardScore("cactus", memberId, cropCollection.Cactus);
            leaderboardService.UpdateLeaderboardScore("carrot", memberId, cropCollection.Carrot);
            leaderboardService.UpdateLeaderboardScore("cocoa", memberId, cropCollection.CocoaBeans);
            leaderboardService.UpdateLeaderboardScore("melon", memberId, cropCollection.Melon);
            leaderboardService.UpdateLeaderboardScore("mushroom", memberId, cropCollection.Mushroom);
            leaderboardService.UpdateLeaderboardScore("netherwart", memberId, cropCollection.NetherWart);
            leaderboardService.UpdateLeaderboardScore("potato", memberId, cropCollection.Potato);
            leaderboardService.UpdateLeaderboardScore("pumpkin", memberId, cropCollection.Pumpkin);
            leaderboardService.UpdateLeaderboardScore("sugarcane", memberId, cropCollection.SugarCane);
            leaderboardService.UpdateLeaderboardScore("wheat", memberId, cropCollection.Wheat);
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
                ProfileMember = member,
            };
            
            await context.BulkInsertAsync([ skillExp ]);
            
            // Update leaderboard positions
            var memberId = member.Id.ToString();
            leaderboardService.UpdateLeaderboardScore("alchemy", memberId, skillExp.Alchemy);
            leaderboardService.UpdateLeaderboardScore("carpentry", memberId, skillExp.Carpentry);
            leaderboardService.UpdateLeaderboardScore("combat", memberId, skillExp.Combat);
            leaderboardService.UpdateLeaderboardScore("enchanting", memberId, skillExp.Enchanting);
            leaderboardService.UpdateLeaderboardScore("farming", memberId, skillExp.Farming);
            leaderboardService.UpdateLeaderboardScore("fishing", memberId, skillExp.Fishing);
            leaderboardService.UpdateLeaderboardScore("mining", memberId, skillExp.Mining);
            leaderboardService.UpdateLeaderboardScore("runecrafting", memberId, skillExp.Runecrafting);
            leaderboardService.UpdateLeaderboardScore("taming", memberId, skillExp.Taming);
            leaderboardService.UpdateLeaderboardScore("social", memberId, skillExp.Social);
        }
    }
}
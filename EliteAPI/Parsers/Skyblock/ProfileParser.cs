using System.Text.Json.Nodes;
using AutoMapper;
using EliteAPI.Background.Profiles;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using Microsoft.EntityFrameworkCore;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Farming;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Parsers.Events;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using HypixelAPI.DTOs;
using Microsoft.Extensions.Options;
using Quartz;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;

namespace EliteAPI.Parsers.Skyblock;

public class ProfileParser(
    DataContext context, 
    IMojangService mojangService,
    IOptions<ConfigLeaderboardSettings> lbOptions,
    IOptions<ChocolateFactorySettings> cfOptions,
    IOptions<ConfigCooldownSettings> coolDowns,
    ILogger<ProfileParser> logger,
    ILeaderboardService leaderboardService,
    ISchedulerFactory schedulerFactory,
    IMessageService messageService,
    IMapper mapper) 
{
    private readonly ConfigLeaderboardSettings _lbSettings = lbOptions.Value;
    private readonly ChocolateFactorySettings _cfSettings = cfOptions.Value;
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    
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
                .AsSplitQuery()
                .FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
        );

    public async Task TransformProfilesResponse(ProfilesResponse data, string? playerUuid) {
        if (!data.Success) {
            logger.LogWarning("Received unsuccessful profiles response from {PlayerUuid}", playerUuid);
            return;
        }

        if (data.Profiles is not { Length: > 0 }) {
            // Mark player as removed from all of their profiles if they have none in the response
            await context.ProfileMembers
                .Include(p => p.Profile)
                .Where(p => p.PlayerUuid.Equals(playerUuid))
                .ExecuteUpdateAsync(member =>
                    member
                        .SetProperty(m => m.WasRemoved, true)
                        .SetProperty(m => m.IsSelected, false)
                );
            return;
        }

        // Parse each profile
        foreach (var profile in data.Profiles) {
            await TransformSingleProfile(profile, playerUuid);
        }

        var profileIds = data.Profiles.Select(p => p.ProfileId.Replace("-", "")).ToList();

        // Get profiles that aren't in the response
        var wipedProfiles = await context.ProfileMembers
            .Include(p => p.Profile)
            .Include(p => p.MinecraftAccount)
            .Where(p => p.PlayerUuid.Equals(playerUuid) && !profileIds.Contains(p.ProfileId))
            .Select(p => new { p.Id, p.WasRemoved, p.PlayerUuid, p.ProfileId, Ign = p.MinecraftAccount.Name, DiscordId = p.MinecraftAccount.AccountId })
            .ToListAsync();
        
        if (wipedProfiles.Count > 0) {
            // Send wiped messages
            foreach (var p in wipedProfiles) {
                if (p.WasRemoved) continue;
                messageService.SendWipedMessage(p.PlayerUuid, p.Ign, p.ProfileId, p.DiscordId?.ToString() ?? "");
            }
            
            // Mark all as removed
            await context.ProfileMembers
                .Include(p => p.Profile)
                .Where(p => p.PlayerUuid.Equals(playerUuid) && !profileIds.Contains(p.ProfileId))
                .ExecuteUpdateAsync(member =>
                    member
                        .SetProperty(m => m.WasRemoved, true)
                        .SetProperty(m => m.IsSelected, false)
                );
        }
        
        await context.SaveChangesAsync();
    }

    private async Task TransformSingleProfile(ProfileResponse profile, string? playerUuid)
    {
        var members = profile.Members;
        if (members.Count == 0) return;

        var profileId = profile.ProfileId.Replace("-", "");
        var existing = await context.Profiles
            .Include(p => p.Garden)
            .FirstOrDefaultAsync(p => p.ProfileId == profileId);

        var profileObj = existing ?? new Profile
        {
            ProfileId = profileId,
            ProfileName = profile.CuteName,
            GameMode = profile.GameMode,
            Members = [],
            IsDeleted = false
        };
        
        profileObj.BankBalance = profile.Banking?.Balance ?? 0.0;

        if (existing is not null)
        {
            profileObj.GameMode = profile.GameMode;
            profileObj.ProfileName = profile.CuteName;
            profileObj.IsDeleted = false;
        }
        else
        {
            try
            {
                context.Profiles.Add(profileObj);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to add profile {ProfileId} to database", profileId);
            }
        }

        foreach (var (key, memberData) in members)
        {
            // Hyphens shouldn't be included anyway, but just in case Hypixel pulls another fast one
            var playerId = key.Replace("-", "");

            var selected = playerUuid?.Equals(playerId) == true && profile.Selected;
            await TransformMemberResponse(playerId, memberData, profileObj, selected, playerUuid ?? "Unknown");
        }

        if (existing?.Garden is null) {
            await UpdateGardenData(profileId);
        } else if (existing.Garden.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockGardenCooldown)) {
            await UpdateGardenData(profileId);
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to save profile {ProfileId} to database", profileId);
        }
    }

    private async Task TransformMemberResponse(string playerId, ProfileMemberResponse memberData, Profile profile, bool selected, string requesterUuid)
    {
        var existing = await _fetchProfileMemberData(context, playerId, profile.ProfileId);
        
        // Should remove if deleted or co op invitation is not accepted
        var shouldRemove = memberData.Profile?.DeletionNotice is not null || memberData.Profile?.CoopInvitation is { Confirmed: false };
        
        // Exit early if removed, and still should be removed
        if (existing?.WasRemoved == true && shouldRemove) return;
        
        if (existing is not null)
        {
            if (shouldRemove) {
                // Remove leaderboard positions
                await leaderboardService.RemoveMemberFromAllLeaderboards(existing.Id.ToString());
                
                messageService.SendWipedMessage(
                    playerId, 
                    existing.MinecraftAccount.Name ?? "", 
                    existing.ProfileId,
                    existing.MinecraftAccount.AccountId?.ToString() ?? "");
            }
            
            // Only update if the player is the requester
            if (playerId == requesterUuid) {
                existing.IsSelected = selected;
                existing.ProfileName = profile.ProfileName;
            }
            
            // Only update if null (profile names can differ between members)
            existing.ProfileName ??= profile.ProfileName;
            
            existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            existing.WasRemoved = shouldRemove;
            
            existing.MinecraftAccount.ProfilesLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            context.MinecraftAccounts.Update(existing.MinecraftAccount);
            context.Entry(existing).State = EntityState.Modified;
            
            await UpdateProfileMember(profile, existing, memberData);
            
            return;
        }
        
        var minecraftAccount = await mojangService.GetMinecraftAccountByUuid(playerId);
        if (minecraftAccount is null) return;

        var member = new ProfileMember
        {
            Id = Guid.NewGuid(),
            PlayerUuid = playerId,
            
            Profile = profile,
            ProfileId = profile.ProfileId,
            ProfileName = profile.ProfileName,

            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsSelected = selected,
            WasRemoved = memberData.Profile?.DeletionNotice is not null
        };
        
        context.ProfileMembers.Add(member);
        profile.Members.Add(member);

        minecraftAccount.ProfilesLastUpdated = playerId == requesterUuid 
            ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : 0;

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
        // Runs on background service
        await ParseJacobContests(member.PlayerUuid, member.ProfileId, member.Id, incomingData.Jacob);
        
        profile.CombineMinions(incomingData.PlayerData?.CraftedGenerators);
        
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
        context.ProfileMembers.Update(member);
        context.JacobData.Update(member.JacobData);
        context.Profiles.Update(profile);
        
        await context.SaveChangesAsync();

        await UpdateLeaderboards(member, previousApi);
    }
    
    private async Task UpdateLeaderboards(ProfileMember member, ApiAccess previousApi) {
        var memberId = member.Id.ToString();
        
        // Update misc leaderboards
        leaderboardService.UpdateLeaderboardScore("farmingweight", memberId, member.Farming.TotalWeight);
        leaderboardService.UpdateLeaderboardScore("skyblockxp", memberId, member.SkyblockXp);

        // If collections api was turned off, remove scores
        if (previousApi.Collections && !member.Api.Collections) {
            await leaderboardService.RemoveMemberFromLeaderboards(_lbSettings.CollectionLeaderboards.Keys, memberId);
        }
        
        // If skills api was turned off, remove scores
        if (previousApi.Skills && !member.Api.Skills) {
            await leaderboardService.RemoveMemberFromLeaderboards(_lbSettings.SkillLeaderboards.Keys, memberId);
        }
        
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
    
    private async Task UpdateGardenData(string profileId) 
    {
        var data = new JobDataMap {
            { "ProfileId", profileId }
        };

        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.TriggerJob(RefreshGardenBackgroundJob.Key, data);
    }

    private async Task AddTimeScaleRecords(ProfileMember member) {
        if (member.Api.Collections) {
            var cropCollection = new CropCollection {
                Time = DateTimeOffset.UtcNow,
            
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
            
                ProfileMemberId = member.Id,
                ProfileMember = member,
            };
            
            await context.CropCollections.SingleInsertAsync(cropCollection);

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
            
            await context.SkillExperiences.SingleInsertAsync(skillExp);
            
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

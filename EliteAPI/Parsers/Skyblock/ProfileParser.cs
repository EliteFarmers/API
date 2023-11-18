using System.Text.Json.Nodes;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Services.MojangService;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Events;
using Microsoft.EntityFrameworkCore;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Farming;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Parsers.Events;
using EliteAPI.Services.Background;
using EliteAPI.Services.CacheService;
using EliteAPI.Services.LeaderboardService;
using Microsoft.Extensions.Options;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;

namespace EliteAPI.Parsers.Skyblock;

public class ProfileParser(
    DataContext context, 
    IMojangService mojangService, 
    IBackgroundTaskQueue taskQueue,
    IOptions<ConfigLeaderboardSettings> lbOptions,
    ILeaderboardService leaderboardService) 
{
    private readonly ConfigLeaderboardSettings lbSettings = lbOptions.Value;
    
    private readonly Func<DataContext, string, string, Task<ProfileMember?>> _fetchProfileMemberData = 
        EF.CompileAsyncQuery((DataContext context, string playerUuid, string profileUuid) =>            
            context.ProfileMembers
                .Include(p => p.MinecraftAccount)
                .Include(p => p.Profile)
                .Include(p => p.Skills)
                .Include(p => p.Farming)
                .Include(p => p.JacobData)
                .ThenInclude(j => j.Contests)
                .ThenInclude(c => c.JacobContest)
                .Include(p => p.EventEntries)
                .AsSplitQuery()
                .FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
        );
    
    public async Task TransformProfilesResponse(RawProfilesResponse data, string? playerUuid) {
        if (!data.Success || data.Profiles is not { Length: > 0 }) return;

        foreach (var profile in data.Profiles) {
            await TransformSingleProfile(profile, playerUuid);
        }

        var profileIds = data.Profiles.Select(p => p.ProfileId.Replace("-", "")).ToList();

        // Mark player as removed from all profiles that aren't in the response
        await context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.PlayerUuid.Equals(playerUuid) && !profileIds.Contains(p.ProfileId))
            .ExecuteUpdateAsync(member =>
                member
                    .SetProperty(m => m.WasRemoved, true)
                    .SetProperty(m => m.IsSelected, false)
            );
        
        await context.SaveChangesAsync();
    }

    private async Task TransformSingleProfile(RawProfileData profile, string? playerUuid)
    {
        var members = profile.Members;
        if (members.Count == 0) return;

        var profileId = profile.ProfileId.Replace("-", "");
        var existing = await context.Profiles.FindAsync(profileId);

        var profileObj = existing ?? new Profile
        {
            ProfileId = profileId,
            ProfileName = profile.CuteName,
            GameMode = profile.GameMode,
            Members = new List<ProfileMember>(),
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
                Console.WriteLine(e);
            }
        }

        foreach (var (key, memberData) in members)
        {
            // Hyphens shouldn't be included anyways, but just in case Hypixel pulls another fast one
            var playerId = key.Replace("-", "");

            var selected = playerUuid?.Equals(playerId) == true && profile.Selected;
            await TransformMemberResponse(playerId, memberData, profileObj, selected, playerUuid ?? "Unknown");
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task TransformMemberResponse(string playerId, RawMemberData memberData, Profile profile, bool selected, string requesterUuid)
    {
        var existing = await _fetchProfileMemberData(context, playerId, profile.ProfileId);
        
        // Should remove if deleted or co op invitation is not accepted
        var shouldRemove = memberData.DeletionNotice is not null || memberData.Profile?.CoopInvitation is { Confirmed: false };
        
        // Exit early if removed, and still should be removed
        if (existing?.WasRemoved == true && shouldRemove) return;
        
        if (existing is not null)
        {
            if (shouldRemove) {
                // Remove leaderboard positions
                await leaderboardService.RemoveMemberFromAllLeaderboards(existing.Id.ToString());
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
            WasRemoved = memberData.DeletionNotice is not null
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
            Console.WriteLine(ex);
        }
    }

    private async Task UpdateProfileMember(Profile profile, ProfileMember member, RawMemberData incomingData) {
        var previousApi = new ApiAccess {
            Collections = member.Api.Collections,
            Inventories = member.Api.Inventories,
            Skills = member.Api.Skills
        };
        
        member.Collections = incomingData.Collection ?? member.Collections;
        member.Api.Collections = incomingData.Collection is not null;
        
        member.SkyblockXp = incomingData.Leveling?.Experience ?? 0;
        member.Purse = incomingData.Currencies?.CoinPurse ?? 0;
        member.Pets = incomingData.PetsData?.Pets?.ToList() ?? new List<Pet>();
        
        member.Unparsed = new UnparsedApiData
        {
            Perks = incomingData.PlayerData?.Perks ?? new Dictionary<string, int>(),
            TempStatBuffs = incomingData.PlayerData?.TempStatBuffs ?? new List<TempStatBuff>(),
            AccessoryBagSettings = incomingData.AccessoryBagSettings ?? new JsonObject(),
            Bestiary = incomingData.Bestiary ?? new JsonObject()
        };
        
        member.ParseJacob(incomingData.Jacob);
        await context.SaveChangesAsync();
        
        member.ParseSkills(incomingData);
        member.ParseCollectionTiers(incomingData.PlayerData?.UnlockedCollTiers);

        await AddTimeScaleRecords(member);
        // Runs on background service
        ParseJacobContests(member.Id, incomingData);
        
        profile.CombineMinions(incomingData.PlayerData?.CraftedGenerators);
        
        await member.ParseFarmingWeight(profile.CraftedMinions, incomingData);

        // Load progress for all active events (if any)
        if (member.EventEntries is { Count: > 0 }) {
            foreach (var entry in member.EventEntries) {
                if (!entry.IsEventRunning()) continue;
                
                var real = await context.EventMembers.FirstOrDefaultAsync(e => e.Id == entry.Id);
                var @event = await context.Events.FindAsync(entry.EventId);
                
                if (real is null || @event is null) continue;
                
                real.LoadProgress(member, @event);
                
                real.EventMemberStartConditions = new EventMemberStartConditions {
                    InitialCollection = real.EventMemberStartConditions.InitialCollection,
                    IncreasedCollection = real.EventMemberStartConditions.IncreasedCollection,
                    CountedCollection = real.EventMemberStartConditions.CountedCollection,
                    ToolStates = real.EventMemberStartConditions.ToolStates,
                    Tools = real.EventMemberStartConditions.Tools
                };
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
            await leaderboardService.RemoveMemberFromLeaderboards(lbSettings.CollectionLeaderboards.Keys, memberId);
        }
        
        // If skills api was turned off, remove scores
        if (previousApi.Skills && !member.Api.Skills) {
            await leaderboardService.RemoveMemberFromLeaderboards(lbSettings.SkillLeaderboards.Keys, memberId);
        }
    }

    private void ParseJacobContests(Guid memberId, RawMemberData incomingData) {
        // Defer jacob contest parsing to the background task queue
        taskQueue.EnqueueAsync(async (scope, ct) => {
            await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var lbService = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();
        
            var member = await context.ProfileMembers
                .Include(p => p.JacobData)
                .ThenInclude(j => j.Contests)
                .ThenInclude(c => c.JacobContest)
                .FirstOrDefaultAsync(p => p.Id == memberId, cancellationToken: ct);
            if (member is null) return;

            await member.ParseJacobContests(incomingData.Jacob, context, cache);

            // Update leaderboard positions
            var mId = memberId.ToString();
            lbService.UpdateLeaderboardScore("participations", mId, member.JacobData.Participations);
            lbService.UpdateLeaderboardScore("firstplace", mId, member.JacobData.FirstPlaceScores);
            
            lbService.UpdateLeaderboardScore("diamondmedals", mId, member.JacobData.EarnedMedals.Diamond);
            lbService.UpdateLeaderboardScore("platinummedals", mId, member.JacobData.EarnedMedals.Platinum);
            lbService.UpdateLeaderboardScore("goldmedals", mId, member.JacobData.EarnedMedals.Gold);
            lbService.UpdateLeaderboardScore("silvermedals", mId, member.JacobData.EarnedMedals.Silver);
            lbService.UpdateLeaderboardScore("bronzemedals", mId, member.JacobData.EarnedMedals.Bronze);
        });
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

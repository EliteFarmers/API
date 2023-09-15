using System.Text.Json.Nodes;
using EliteAPI.Data;
using EliteAPI.Services.MojangService;
using EliteAPI.Models.DTOs.Incoming;
using Microsoft.EntityFrameworkCore;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Farming;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Parsers.Events;
using EliteAPI.Services.CacheService;
using EliteAPI.Services.LeaderboardService;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;

namespace EliteAPI.Parsers.Skyblock;

public class ProfileParser
{
    private readonly DataContext _context;
    private readonly IMojangService _mojangService;
    private readonly ILeaderboardService _leaderboardService;
    private readonly IServiceScopeFactory _provider;

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


    public ProfileParser(DataContext context, IMojangService mojangService, IServiceScopeFactory provider, ILeaderboardService leaderboardService)
    {
        _context = context;
        _mojangService = mojangService;
        _provider = provider;
        _leaderboardService = leaderboardService;
    }

    public async Task TransformProfilesResponse(RawProfilesResponse data, string? playerUuid) {
        if (!data.Success || data.Profiles is not { Length: > 0 }) return;

        foreach (var profile in data.Profiles) {
            await TransformSingleProfile(profile, playerUuid);
        }

        var profileIds = data.Profiles.Select(p => p.ProfileId.Replace("-", "")).ToList();

        // Make player as removed from all profiles that aren't in the response
        await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.PlayerUuid.Equals(playerUuid) && !profileIds.Contains(p.ProfileId))
            .ExecuteUpdateAsync(member =>
                member
                    .SetProperty(m => m.WasRemoved, true)
                    .SetProperty(m => m.IsSelected, false)
            );
        
        await _context.SaveChangesAsync();
    }

    private async Task TransformSingleProfile(RawProfileData profile, string? playerUuid)
    {
        var members = profile.Members;
        if (members.Count == 0) return;

        var profileId = profile.ProfileId.Replace("-", "");
        var existing = await _context.Profiles.FindAsync(profileId);

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
                _context.Profiles.Add(profileObj);
                await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task TransformMemberResponse(string playerId, RawMemberData memberData, Profile profile, bool selected, string requesterUuid)
    {
        var existing = await _fetchProfileMemberData(_context, playerId, profile.ProfileId);

        if (existing?.WasRemoved == true && memberData.DeletionNotice is not null) return;
        
        if (existing is not null)
        {
            // Only update if the player is the requester
            if (playerId == requesterUuid) {
                existing.IsSelected = selected;
                existing.ProfileName = profile.ProfileName;
            }
            
            // Only update if null (profile names can differ between members)
            existing.ProfileName ??= profile.ProfileName;
            
            existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            existing.WasRemoved = memberData.DeletionNotice is not null;
            
            existing.MinecraftAccount.ProfilesLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _context.MinecraftAccounts.Update(existing.MinecraftAccount);
            _context.Entry(existing).State = EntityState.Modified;
            
            await UpdateProfileMember(profile, existing, memberData);
            
            return;
        }
        
        var minecraftAccount = await _mojangService.GetMinecraftAccountByUuid(playerId);
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
        
        _context.ProfileMembers.Add(member);
        profile.Members.Add(member);

        minecraftAccount.ProfilesLastUpdated = playerId == requesterUuid 
            ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : 0;

        await UpdateProfileMember(profile, member, memberData);

        try
        {
            await _context.SaveChangesAsync();
            await _context.Entry(member).GetDatabaseValuesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task UpdateProfileMember(Profile profile, ProfileMember member, RawMemberData incomingData)
    {
        member.Collections = incomingData.Collection ?? member.Collections;
        member.Api.Collections = incomingData.Collection is not null;
        
        member.SkyblockXp = incomingData.Leveling?.Experience ?? 0;
        member.Purse = incomingData.CoinPurse ?? 0;
        member.Pets = incomingData.Pets?.ToList() ?? new List<Pet>();
        
        member.Unparsed = new UnparsedApiData
        {
            Perks = incomingData.Perks,
            TempStatBuffs = incomingData.TempStatBuffs,
            AccessoryBagSettings = incomingData.AccessoryBagSettings ?? new JsonObject(),
        };
        
        member.ParseJacob(incomingData.Jacob);
        await _context.SaveChangesAsync();
        
        member.ParseSkills(incomingData);
        member.ParseCollectionTiers(incomingData.UnlockedCollTiers);
        
        // Fire and forget
        #pragma warning disable CS4014
        Task.Run(() => AddTimeScaleRecords(member.Id));
        Task.Run(() => ParseJacobContests(member.Id, incomingData));
        #pragma warning restore CS4014

        profile.CombineMinions(incomingData.CraftedGenerators);
        
        await member.ParseFarmingWeight(profile.CraftedMinions, incomingData);

        // Load progress for all active events (if any)
        if (member.EventEntries is { Count: > 0 }) {
            foreach (var entry in member.EventEntries) {
                entry.LoadProgress(member);
            }
        }

        _context.Farming.Update(member.Farming);
        _context.ProfileMembers.Update(member);
        _context.JacobData.Update(member.JacobData);
        _context.Profiles.Update(profile);
        
        UpdateLeaderboards(member);

        await _context.SaveChangesAsync();
    }
    
    private void UpdateLeaderboards(ProfileMember member) {
        // TODO: Update all leaderboards, not just farming weight
        // (I'm avoiding this right now because idk a clever solution that's not a ton of if statements)

        var farmingWeight = member.Farming.TotalWeight;
        _leaderboardService.UpdateLeaderboardScore("farmingweight", member.Id.ToString(), farmingWeight);
    }

    private async Task ParseJacobContests(Guid memberId, RawMemberData incomingData) {
        using var scope = _provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        var member = await context.ProfileMembers
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .FirstOrDefaultAsync(p => p.Id == memberId);
        if (member is null) return;

        await member.ParseJacobContests(incomingData.Jacob, context, cache);
    }

    private async Task AddTimeScaleRecords(Guid memberId) {
        using var scope = _provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var member = await context.ProfileMembers
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.Id == memberId);
        if (member is null) return;
        
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
        context.CropCollections.Add(cropCollection);
        
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
        context.SkillExperiences.Add(skillExp);

        await context.SaveChangesAsync();
    }
}

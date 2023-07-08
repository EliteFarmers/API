using System.Text.Json;
using EliteAPI.Data;
using EliteAPI.Services;
using EliteAPI.Services.MojangService;
using AutoMapper;
using EliteAPI.Models.DTOs.Incoming;
using Microsoft.EntityFrameworkCore;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.FarmingWeight;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Services.CacheService;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;

namespace EliteAPI.Parsers.Skyblock;

public class ProfileParser
{
    private readonly DataContext _context;
    private readonly IMojangService _mojangService;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    private readonly Func<DataContext, long, Task<JacobContest?>> _fetchJacobContest = 
        EF.CompileAsyncQuery((DataContext context, long key) =>            
            context.JacobContests
                .FirstOrDefault(j => j.Id == key)
        );

    private readonly Func<DataContext, string, string, Task<ProfileMember?>> _fetchProfileMemberData = 
        EF.CompileAsyncQuery((DataContext context, string playerUuid, string profileUuid) =>            
            context.ProfileMembers
                .Include(p => p.Profile)
                .Include(p => p.Skills)
                .Include(p => p.FarmingWeight)
                .Include(p => p.JacobData)
                .ThenInclude(j => j.Contests)
                .ThenInclude(c => c.JacobContest)
                .AsSplitQuery()
                .FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
        );
    
    public ProfileParser(DataContext context, IMojangService mojangService, IMapper mapper, ICacheService cacheService)
    {
        _context = context;
        _mojangService = mojangService;
        _mapper = mapper;
        _cache = cacheService;
    }

    public async Task<List<ProfileMember>> TransformProfilesResponse(RawProfilesResponse data, string? playerUuid)
    {
        var profiles = new List<ProfileMember>();
        if (!data.Success || data.Profiles is not { Length: > 0 }) return profiles;

        var existingProfileIds = new List<string>();
        
        foreach (var profile in data.Profiles)
        {
            var transformed = await TransformSingleProfile(profile, playerUuid);

            if (transformed == null) continue;

            var owned = transformed.Members
                .Where(member => member.PlayerUuid.Equals(playerUuid));

            profiles.AddRange(owned);
            existingProfileIds.Add(transformed.ProfileId);
        }

        var missingProfileIds = await _context.ProfileMembers               
            .Where(p => p.PlayerUuid.Equals(playerUuid) && !p.WasRemoved && !existingProfileIds.Contains(p.ProfileId))
            .Select(p => p.Id)
            .ToListAsync();

        if (missingProfileIds.Count == 0) return profiles;

        // Mark all members that were not returned as deleted
        foreach (var memberId in missingProfileIds)
        {
            var member = await _context.ProfileMembers.FindAsync(memberId);
            if (member is null) continue;

            member.WasRemoved = true;
        }

        await _context.SaveChangesAsync();

        return profiles;
    }

    public async Task<Profile?> TransformSingleProfile(RawProfileData profile, string? playerUuid)
    {
        var members = profile.Members;
        if (members.Count == 0) return null;

        var profileId = profile.ProfileId.Replace("-", "");
        var existing = await _context.Profiles.FindAsync(profileId);

        var profileObj = existing ?? new Profile
        {
            ProfileId = profileId,
            ProfileName = profile.CuteName,
            GameMode = profile.GameMode,
            Members = new List<ProfileMember>(),
            IsDeleted = false,
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
            await TransformMemberResponse(playerId, memberData, profileObj, selected);
        }

        MetricsService.IncrementProfilesTransformedCount(profileId ?? "Unknown");
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return profileObj;
    }

    public async Task TransformMemberResponse(string playerId, RawMemberData memberData, Profile profile, bool selected)
    {
        var existing = await _fetchProfileMemberData(_context, playerId, profile.ProfileId);

        if (existing is not null)
        {
            //if (existing.WasRemoved) return;

            existing.IsSelected = selected;
            existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

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
            MinecraftAccount = minecraftAccount,

            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsSelected = selected,
            WasRemoved = false
        };
        
        _context.ProfileMembers.Add(member);
        profile.Members.Add(member);
        
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
        member.SkyblockXp = incomingData.Leveling?.Experience ?? 0;
        member.Purse = incomingData.CoinPurse ?? 0;
        member.Pets = incomingData.Pets?.ToList() ?? new List<Pet>();

        member.ParseJacob(incomingData.Jacob);
        await _context.SaveChangesAsync();
        
        await member.ParseJacobContests(incomingData.Jacob, _context, _cache);

        member.ParseSkills(incomingData);
        member.ParseCollectionTiers(incomingData.UnlockedCollTiers);

        profile.CombineMinions(incomingData.CraftedGenerators);

        member.ParseFarmingWeight(profile.CraftedMinions);

        _context.FarmingWeights.Update(member.FarmingWeight);
        _context.ProfileMembers.Update(member);
        _context.JacobData.Update(member.JacobData);
        _context.Profiles.Update(profile);

        await _context.SaveChangesAsync();
    }
}

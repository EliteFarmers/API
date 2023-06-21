using EliteAPI.Data;
using EliteAPI.Mappers.Skyblock;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.HypixelService;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.ProfileService;

public class ProfileService : IProfileService
{
    private readonly DataContext _context;
    private readonly IHypixelService _hypixelService;
    private readonly ProfileParser _profileParser;

    public ProfileService(DataContext context, IHypixelService hypixelService, ProfileParser profileParser)
    {
        _context = context;
        _hypixelService = hypixelService;
        _profileParser = profileParser;
    }

    public async Task<Profile?> GetProfile(string profileId)
    {
        return await _context.Profiles
            .Include(p => p.Members)
            .ThenInclude(m => m.MinecraftAccount)
            .FirstOrDefaultAsync(p => p.ProfileId.Equals(profileId));
    }

    public async Task<Profile?> GetPlayersProfileByName(string playerUuid, string profileName)
    {
        var member = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.Profile.ProfileName.Equals(profileName))
            .FirstOrDefaultAsync();

        return member?.Profile;
    }

    public async Task<Profile?> GetPlayersSelectedProfile(string playerUuid)
    {
        return (await GetSelectedProfileMember(playerUuid))?.Profile;
    }

    public async Task<bool> AddProfile(Profile profile)
    {
        try
        {
            await _context.Profiles.AddAsync(profile);
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> UpdateProfile(string profileUuid, Profile newProfile)
    {
        var profile = await GetProfile(profileUuid);
        if (profile == null) return false;

        profile.ProfileName = newProfile.ProfileName;
        profile.GameMode = newProfile.GameMode;
        profile.LastSave = newProfile.LastSave;
        profile.Banking = newProfile.Banking;
        profile.CraftedMinions = newProfile.CraftedMinions;
        profile.Members = newProfile.Members;

        _context.Profiles.Update(profile);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteProfile(string profileUuid)
    {
        var profile = await GetProfile(profileUuid);
        if (profile == null) return false;
        
        _context.Profiles.Remove(profile);
        await _context.SaveChangesAsync();

        return true;
    }

    private readonly Func<DataContext, string, string, Task<ProfileMember?>> _fetchProfileMemberData = 
        EF.CompileAsyncQuery((DataContext context, string profileUuid, string playerUuid) =>            
            context.ProfileMembers
                   .Include(p => p.Profile)
                   .Include(p => p.Skills)
                   .Include(p => p.JacobData)
                   //.ThenInclude(j => j.Contests)
                   .AsSplitQuery()
                   .FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
        );

    public async Task<ProfileMember?> GetProfileMember(string profileUuid, string playerUuid)
    {
        var member = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .FirstOrDefaultAsync();

        // Fetch new data if it doesn't exists or is old
        if (member == null || member.LastUpdated.AddMinutes(10) < DateTime.Now || true)
        {
            await RefreshProfileMembers(playerUuid);
        }

        return await _context.ProfileMembers
            .Include(p => p.Profile)
            .Include(p => p.Skills)
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .AsSplitQuery()
            .Where(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .FirstOrDefaultAsync();
    }

    public async Task<ProfileMember?> GetSelectedProfileMember(string playerUuid)
    {
        var member = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.IsSelected)
            .FirstOrDefaultAsync();

        if (member == null || true)
        {
            var data = await RefreshProfileMembers(playerUuid);

            return data.FirstOrDefault(p => p.IsSelected);
        }

        return null;
    }

    public async Task<ProfileMember?> GetProfileMemberByProfileName(string playerUuid, string profileName)
    {
        return await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.Profile.ProfileName.Equals(profileName))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> AddProfileMember(ProfileMember member)
    {
        try
        {
            await _context.ProfileMembers.AddAsync(member);
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> UpdateProfileMember(string profileUuid, string playerUuid, ProfileMember newMember)
    {
        var member = await GetProfileMember(profileUuid, playerUuid);
        if (member == null) return false;

        member.IsSelected = newMember.IsSelected;
        member.Skills = newMember.Skills;
        member.Pets = newMember.Pets;
        member.Collections = newMember.Collections;
        member.JacobData = newMember.JacobData;
        member.WasRemoved = newMember.WasRemoved;
        member.LastUpdated = newMember.LastUpdated;

        _context.ProfileMembers.Update(member);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteProfileMember(string profileUuid, string playerUuid)
    {
        var member = await GetProfileMember(profileUuid, playerUuid);
        if (member == null) return false;

        member.WasRemoved = true;

        _context.ProfileMembers.Update(member);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<List<ProfileMember>> RefreshProfileMembers(string playerUuid)
    {
        var rawData = await _hypixelService.FetchProfiles(playerUuid);
        var profiles = rawData.Value;

        if (profiles == null) return new List<ProfileMember>();

        return await _profileParser.TransformProfilesResponse(profiles, playerUuid);
    }
}

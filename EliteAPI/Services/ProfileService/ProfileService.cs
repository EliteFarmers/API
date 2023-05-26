using EliteAPI.Data;
using EliteAPI.Mappers.Skyblock;
using EliteAPI.Models.Hypixel;
using EliteAPI.Services.HypixelService;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.ProfileService;

public class ProfileService : IProfileService
{
    private readonly DataContext _context;
    private readonly IHypixelService _hypixelService;
    private readonly ProfileMapper _mapper;

    public ProfileService(DataContext context, IHypixelService hypixelService, ProfileMapper mapper)
    {
        _context = context;
        _hypixelService = hypixelService;
        _mapper = mapper;
    }

    public async Task<Profile?> GetProfile(string profileId)
    {
        return await _context.Profiles.FindAsync(profileId);
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

    public async Task<ProfileMember?> GetProfileMember(string profileUuid, string playerUuid)
    {
        var member = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Include(p => p.Collections)
            .Include(p => p.Skills)
            .Include(p => p.Pets)
            .Include(p => p.JacobData)
            .Where(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .FirstOrDefaultAsync();

        // TODO: Check if the member data is old
        if (member != null)
        {
            Console.WriteLine("Member data is not old");
            return member;
        }

        var rawData = await _hypixelService.FetchProfiles(playerUuid);
        var profiles = rawData.Value;

        if (profiles == null) return null;

        await _mapper.TransformProfilesResponse(profiles);

        return await _context.ProfileMembers
            .Include(p => p.Profile)
            .Include(p => p.Collections)
            .Include(p => p.Skills)
            .Include(p => p.Pets)
            .Include(p => p.JacobData)
            .Where(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .FirstOrDefaultAsync();
    }

    public async Task<ProfileMember?> GetSelectedProfileMember(string playerUuid)
    {
        var member = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.IsSelected)
            .FirstOrDefaultAsync();

        // TODO: Check if the member data is old
        if (member != null)
        {
            return member;
        }

        var rawData = await _hypixelService.FetchProfiles(playerUuid);
        var profiles = rawData.Value;

        if (profiles == null) return null;

        await _mapper.TransformProfilesResponse(profiles);

        return await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.IsSelected)
            .FirstOrDefaultAsync();
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
}

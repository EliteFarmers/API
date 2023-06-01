using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Services.ProfileService;

public interface IProfileService
{
    public Task<Profile?> GetProfile(string profileUuid);
    public Task<Profile?> GetPlayersProfileByName(string playerUuid, string profileName);
    public Task<Profile?> GetPlayersSelectedProfile(string playerUuid);

    public Task<bool> AddProfile(Profile profile);
    public Task<bool> UpdateProfile(string profileUuid, Profile newProfile);
    public Task<bool> DeleteProfile(string profileUuid);

    public Task<ProfileMember?> GetProfileMember(string profileUuid, string playerUuid); 
    public Task<ProfileMember?> GetSelectedProfileMember(string playerUuid);
    public Task<ProfileMember?> GetProfileMemberByProfileName(string playerUuid, string profileName);
    
    public Task<bool> AddProfileMember(ProfileMember member);
    public Task<bool> UpdateProfileMember(string profileUuid, string playerUuid, ProfileMember newMember);
    public Task<bool> DeleteProfileMember(string profileUuid, string playerUuid);
}

using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Services.ProfileService;

public interface IProfileService
{
    public Task<Profile?> GetProfile(string profileUuid);
    public Task<Profile?> GetPlayersProfileByName(string playerUuid, string profileName);
    public Task<Profile?> GetPlayersSelectedProfile(string playerUuid);
    public Task<List<Profile>> GetPlayersProfiles(string playerUuid);
    public Task<List<ProfileDetailsDto>> GetProfilesDetails(string playerUuid);

    public Task<ProfileMember?> GetProfileMember(string playerUuid, string profileUuid);
    public Task<ProfileMember?> GetSelectedProfileMember(string playerUuid);

    public Task<PlayerData?> GetPlayerData(string playerUuid, bool skipCooldown = false);
    public Task<PlayerData?> GetPlayerDataByIgn(string playerName, bool skipCooldown = false);
    public Task<PlayerData?> GetPlayerDataByUuidOrIgn(string uuidOrIgn, bool skipCooldown = false);
}

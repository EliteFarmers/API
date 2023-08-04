using EliteAPI.Models.Entities;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Services.MemberService; 

public interface IMemberService {
    Task UpdatePlayerIfNeeded(string playerUuid);
    Task UpdateProfileMemberIfNeeded(Guid memberId);
    Task<IQueryable<ProfileMember>?> ProfileMemberQuery(string playerUuid);
    Task<IQueryable<ProfileMember>?> ProfileMemberCompleteQuery(string playerUuid);
    Task<IQueryable<ProfileMember>?> ProfileMemberIgnQuery(string playerName);

    Task RefreshPlayerData(string playerUuid, MinecraftAccount? account = null);
    Task RefreshProfiles(string playerUuid);
}
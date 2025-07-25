﻿using EliteAPI.Features.Account.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Services.Interfaces; 

public interface IMemberService {
    Task UpdatePlayerIfNeeded(string playerUuid, float cooldownMultiplier = 1);
    Task UpdateProfileMemberIfNeeded(Guid memberId, float cooldownMultiplier = 1);
    Task<Guid?> GetProfileMemberId(string playerUuid, string profileId);
    Task<IQueryable<ProfileMember>?> ProfileMemberQuery(string playerUuid, float cooldownMultiplier = 1);
    Task<IQueryable<ProfileMember>?> ProfileMemberCompleteQuery(string playerUuid, float cooldownMultiplier = 1);
    Task<IQueryable<ProfileMember>?> ProfileMemberIgnQuery(string playerName);

    Task RefreshPlayerData(string playerUuid, MinecraftAccount? account = null);
    Task RefreshProfiles(string playerUuid);
}
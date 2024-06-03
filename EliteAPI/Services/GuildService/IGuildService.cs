using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.GuildService; 

public interface IGuildService {
    Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, ulong authorId, string lbId);
    bool HasGuildAdminPermissions(UserGuildDto guild);
    Task UpdateGuildData(ulong guildId, IncomingGuildDto guild);
    Task UpdateGuildChannelData(ulong guildId, IncomingGuildChannelDto channel);
    Task UpdateGuildRoleData(ulong guildId, IncomingGuildRoleDto role);
    Task<UserGuildDto?> GetUserGuild(string userId, ulong guildId);
    Task<UserGuildDto?> GetUserGuild(ApiUser user, ulong guildId);
}
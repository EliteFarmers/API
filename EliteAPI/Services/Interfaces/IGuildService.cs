using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.Interfaces; 

public interface IGuildService {
    Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, string authorId, string lbId);
    Task UpdateGuildData(ulong guildId, IncomingGuildDto guild);
    Task UpdateGuildChannelData(ulong guildId, IncomingGuildChannelDto channel);
    Task UpdateGuildRoleData(ulong guildId, IncomingGuildRoleDto role);
    Task<GuildMemberDto?> GetUserGuild(string userId, ulong guildId);
    Task<GuildMemberDto?> GetUserGuild(ApiUser user, ulong guildId);
}
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Guilds.Services;

public interface IGuildService
{
	Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, string authorId, string lbId);
	Task UpdateGuildData(ulong guildId, IncomingGuildDto guild);
	Task UpdateGuildChannelData(ulong guildId, IncomingGuildChannelDto channel, bool skipSave = false);
	Task UpdateGuildRoleData(ulong guildId, IncomingGuildRoleDto role, bool skipSave = false);
	Task<GuildMemberDto?> GetUserGuild(string userId, ulong guildId);
	Task<GuildMemberDto?> GetUserGuild(ApiUser user, ulong guildId);
}
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.GuildService; 

public interface IGuildService {
    Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, ulong authorId, string lbId);
    bool HasGuildAdminPermissions(UserGuildDto guild);
}
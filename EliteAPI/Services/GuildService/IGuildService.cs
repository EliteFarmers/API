using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.GuildService; 

public interface IGuildService {
    Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, string lbId);
}
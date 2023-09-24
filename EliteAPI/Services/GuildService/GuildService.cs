using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.DTOs.Outgoing.Messaging;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.MessageService;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.GuildService; 

public class GuildService : IGuildService {
    private readonly IDiscordService _discordService;
    private readonly IMessageService _messageService;

    public GuildService(IDiscordService discordService, IMessageService messageService)
    {
        _discordService = discordService;
        _messageService = messageService;
    }

    public async Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, ulong authorId, string lbId) {
        var guild = await _discordService.GetGuild(guildId);

        if (guild is null) {
            return new NotFoundObjectResult("Guild not found.");
        }
        
        var channel = guild.Channels.FirstOrDefault(x => x.Id == channelId);
        
        if (channel is null) {
            return new NotFoundObjectResult("Channel not found.");
        }
        
        var message = new MessageDto {
            Name = "leaderboardInit",
            GuildId = guildId.ToString(),
            AuthorId = authorId.ToString(),
            Data = $$"""
                 {
                     "channelId": "{{channelId}}",
                     "leaderboardId": "{{lbId}}"
                 }
            """
        };
        
        _messageService.SendMessage(message);

        return new OkResult();
    }

    public bool HasGuildAdminPermissions(UserGuildDto guild) {
        var permissions = guild.Permissions;
        
        if (!ulong.TryParse(permissions, out var bits)) {
            return false;
        }
        
        const ulong admin = 0x8;
        const ulong manageGuild = 0x20;

        // Check if the user has the manage guild or admin permission
        return (bits & admin) == admin || (bits & manageGuild) == manageGuild;
    }
}
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.DTOs.Outgoing.Messaging;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.MessageService;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.GuildService; 

public class GuildService(
    IDiscordService discordService, 
    IMessageService messageService)
    : IGuildService 
{
    public async Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, ulong authorId, string lbId) {
        var guild = await discordService.GetGuild(guildId);

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
        
        messageService.SendMessage(message);

        return new OkResult();
    }

    public bool HasGuildAdminPermissions(UserGuildDto guild) {
        return guild.HasGuildAdminPermissions();
    }
}
using System.Net.Http.Headers;
using System.Text;
using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services.GuildService; 

public class GuildService : IGuildService {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDiscordService _discordService;
    private readonly ILogger<GuildService> _logger;
    
    private readonly string _botToken;

    private const string DiscordBaseUrl = "https://discord.com/api/v10";

    public GuildService(IHttpClientFactory httpClientFactory, IDiscordService discordService, ILogger<GuildService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _discordService = discordService;
        _logger = logger;
        
        _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
                    ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");
    }

    public async Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, string lbId) {
        var guild = await _discordService.GetGuild(guildId);

        if (guild is null) {
            return new NotFoundObjectResult("Guild not found.");
        }
        
        var channel = guild.Channels.FirstOrDefault(x => x.Id == channelId);
        
        if (channel is null) {
            return new NotFoundObjectResult("Channel not found.");
        }
        
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);

        var embed = $@"
            {{
                ""embeds"": [
                    {{
                        ""title"": ""Jacob Leaderboard"",
                        ""description"": ""Newly created leaderboard! Awaiting setup...""
                    }}
                ],
                ""components"": [
                    {{
                        ""type"": 1,
                        ""components"": [
                            {{
                                ""type"": 2,
                                ""label"": ""Setup"",
                                ""style"": 2,
                                ""custom_id"": ""LBSETUP|{lbId}""
                            }}
                        ]
                    }}
                ],
                ""allowed_mentions"": {{
                    ""parse"": []
                }}
            }}
        ";

        try {
            var response = await client.PostAsync($"{DiscordBaseUrl}/channels/{channelId}/messages",
                new StringContent(embed, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("Failed to send discord message to {Guild}, {Channel}: {Reason}", guildId, channelId, response.ReasonPhrase);
                return new UnauthorizedObjectResult("Failed to send message in Discord, check permissions.");
            }

            return new OkResult();
        }
        catch (Exception e) {
            _logger.LogError("Failed to send discord message to {Guild}, {Channel}: {Reason}", guildId, channelId, e.StackTrace);
            return new UnauthorizedObjectResult("Failed to send message in Discord, check permissions.");
        }
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
using System.Net.Http.Headers;
using System.Text.Json;
using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Background;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services.DiscordService;

public class DiscordService(
    IHttpClientFactory httpClientFactory,
    DataContext context,
    ILogger<DiscordService> logger,
    IConnectionMultiplexer redis,
    UserManager<ApiUser> userManager,
    IOptions<ConfigCooldownSettings> coolDowns,
    IMapper mapper,
    IBackgroundTaskQueue taskQueue)
    : IDiscordService 
{
    
    private const string ClientName = "EliteAPI";
    private readonly string _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") 
                                        ?? throw new Exception("DISCORD_CLIENT_ID env variable is not set.");
    private readonly string _clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET") 
                                            ?? throw new Exception("DISCORD_CLIENT_SECRET env variable is not set.");
    private readonly string _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
                                        ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");

    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    
    private const string DiscordBaseUrl = "https://discord.com/api/v10";

    public async Task<DiscordUpdateResponse?> GetDiscordUser(string? accessToken, string? refreshToken)
    {
        if (accessToken is null && refreshToken is null) return null;

        if (accessToken is null && refreshToken is not null)
        {
            var refreshed = await RefreshDiscordUser(refreshToken);
            if (refreshed is null) return null;

            refreshed.Account = await FetchDiscordUser(refreshed.AccessToken);
            return refreshed;
        }

        if (accessToken is not null && refreshToken is null)
        {
            var refresh = await FetchRefreshToken(accessToken);
            if (refresh is null) return null;

            refresh.Account = await FetchDiscordUser(refresh.AccessToken);
            return refresh;
        }

        if (accessToken is null) return null;
        
        var account = await FetchDiscordUser(accessToken);
        if (account is null) return null;

        return new DiscordUpdateResponse()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken ?? "",
            Account = account
        };
    }

    private async Task<EliteAccount?> FetchDiscordUser(string accessToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(DiscordBaseUrl + "/users/@me");

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var user = await response.Content.ReadFromJsonAsync<DiscordUserResponse>();

            if (user is null) return null;

            var existing = await context.Accounts.FindAsync(user.Id);
                
            var account = existing ?? new EliteAccount()
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName ?? user.Username
            };

            account.Username = user.Username;
            account.DisplayName = user.DisplayName ?? user.Username;
            account.Discriminator = user.Discriminator;
            account.Email = user.Email ?? account.Email;
            account.Avatar = user.Avatar;
            account.Locale = user.Locale;

            if (existing is null) context.Accounts.Add(account);
            await context.SaveChangesAsync();

            return account;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task<DiscordUpdateResponse?> RefreshDiscordUser(string refreshToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        // application/x-www-form-urlencoded
        var body = new Dictionary<string, string>()
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "scope", "identify guilds role_connections.write" },
        };

        var response = await client.PostAsync(DiscordBaseUrl + "/oauth2/token", new FormUrlEncodedContent(body));

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var refreshTokenResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            return ProcessResponse(refreshTokenResponse);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task<DiscordUpdateResponse?> FetchRefreshToken(string accessToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // application/x-www-form-urlencoded
        var body = new Dictionary<string, string>()
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "grant_type", "authorization_code" },
            { "code", accessToken },
            { "scope", "identify guilds role_connections.write" },
        };

        var response = await client.PostAsync(DiscordBaseUrl + "/oauth2/token", new FormUrlEncodedContent(body));

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var tokenResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            return ProcessResponse(tokenResponse);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task<EliteAccount?> GetDiscordUser(string accessToken) {
        return await FetchDiscordUser(accessToken);
    }

    public async Task<string> GetGuildMemberPermissions(ulong guildId, ulong userId, string accessToken) {
        var guilds = await GetUsersGuilds(userId, accessToken);
        
        var guild = guilds.FirstOrDefault(g => g.Id.Equals(guildId.ToString()));

        return guild?.Permissions ?? "0";
    }
    
    public async Task<List<UserGuildDto>> GetUsersGuilds(ulong userId, string accessToken) {
        var url = DiscordBaseUrl + "/users/@me/guilds";
        var key = $"user:guilds:{userId}";
        
        var db = redis.GetDatabase();
        if (db.KeyExists(key)) {
            var guilds = await db.StringGetAsync(key);
            
            if (guilds.IsNullOrEmpty) return new List<UserGuildDto>();

            try {
                var list = JsonSerializer.Deserialize<List<UserGuildDto>>(guilds!);
                if (list is not null) return list;
            } catch (Exception e) {
                logger.LogError(e, "Failed to parse guilds from Redis");
            }
        }
        
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch guilds from Discord");
            return new List<UserGuildDto>();
        }
        
        try {
            var list = await response.Content.ReadFromJsonAsync<List<DiscordGuild>>();
            if (list is null) return new List<UserGuildDto>();
            
            var guilds = mapper.Map<List<UserGuildDto>>(list);

            foreach (var guild in guilds) {
                guild.HasBot = await db.KeyExistsAsync($"bot:guild:{guild.Id}");
            }

            await db.StringSetAsync(key, JsonSerializer.Serialize(guilds), TimeSpan.FromSeconds(_coolDowns.UserGuildsCooldown));
            return guilds;
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse guilds from Discord");
            return new List<UserGuildDto>();
        }
    }

    public async Task<UserGuildDto?> GetUserGuildIfManagable(ApiUser user, ulong guildId) {
        if (!user.AccountId.HasValue || user.DiscordAccessToken is null) return null;
        
        var roles = await userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains(ApiUserRoles.Admin);

        var guilds = await GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (isAdmin) {
            return new UserGuildDto {
                Id = guildId.ToString(),
                Permissions = "8",
                Name = string.Empty
            };
        }

        if (userGuild is null || !userGuild.HasGuildAdminPermissions()) {
            return null;
        }

        return userGuild;
    }

    public async Task<Guild?> GetGuild(ulong guildId, bool skipCache = false) {
        if (!skipCache) {
            await RefreshBotGuilds();
        }
        
        var existing = await context.Guilds
            .Include(g => g.Channels)
            .Include(g => g.Roles)
            .FirstOrDefaultAsync(g => g.Id == guildId);
        
        if (existing is not null && !existing.LastUpdated.OlderThanSeconds(_coolDowns.DiscordGuildsCooldown)) {
            return existing;
        }
        
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
        
        var response = await client.GetAsync(DiscordBaseUrl + $"/guilds/{guildId}?with_counts=true");
        
        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch guild from Discord");
            return existing;
        }
        
        try {
            var guild = await response.Content.ReadFromJsonAsync<FullDiscordGuild>();
            if (guild is null) return existing;
            
            return await UpdateDiscordGuild(guild);
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse guild from Discord");
            return existing;
        }
    }

    public async Task RefreshDiscordGuild(ulong guildId) {
        await GetGuild(guildId, true);
    }

    private async Task<Guild?> UpdateDiscordGuild(FullDiscordGuild? incoming, bool fetchChannels = true) {
        if (incoming is null) return null;
        if (!ulong.TryParse(incoming.Id, out var guildId)) return null;
        
        var guild = await context.Guilds.FindAsync(guildId);
        
        if (guild is null) {
            context.Guilds.Add(new Guild {
                Id = guildId,
                Name = incoming.Name,
                Icon = incoming.Icon,
                DiscordFeatures = incoming.Features,
                MemberCount = incoming.MemberCount
            });
        } else {
            guild.Name = incoming.Name;
            guild.Icon = incoming.Icon;
            guild.DiscordFeatures = incoming.Features;
            guild.InviteCode = incoming.VanityUrlCode ?? guild.InviteCode;
            guild.Banner = incoming.Splash;
            guild.MemberCount = incoming.MemberCount;
            guild.LastUpdated = DateTimeOffset.UtcNow;
            
            if (guild.Features.JacobLeaderboardEnabled) {
                guild.Features.JacobLeaderboard ??= new GuildJacobLeaderboardFeature();
            }
                
            if (guild.Features.VerifiedRoleEnabled) {
                guild.Features.VerifiedRole ??= new VerifiedRoleFeature();
            }
        }
        
        await context.SaveChangesAsync();

        if (!fetchChannels || guild is null) return guild;
        
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);

        var channels = await client.GetAsync(DiscordBaseUrl + $"/guilds/{incoming.Id}/channels");
        if (!channels.IsSuccessStatusCode) return guild;
        
        var channelList = await channels.Content.ReadFromJsonAsync<List<DiscordChannel>>();
        if (channelList is null) return guild;
        
        foreach (var channel in channelList) {
            await UpdateDiscordChannel(guild, channel, false);
        }

        await context.SaveChangesAsync();
        
        return guild;
    }

    private async Task UpdateDiscordChannel(Guild guild, DiscordChannel channel, bool save = true) {
        if (!ulong.TryParse(channel.Id, out var channelId)) return;
        
        var existingChannel = guild.Channels.FirstOrDefault(c => c.Id == channelId);
        
        if (existingChannel is null) {
            var newChannel = new GuildChannel {
                GuildId = guild.Id,
                
                Id = channelId,
                Name = channel.Name,
                Position = channel.Position,
                Type = (int) channel.Type
            };
            
            guild.Channels.Add(newChannel);
            context.GuildChannels.Add(newChannel);
        } else {
            existingChannel.Name = channel.Name;
            existingChannel.Position = channel.Position;
            existingChannel.Type = (int) channel.Type;
        }

        if (save) {
            await context.SaveChangesAsync();
        }
    }

    private DiscordUpdateResponse? ProcessResponse(RefreshTokenResponse? tokenResponse)
    {
        if (tokenResponse?.AccessToken is null || tokenResponse.RefreshToken is null || tokenResponse.Error is not null) return null;

        DateTimeOffset? accessTokenExpires = tokenResponse.ExpiresIn > 0 ? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn) : null;
        var refreshTokenExpires = DateTimeOffset.UtcNow.AddDays(30);

        return new DiscordUpdateResponse()
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            AccessTokenExpires = accessTokenExpires,
            RefreshTokenExpires = refreshTokenExpires,
            Account = null
        };
    }
}

public class DiscordUpdateResponse
{
    public required string AccessToken { get; set; }
    public DateTimeOffset? AccessTokenExpires { get; set; }
    public required string RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpires { get; set; }
    public EliteAccount? Account { get; set; }
}

public static class DiscordExtensions 
{
    public static bool HasGuildAdminPermissions(this UserGuildDto guild) {
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